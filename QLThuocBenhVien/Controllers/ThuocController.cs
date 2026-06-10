using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QLThuocBenhVien.Controllers
{
    public class ThuocController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ThuocController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Hiển thị danh sách thuốc (có hỗ trợ lọc theo nhóm bệnh)
        public async Task<IActionResult> Index(int? maNhomBenh)
        {
            // Đổ dữ liệu ra Dropdown list để lọc
            ViewBag.NhomBenhList = new SelectList(_context.NhomBenh, "MaNhomBenh", "TenNhomBenh");

            var query = _context.Thuoc
                .Include(t => t.ThuocNhomBenhs)
                .ThenInclude(tnb => tnb.NhomBenh)
                .AsQueryable();

            if (maNhomBenh.HasValue)
            {
                // Lọc những thuốc thuộc nhóm bệnh được chọn
                query = query.Where(t => t.ThuocNhomBenhs.Any(tnb => tnb.MaNhomBenh == maNhomBenh));
            }

            var danhSachThuoc = await query.ToListAsync();
            return View(danhSachThuoc);
        }
    }
}