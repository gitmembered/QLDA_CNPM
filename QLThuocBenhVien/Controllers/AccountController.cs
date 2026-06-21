using Microsoft.AspNetCore.Mvc;

namespace QLThuocBenhVien.Controllers
{
    public class AccountController : Controller
    {
        // GET: Hiển thị giao diện đăng nhập ban đầu
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Xử lý khi người dùng bấm nút Sign In
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Kiểm tra tài khoản mặc định
            if (username == "admin" && password == "12345")
            {
                // Nếu đúng -> Chuyển hướng sang trang Dashboard (Index của HomeController)
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // Nếu sai -> Báo lỗi và trả lại trang đăng nhập
                ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                return View();
            }
        }
    }
}