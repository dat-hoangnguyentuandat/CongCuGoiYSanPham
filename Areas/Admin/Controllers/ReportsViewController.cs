using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongCuGoiYSanPham.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportsViewController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/Reports/Index.cshtml");
        }
    }
}