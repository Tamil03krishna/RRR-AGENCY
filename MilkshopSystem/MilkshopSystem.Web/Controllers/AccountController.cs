using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;
using MilkshopSystem.Web.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MilkshopSystem.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _config;
        private readonly IPermissionService _permissions;

        public AccountController(IUserRepository userRepository, IConfiguration config, IPermissionService permissions)
        {
            _userRepository = userRepository;
            _config = config;
            _permissions = permissions;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction(nameof(Home));

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userRepository.GetByUsernameAsync(model.Username);
            if (user is null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Username or password is wrong. Please try again.");
                return View(model);
            }

            var expiryMinutes = _config.GetValue<int?>("Jwt:ExpiryMinutes") ?? 480;
            var expires = model.RememberMe
                ? DateTime.UtcNow.AddDays(30)
                : DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = GenerateJwtToken(user.Id, user.Username, user.FullName, user.Role, expires);

            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = expires
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Home));
        }

        // Landing page after login: Admin -> Dashboard, everyone else -> the first page
        // they were given View access to (e.g. only Invoices). No access at all -> NoAccess page.
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Home()
        {
            var target = await _permissions.GetLandingAsync(User);
            if (target is null) return View("NoAccess");

            return RedirectToAction(target.Value.Action, target.Value.Controller);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token");
            return RedirectToAction("Login");
        }

        // Self-service: the currently logged-in user changes their own password
        // (requires the current password, unlike an Admin resetting someone else's).
        [Authorize]
        [HttpGet]
        public IActionResult MyPassword()
        {
            var username = User.FindFirst("Username")?.Value;
            return View(new ChangePasswordViewModel { Username = username });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyPassword(ChangePasswordViewModel vm)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId)) return Forbid();

            var user = await _userRepository.GetByIdAsync(userId);
            if (user is null) return NotFound();

            if (string.IsNullOrWhiteSpace(vm.CurrentPassword) || !BCrypt.Net.BCrypt.Verify(vm.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError(nameof(vm.CurrentPassword), "Current password is incorrect.");
            }

            if (!ModelState.IsValid)
            {
                vm.Username = user.Username;
                return View(vm);
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
            await _userRepository.UpdatePasswordAsync(userId, hash);

            TempData["Success"] = "Your password has been changed successfully.";
            return RedirectToAction("Index", "Dashboard");
        }

        private string GenerateJwtToken(int userId, string username, string fullName, string role, DateTime expires)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim("Username", username),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
