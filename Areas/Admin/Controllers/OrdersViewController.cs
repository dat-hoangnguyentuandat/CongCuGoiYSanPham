using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongCuGoiYSanPham.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersViewController : Controller
    {
        public IActionResult Index()
        {
            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Check if user has Admin role
            if (!User.IsInRole("Admin"))
            {
                return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });
            }

            return View("~/Areas/Admin/Views/OrdersView/Index.cshtml");
        }

        public IActionResult Detail(int id)
        {
            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Check if user has Admin role
            if (!User.IsInRole("Admin"))
            {
                return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });
            }

            ViewBag.OrderId = id;
            return View();
        }
    }
}