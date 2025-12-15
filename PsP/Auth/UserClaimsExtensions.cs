    using System.Security.Claims;

    namespace PsP.Auth;

    public static class UserClaimsExtensions
    {
        public static int GetBusinessId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst("businessId")?.Value;
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Missing businessId claim");

            return int.Parse(value);
        }

        public static int GetEmployeeId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst("employeeId")?.Value;
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Missing employeeId claim");

            return int.Parse(value);
        }

        public static string? GetRole(this ClaimsPrincipal user)
            => user.FindFirst(ClaimTypes.Role)?.Value;

        public static bool IsOwnerOrManager(this ClaimsPrincipal user)
        {
            var role = user.GetRole();
            return string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
        }
    }