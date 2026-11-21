namespace VolleyballTeam.Infrastructure.Auditing;

public class CurrentUser
{
    public int? UserId { get; private set; }

    public void Set(int userId) => UserId = userId;
}
