namespace SadcOrders.API.Auth;

public class DevAuthOptions
{
    public const string SectionName = "DevAuth";

    public bool Enabled { get; set; } = true;

    public List<DevAuthUser> Users { get; set; } = [];
}

public class DevAuthUser
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];
}
