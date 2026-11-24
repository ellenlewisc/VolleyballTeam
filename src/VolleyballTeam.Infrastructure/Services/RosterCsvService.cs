using System.Globalization;
using System.Net.Mail;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure.Data;

namespace VolleyballTeam.Infrastructure.Services;

public class RosterCsvRecord
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? GradYear { get; set; }
    public string? JerseyNumber { get; set; }
    public string? PrimaryPosition { get; set; }
    public string? SecondaryPosition { get; set; }
    public string? Guardian1FirstName { get; set; }
    public string? Guardian1LastName { get; set; }
    public string? Guardian1Email { get; set; }
    public string? Guardian1Relationship { get; set; }
    public string? Guardian2FirstName { get; set; }
    public string? Guardian2LastName { get; set; }
    public string? Guardian2Email { get; set; }
    public string? Guardian2Relationship { get; set; }
}

public enum ImportAction
{
    NewPlayer,
    ExistingPlayer,
    UpdateOnRoster,
    Skip,
}

public record ImportRow(
    int Line,
    string FirstName,
    string LastName,
    int? GradYear,
    int? JerseyNumber,
    Position? PrimaryPosition,
    Position? SecondaryPosition,
    IReadOnlyList<GuardianInput> Guardians,
    ImportAction Action,
    int? PlayerId,
    IReadOnlyList<string> Errors);

public record ImportPreview(IReadOnlyList<ImportRow> Rows)
{
    public int ValidCount => Rows.Count(r => r.Action != ImportAction.Skip);
}

public record ImportResult(int Added, int Updated, int Skipped);

public class RosterCsvService(AppDbContext db, RosterService roster)
{
    public const int MaxBytes = 256 * 1024;

    // Names typed into the app could start with "=" or "+"; escaping stops Excel running them as formulas.
    private static readonly CsvConfiguration WriteConfig = new(CultureInfo.InvariantCulture)
    {
        InjectionOptions = InjectionOptions.Escape,
    };

    private static readonly CsvConfiguration ReadConfig = new(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null,
        MissingFieldFound = null,
        TrimOptions = TrimOptions.Trim,
        PrepareHeaderForMatch = args => args.Header.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant(),
    };

    public async Task<byte[]> ExportAsync(int teamSeasonId, CancellationToken cancellationToken = default)
    {
        var entries = await db.RosterEntries
            .Where(r => r.TeamSeasonId == teamSeasonId && r.LeftOn == null)
            .OrderBy(r => r.JerseyNumber == null)
            .ThenBy(r => r.JerseyNumber)
            .ThenBy(r => r.Player!.LastName)
            .Select(r => new
            {
                r.Player!.FirstName,
                r.Player.LastName,
                r.Player.GradYear,
                r.JerseyNumber,
                r.PrimaryPosition,
                r.SecondaryPosition,
                Guardians = r.Player.Guardians.OrderBy(g => g.Id).Select(g => new { g.FirstName, g.LastName, g.Email, g.Relationship }).Take(2).ToList(),
            })
            .ToListAsync(cancellationToken);

        var records = entries.Select(e =>
        {
            var first = e.Guardians.ElementAtOrDefault(0);
            var second = e.Guardians.ElementAtOrDefault(1);
            return new RosterCsvRecord
            {
                FirstName = e.FirstName,
                LastName = e.LastName,
                GradYear = e.GradYear?.ToString(CultureInfo.InvariantCulture),
                JerseyNumber = e.JerseyNumber?.ToString(CultureInfo.InvariantCulture),
                PrimaryPosition = e.PrimaryPosition?.ToString(),
                SecondaryPosition = e.SecondaryPosition?.ToString(),
                Guardian1FirstName = first?.FirstName,
                Guardian1LastName = first?.LastName,
                Guardian1Email = first?.Email,
                Guardian1Relationship = first?.Relationship,
                Guardian2FirstName = second?.FirstName,
                Guardian2LastName = second?.LastName,
                Guardian2Email = second?.Email,
                Guardian2Relationship = second?.Relationship,
            };
        });

        await using var stream = new MemoryStream();
        await using (var writer = new StreamWriter(stream, leaveOpen: true))
        await using (var csv = new CsvWriter(writer, WriteConfig))
        {
            await csv.WriteRecordsAsync(records, cancellationToken);
        }

        return stream.ToArray();
    }

    public async Task<ImportPreview> PreviewAsync(int teamSeasonId, string csvText, CancellationToken cancellationToken = default)
    {
        var records = ReadRecords(csvText);

        var players = await db.Players
            .Select(p => new { p.Id, p.FirstName, p.LastName, p.GradYear })
            .ToListAsync(cancellationToken);
        var active = await db.RosterEntries
            .Where(r => r.TeamSeasonId == teamSeasonId && r.LeftOn == null)
            .Select(r => new { r.PlayerId, r.JerseyNumber })
            .ToListAsync(cancellationToken);

        var rows = new List<ImportRow>();
        var jerseysInFile = new Dictionary<int, int>();

        foreach (var (record, line) in records)
        {
            var errors = new List<string>();
            var firstName = record.FirstName?.Trim() ?? "";
            var lastName = record.LastName?.Trim() ?? "";

            if (firstName.Length == 0 || lastName.Length == 0)
            {
                errors.Add("First and last name are required.");
            }

            var gradYear = ParseNumber(record.GradYear, "Grad year", 1990, 2100, errors);
            var jersey = ParseNumber(record.JerseyNumber, "Jersey number", 0, 99, errors);
            var primary = ParsePosition(record.PrimaryPosition, "Primary position", errors);
            var secondary = ParsePosition(record.SecondaryPosition, "Secondary position", errors);
            var guardians = new List<GuardianInput>();
            AddGuardian(record.Guardian1FirstName, record.Guardian1LastName, record.Guardian1Email, record.Guardian1Relationship, "Guardian 1", guardians, errors);
            AddGuardian(record.Guardian2FirstName, record.Guardian2LastName, record.Guardian2Email, record.Guardian2Relationship, "Guardian 2", guardians, errors);

            var matches = players
                .Where(p => string.Equals(p.FirstName, firstName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(p.LastName, lastName, StringComparison.OrdinalIgnoreCase)
                    && (gradYear is null || p.GradYear is null || p.GradYear == gradYear))
                .ToList();

            if (matches.Count > 1)
            {
                errors.Add("Matches more than one player in the club; add a grad year to tell them apart.");
            }

            var playerId = matches.Count == 1 ? matches[0].Id : (int?)null;
            var onRoster = playerId is not null && active.Any(a => a.PlayerId == playerId);

            if (jersey is int number)
            {
                if (jerseysInFile.TryGetValue(number, out var otherLine))
                {
                    errors.Add($"Number {number} is also used on line {otherLine}.");
                }
                else
                {
                    jerseysInFile[number] = line;
                }

                if (active.Any(a => a.JerseyNumber == number && a.PlayerId != playerId))
                {
                    errors.Add($"Number {number} is already worn by another player on this roster.");
                }
            }

            var action = errors.Count > 0 ? ImportAction.Skip
                : onRoster ? ImportAction.UpdateOnRoster
                : playerId is not null ? ImportAction.ExistingPlayer
                : ImportAction.NewPlayer;

            rows.Add(new ImportRow(line, firstName, lastName, gradYear, jersey, primary, secondary, guardians, action, playerId, errors));
        }

        return new ImportPreview(rows);
    }

    public async Task<ImportResult> ImportAsync(int teamSeasonId, string csvText, CancellationToken cancellationToken = default)
    {
        await roster.EnsureEditableAsync(teamSeasonId, cancellationToken);
        var preview = await PreviewAsync(teamSeasonId, csvText, cancellationToken);

        int added = 0, updated = 0;
        foreach (var row in preview.Rows.Where(r => r.Action != ImportAction.Skip))
        {
            var placement = new RosterPlacement(row.JerseyNumber, row.PrimaryPosition, row.SecondaryPosition);
            Player player;

            if (row.PlayerId is int playerId)
            {
                player = await db.Players.Include(p => p.Guardians).SingleAsync(p => p.Id == playerId, cancellationToken);
                player.GradYear ??= row.GradYear;
            }
            else
            {
                player = new Player { FirstName = row.FirstName, LastName = row.LastName, GradYear = row.GradYear };
                db.Players.Add(player);
            }

            if (row.Action == ImportAction.UpdateOnRoster)
            {
                var entry = await db.RosterEntries.SingleAsync(r => r.TeamSeasonId == teamSeasonId && r.PlayerId == player.Id && r.LeftOn == null, cancellationToken);
                entry.JerseyNumber = row.JerseyNumber ?? entry.JerseyNumber;
                entry.PrimaryPosition = row.PrimaryPosition ?? entry.PrimaryPosition;
                entry.SecondaryPosition = row.SecondaryPosition ?? entry.SecondaryPosition;
                updated++;
            }
            else
            {
                var entry = roster.NewEntry(teamSeasonId, placement);
                entry.Player = player;
                db.RosterEntries.Add(entry);
                added++;
            }

            foreach (var guardian in row.Guardians)
            {
                var email = PlayerService.NormalizeEmail(guardian.Email);
                if (player.Guardians.All(g => g.Email != email))
                {
                    player.Guardians.Add(new Guardian
                    {
                        FirstName = guardian.FirstName.Trim(),
                        LastName = guardian.LastName.Trim(),
                        Email = email,
                        Relationship = TeamService.Blank(guardian.Relationship),
                    });
                }
            }
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new BusinessRuleException("The roster changed while importing (for example a jersey number was taken). Nothing was saved; preview the file again.");
        }

        return new ImportResult(added, updated, preview.Rows.Count - added - updated);
    }

    private static List<(RosterCsvRecord Record, int Line)> ReadRecords(string csvText)
    {
        try
        {
            using var reader = new StringReader(csvText);
            using var csv = new CsvReader(reader, ReadConfig);
            var records = new List<(RosterCsvRecord, int)>();

            if (!csv.Read() || !csv.ReadHeader())
            {
                return records;
            }

            while (csv.Read())
            {
                records.Add((csv.GetRecord<RosterCsvRecord>(), csv.Parser.Row));
            }

            return records;
        }
        catch (CsvHelperException)
        {
            throw new BusinessRuleException("That file couldn't be read as a CSV. Start from the exported roster to get the right columns.");
        }
    }

    private static int? ParseNumber(string? value, string label, int min, int max, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= min && number <= max)
        {
            return number;
        }

        errors.Add($"{label} must be a whole number from {min} to {max}.");
        return null;
    }

    private static Position? ParsePosition(string? value, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Enum.TryParse<Position>(value.Replace(" ", "", StringComparison.Ordinal), ignoreCase: true, out var position)
            && Enum.IsDefined(position))
        {
            return position;
        }

        errors.Add($"{label} \"{value}\" isn't one of: {string.Join(", ", Enum.GetNames<Position>())}.");
        return null;
    }

    private static void AddGuardian(
        string? firstName, string? lastName, string? email, string? relationship, string label, List<GuardianInput> guardians, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName) && string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
        {
            errors.Add($"{label} needs a first name, last name and email.");
            return;
        }

        if (!MailAddress.TryCreate(email.Trim(), out _))
        {
            errors.Add($"{label} email \"{email}\" isn't valid.");
            return;
        }

        guardians.Add(new GuardianInput(firstName, lastName, email, relationship));
    }
}
