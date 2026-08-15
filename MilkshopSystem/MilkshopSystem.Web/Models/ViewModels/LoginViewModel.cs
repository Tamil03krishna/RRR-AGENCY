using System.ComponentModel.DataAnnotations;

namespace MilkshopSystem.Web.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username வேணும்")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password வேணும்")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
