using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongCuGoiYSanPham.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InventoryViewController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/Inventory/Index.cshtml");
        }
    }
}