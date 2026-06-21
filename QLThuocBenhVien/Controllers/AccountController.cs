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
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.TaiKhoan
                .FirstOrDefaultAsync(t => t.TenDangNhap == username && t.MatKhau == password && t.TrangThai == true);

            if (user != null)
            {
                await SignInUserAsync(user); // Gọi hàm tạo Cookie
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ErrorMessage = "Tài khoản không tồn tại, sai mật khẩu hoặc đã bị khóa!";
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
        // Thêm tham số IFormFile để nhận file ảnh từ giao diện
        public async Task<IActionResult> UpdateProfile(int id, string hoTen, IFormFile? avatarFile)
        {
            var taiKhoan = await _context.TaiKhoan.FindAsync(id);
            if (taiKhoan != null && !string.IsNullOrEmpty(hoTen))
            {
                taiKhoan.HoTen = hoTen;

                // Xử lý Upload Avatar
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    // Tạo thư mục wwwroot/uploads/avatars nếu chưa có
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    // Tạo tên file ngẫu nhiên để không bị trùng
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + avatarFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Lưu file vào thư mục
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatarFile.CopyToAsync(fileStream);
                    }

                    taiKhoan.Avatar = "/uploads/avatars/" + uniqueFileName;
                }

                await _context.SaveChangesAsync();

                // RẤT QUAN TRỌNG: Cập nhật lại Cookie ngay lập tức để Header đổi tên/ảnh
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
                new Claim("Avatar", user.Avatar ?? "") // Bổ sung claim Avatar
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }
    }
}