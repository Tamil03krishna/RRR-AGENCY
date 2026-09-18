using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using MilkshopSystem.Web.Models.Entities;

namespace MilkshopSystem.Web.Models.ViewModels
{
    public class UserFormViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        // Only required when creating a new user; left blank on edit means "keep the current password"
        public string? Password { get; set; }

        [Required]
        public int RoleId { get; set; }

        public bool IsActive { get; set; } = true;

        public List<SelectListItem> Roles { get; set; } = new();
    }

    public class ChangePasswordViewModel
    {
        public int UserId { get; set; }
        public string? Username { get; set; }

        // Only asked when a user changes their OWN password (not when an Admin resets someone else's)
        public string? CurrentPassword { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UserAccessViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<ModulePermissionRow> Permissions { get; set; } = new();
    }

    public class ModulePermissionRow
    {
        public string ModuleName { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
