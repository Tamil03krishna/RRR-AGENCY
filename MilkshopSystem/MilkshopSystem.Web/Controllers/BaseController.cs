using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MilkshopSystem.Web.Controllers
{
    // Every module controller inherits this so the whole app requires login,
    // except AccountController (login page) which is marked [AllowAnonymous].
    [Authorize]
    public class BaseController : Controller
    {
    }
}
