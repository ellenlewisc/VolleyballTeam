namespace VolleyballTeam.Infrastructure.Services;

public class BusinessRuleException(string message) : InvalidOperationException(message);
