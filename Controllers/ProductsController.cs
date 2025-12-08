using Microsoft.AspNetCore.Mvc;

namespace CongCuGoiYSanPham.Controllers
{
    public class ProductsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Detail(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }
    }
}
