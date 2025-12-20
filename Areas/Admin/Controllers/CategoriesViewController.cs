using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongCuGoiYSanPham.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesViewController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/Categories/Index.cshtml");
        }
    }
}