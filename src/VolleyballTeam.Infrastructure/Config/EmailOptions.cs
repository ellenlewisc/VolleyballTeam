namespace VolleyballTeam.Infrastructure.Config;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string ConnectionString { get; set; } = "";
    public string SenderAddress { get; set; } = "";

    // Development only: without Azure Communication Services, write the email (including its links) to the log.
    public bool LogBodyWhenNotConfigured { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString) && !string.IsNullOrWhiteSpace(SenderAddress);
}
