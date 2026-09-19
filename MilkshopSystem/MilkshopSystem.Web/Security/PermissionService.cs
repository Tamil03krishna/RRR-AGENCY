using System.Security.Claims;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Security
{
    public interface IPermissionService
    {
        bool IsAdmin(ClaimsPrincipal user);

        // permission = "View" | "Add" | "Edit" | "Delete"
        Task<bool> HasAsync(ClaimsPrincipal user, string module, string permission);

        // The first page this user is allowed to open (used right after login).
        // Returns null when the user has not been given access to any page.
        Task<(string Controller, string Action)?> GetLandingAsync(ClaimsPrincipal user);
    }

    public class PermissionService : IPermissionService
    {
        // Order in which we pick the landing page for a non-admin user.
        private static readonly (string Module, string Controller, string Action)[] LandingOrder =
        {
            ("Billing", "Billing", "Index"),
            ("Customer", "Customer", "Index"),
            ("Product", "Product", "Index"),
            ("Category", "Category", "Index"),
            ("Stock", "Stock", "Index"),
            ("Unit", "Unit", "Index"),
            ("PaymentMode", "PaymentMode", "Index")
        };

        private readonly IUserRepository _userRepo;

        // Permissions are read from the DB once per request (the service is scoped),
        // so a change made on the Access page applies on the user's very next click.
        private List<UserPermission>? _cached;
        private int _cachedUserId;

        public PermissionService(IUserRepository userRepo) => _userRepo = userRepo;

        public bool IsAdmin(ClaimsPrincipal user) => user.IsInRole("Admin");

        public async Task<bool> HasAsync(ClaimsPrincipal user, string module, string permission)
        {
            if (IsAdmin(user)) return true;

            var rows = await LoadAsync(user);
            var row = rows.FirstOrDefault(p => string.Equals(p.ModuleName, module, StringComparison.OrdinalIgnoreCase));
            if (row is null) return false;

            return permission switch
            {
                "View" => row.CanView,
                "Add" => row.CanAdd,
                "Edit" => row.CanEdit,
                "Delete" => row.CanDelete,
                _ => false
            };
        }

        public async Task<(string Controller, string Action)?> GetLandingAsync(ClaimsPrincipal user)
        {
            if (IsAdmin(user)) return ("Dashboard", "Index");

            foreach (var (module, controller, action) in LandingOrder)
            {
                if (await HasAsync(user, module, "View")) return (controller, action);
            }
            return null;
        }

        private async Task<List<UserPermission>> LoadAsync(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                return new List<UserPermission>();

            if (_cached is null || _cachedUserId != userId)
            {
                _cached = await _userRepo.GetPermissionsAsync(userId);
                _cachedUserId = userId;
            }
            return _cached;
        }
    }
}
