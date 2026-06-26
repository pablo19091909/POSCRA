namespace PulperiaPOS.Security
{
    public static class PermissionHelper
    {
        public static bool HasPermission(string permission) => UserSession.HasPermission(permission);
    }
}
