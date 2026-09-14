using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MilkshopSystem.Web.Controllers
{
   
    [Authorize]
    public class BaseController : Controller
    {
    }
}
