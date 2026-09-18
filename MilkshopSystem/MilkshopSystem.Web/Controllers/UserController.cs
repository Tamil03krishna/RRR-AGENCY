using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : BaseController
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;

        public UserController(IUserRepository userRepo, IRoleRepository roleRepo)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
        }

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var result = await _userRepo.GetPagedAsync(search, page, pageSize);
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user is null) return NotFound();
            return View(user);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new UserFormViewModel { Roles = await GetRoleOptions() };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel vm)
        {
            vm.Roles = await GetRoleOptions();

            if (string.IsNullOrWhiteSpace(vm.Password))
                ModelState.AddModelError(nameof(vm.Password), "Password is required for a new user.");

            if (await _userRepo.UsernameExistsAsync(vm.Username))
                ModelState.AddModelError(nameof(vm.Username), "This username is already taken.");

            if (!ModelState.IsValid) return View(vm);

            var role = await _roleRepo.GetByIdAsync(vm.RoleId);

            var user = new User
            {
                Username = vm.Username,
                FullName = vm.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password),
                Role = role?.Name ?? "Staff",
                RoleId = vm.RoleId,
                IsActive = vm.IsActive
            };

            await _userRepo.CreateAsync(user);
            TempData["Success"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user is null) return NotFound();

            var vm = new UserFormViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                RoleId = user.RoleId ?? 0,
                IsActive = user.IsActive,
                Roles = await GetRoleOptions()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserFormViewModel vm)
        {
            vm.Roles = await GetRoleOptions();
            ModelState.Remove(nameof(vm.Password)); // password is optional on edit

            if (!ModelState.IsValid) return View(vm);

            var role = await _roleRepo.GetByIdAsync(vm.RoleId);

            var user = new User
            {
                Id = vm.Id,
                FullName = vm.FullName,
                Role = role?.Name ?? "Staff",
                RoleId = vm.RoleId,
                IsActive = vm.IsActive
            };

            await _userRepo.UpdateAsync(user);
            TempData["Success"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Admin resetting another user's password (no current-password check needed)
        public async Task<IActionResult> ChangePassword(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user is null) return NotFound();

            return View(new ChangePasswordViewModel { UserId = user.Id, Username = user.Username });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var hash = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
            await _userRepo.UpdatePasswordAsync(vm.UserId, hash);

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Per-module Add/Edit/Delete/View access checkboxes for this user
        public async Task<IActionResult> Access(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user is null) return NotFound();

            var existing = await _userRepo.GetPermissionsAsync(id);

            var vm = new UserAccessViewModel
            {
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Permissions = AppModules.All.Select(m =>
                {
                    var match = existing.FirstOrDefault(p => p.ModuleName == m);
                    return new ModulePermissionRow
                    {
                        ModuleName = m,
                        CanView = match?.CanView ?? false,
                        CanAdd = match?.CanAdd ?? false,
                        CanEdit = match?.CanEdit ?? false,
                        CanDelete = match?.CanDelete ?? false
                    };
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Access(UserAccessViewModel vm)
        {
            var permissions = vm.Permissions.Select(p => new UserPermission
            {
                UserId = vm.UserId,
                ModuleName = p.ModuleName,
                CanView = p.CanView,
                CanAdd = p.CanAdd,
                CanEdit = p.CanEdit,
                CanDelete = p.CanDelete
            }).ToList();

            await _userRepo.SavePermissionsAsync(vm.UserId, permissions);
            TempData["Success"] = "Access permissions updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetRoleOptions()
        {
            var roles = await _roleRepo.GetAllActiveAsync();
            return roles.Select(r => new SelectListItem(r.Name, r.Id.ToString())).ToList();
        }
    }
}
