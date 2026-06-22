using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using QLThuocBenhVien.Models;

namespace QLThuocBenhVien.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // Dùng để xác định thư mục wwwroot

        public AccountController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpGet("Account/Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // ĐÃ CẬP NHẬT LOGIC KIỂM TRA 3 TRẠNG THÁI (INT)
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // 1. Chỉ kiểm tra Tên đăng nhập và Mật khẩu trước
            var user = await _context.TaiKhoan
                .FirstOrDefaultAsync(t => t.TenDangNhap == username && t.MatKhau == password);

            if (user != null)
            {
                // 2. Nếu thông tin đúng, kiểm tra tiếp trạng thái (1 = Hoạt động)
                if (user.TrangThai == 1)
                {
                    await SignInUserAsync(user); // Gọi hàm tạo Cookie
                    return RedirectToAction("Index", "Home");
                }
                else if (user.TrangThai == 0) // 0 = Chờ duyệt
                {
                    ViewBag.ErrorMessage = "Tài khoản của bạn đang chờ Quản trị viên phê duyệt!";
                    return View();
                }
                else // 2 = Bị khóa
                {
                    ViewBag.ErrorMessage = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin!";
                    return View();
                }
            }

            // Nếu user = null nghĩa là sai tên hoặc sai mật khẩu
            ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var currentUsername = User.Identity.Name;
            var taiKhoan = await _context.TaiKhoan.FirstOrDefaultAsync(tk => tk.TenDangNhap == currentUsername);
            if (taiKhoan == null) return NotFound();
            return View(taiKhoan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(int id, string hoTen, IFormFile? avatarFile)
        {
            var taiKhoan = await _context.TaiKhoan.FindAsync(id);
            if (taiKhoan != null && !string.IsNullOrEmpty(hoTen))
            {
                taiKhoan.HoTen = hoTen;

                // Xử lý Upload Avatar
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + avatarFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatarFile.CopyToAsync(fileStream);
                    }

                    taiKhoan.Avatar = "/uploads/avatars/" + uniqueFileName;
                }

                await _context.SaveChangesAsync();
                await SignInUserAsync(taiKhoan);

                TempData["SuccessMsg"] = "Đã cập nhật thông tin thành công!";
            }
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int id, string oldPassword, string newPassword)
        {
            var taiKhoan = await _context.TaiKhoan.FindAsync(id);
            if (taiKhoan != null && taiKhoan.MatKhau == oldPassword && newPassword.Length >= 5)
            {
                taiKhoan.MatKhau = newPassword;
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Đã đổi mật khẩu thành công!";
            }
            else
            {
                TempData["ErrorMsg"] = "Mật khẩu cũ không đúng hoặc mật khẩu mới quá ngắn!";
            }
            return RedirectToAction(nameof(Profile));
        }

        // Hàm hỗ trợ tạo/cập nhật Cookie dùng chung cho cả Login và UpdateProfile
        private async Task SignInUserAsync(TaiKhoan user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.TenDangNhap),
                new Claim(ClaimTypes.Role, user.VaiTro),
                new Claim("FullName", user.HoTen),
                new Claim("UserId", user.MaTaiKhoan.ToString()),
                new Claim("Avatar", user.Avatar ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        // ================= ĐĂNG KÝ TÀI KHOẢN =================
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string tenDangNhap, string matKhau, string hoTen)
        {
            // 1. Kiểm tra trùng lặp tên đăng nhập
            if (await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap == tenDangNhap))
            {
                ViewBag.ErrorMessage = "Tên đăng nhập này đã có người sử dụng!";
                return View();
            }

            // 2. Tạo tài khoản mới (Mặc định: Dược Sĩ & Chờ Admin duyệt - Trạng thái số 0)
            var newAccount = new TaiKhoan
            {
                TenDangNhap = tenDangNhap,
                MatKhau = matKhau,
                HoTen = hoTen,
                VaiTro = "DuocSi", // Phân quyền thấp nhất mặc định
                TrangThai = 0  // 0 = Mới đăng ký / Chờ duyệt
            };

            _context.TaiKhoan.Add(newAccount);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng chờ Quản trị viên phê duyệt tài khoản.";
            return RedirectToAction("Login");
        }

        // ================= QUÊN MẬT KHẨU =================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string tenDangNhap, string hoTen, string newPassword)
        {
            // Xác thực kép: Yêu cầu nhập đúng cả Tên đăng nhập và Họ tên hiển thị
            var user = await _context.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap && t.HoTen == hoTen);

            if (user == null)
            {
                ViewBag.ErrorMessage = "Thông tin xác thực không khớp với bất kỳ tài khoản nào trong hệ thống!";
                return View();
            }

            // Tiến hành đổi mật khẩu
            user.MatKhau = newPassword;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Khôi phục mật khẩu thành công! Vui lòng đăng nhập lại.";
            return RedirectToAction("Login");
        }
    }
}