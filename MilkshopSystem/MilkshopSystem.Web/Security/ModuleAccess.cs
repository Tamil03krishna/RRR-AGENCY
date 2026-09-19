using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MilkshopSystem.Web.Security
{
    /// <summary>
    /// Marks a controller (or a single action) as belonging to a module on the Manage Access page.
    ///
    ///   [ModuleAccess("Billing")]            -> the needed permission is worked out from the action name:
    ///                                           Create*/Add* = Add, Edit*/Update* = Edit,
    ///                                           Delete*/Cancel*/Remove* = Delete, everything else = View
    ///   [ModuleAccess("Billing", "Add")]     -> force one specific permission (also lets an action
    ///                                           borrow another module's rule, e.g. the New Bill page's
    ///                                           customer/product search endpoints)
    ///
    /// An attribute on the action wins over the one on the controller.
    /// The check itself is done by <see cref="ModuleAccessFilter"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ModuleAccessAttribute : Attribute
    {
        public string Module { get; }
        public string? Permission { get; }

        public ModuleAccessAttribute(string module, string? permission = null)
        {
            Module = module;
            Permission = permission;
        }
    }

    public sealed class ModuleAccessFilter : IAsyncAuthorizationFilter
    {
        private readonly IPermissionService _permissions;
        private readonly ITempDataDictionaryFactory _tempDataFactory;

        public ModuleAccessFilter(IPermissionService permissions, ITempDataDictionaryFactory tempDataFactory)
        {
            _permissions = permissions;
            _tempDataFactory = tempDataFactory;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Endpoint metadata is ordered controller -> action, so the last one is the most specific.
            var rule = context.ActionDescriptor.EndpointMetadata.OfType<ModuleAccessAttribute>().LastOrDefault();
            if (rule is null) return;

            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true) return; // [Authorize] sends them to the login page

            var actionName = (context.ActionDescriptor as ControllerActionDescriptor)?.ActionName ?? string.Empty;
            var permission = rule.Permission ?? InferPermission(actionName);

            if (await _permissions.HasAsync(user, rule.Module, permission)) return;

            var request = context.HttpContext.Request;
            var isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest"
                         || request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
            if (isAjax)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            _tempDataFactory.GetTempData(context.HttpContext)["Error"] = "You do not have access to that page.";
            context.Result = new RedirectToActionResult("Home", "Account", null);
        }

        private static string InferPermission(string actionName)
        {
            if (StartsWithAny(actionName, "Delete", "Cancel", "Remove")) return "Delete";
            if (StartsWithAny(actionName, "Create", "Add")) return "Add";
            if (StartsWithAny(actionName, "Edit", "Update")) return "Edit";
            return "View";
        }

        private static bool StartsWithAny(string value, params string[] prefixes)
            => prefixes.Any(p => value.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}
