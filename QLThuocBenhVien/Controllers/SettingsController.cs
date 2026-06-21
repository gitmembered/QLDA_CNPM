using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;

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
            var danhSachNhanVien = await _context.TaiKhoan
                                        .Where(tk => tk.VaiTro != "Admin")
                                        .ToListAsync();

            // Lấy 50 dòng log mới nhất
            ViewBag.Logs = await _context.NhatKyHeThong
                                        .OrderByDescending(l => l.ThoiGian)
                                        .Take(50)
                                        .ToListAsync();

            return View(danhSachNhanVien);
        }
    }
}