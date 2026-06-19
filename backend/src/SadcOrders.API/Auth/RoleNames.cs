namespace SadcOrders.API.Auth;

public static class RoleNames
{
    public const string OrderAdmin = "OrderAdmin";
    public const string OrderReader = "OrderReader";

    public const string ReadAccess = $"{OrderAdmin},{OrderReader}";
}
