namespace VolleyballTeam.Infrastructure.Tenancy;

public class TenantViolationException(string message) : InvalidOperationException(message);
