using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using QLThuocBenhVien.Models;

namespace QLThuocBenhVien.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Settings
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách tài khoản KHÔNG PHẢI là Admin để quản lý
            var danhSachNhanVien = await _context.TaiKhoan
                                        .Where(tk => tk.VaiTro != "Admin")
                                        .ToListAsync();

            ViewBag.Logs = await _context.NhatKyHeThong
                                        .OrderByDescending(l => l.ThoiGian)
                                        .Take(50)
                                        .ToListAsync();

            return View(danhSachNhanVien);
        }

        // POST: Thêm tài khoản mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(TaiKhoan user)
        {
            // Giữ cho trang không bị nhảy Tab sau khi reload
            TempData["ActiveTab"] = "users-panel";

            if (!string.IsNullOrEmpty(user.TenDangNhap) && !string.IsNullOrEmpty(user.MatKhau))
            {
                // Kiểm tra tên đăng nhập đã tồn tại chưa
                var exists = await _context.TaiKhoan.AnyAsync(x => x.TenDangNhap == user.TenDangNhap);
                if (exists)
                {
                    TempData["ErrorMessage"] = "Tên đăng nhập này đã tồn tại trong hệ thống!";
                    return RedirectToAction(nameof(Index));
                }

                // ĐÃ SỬA: Admin thêm mới thì tài khoản mặc định được Hoạt động (Trạng thái = 1)
                user.TrangThai = 1;
                _context.TaiKhoan.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã thêm tài khoản {user.TenDangNhap} thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Sửa quyền / Khóa tài khoản
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ĐÃ SỬA: Tham số trangThai chuyển từ kiểu bool sang int
        public async Task<IActionResult> EditUserRole(int maTaiKhoan, string vaiTro, int trangThai)
        {
            TempData["ActiveTab"] = "users-panel";

            var user = await _context.TaiKhoan.FindAsync(maTaiKhoan);
            if (user != null)
            {
                user.VaiTro = vaiTro;
                user.TrangThai = trangThai;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Cập nhật quyền cho tài khoản {user.TenDangNhap} thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}