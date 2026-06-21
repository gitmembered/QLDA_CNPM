using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using QLThuocBenhVien.Models;
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
        // ==========================================
        // 1. CHỨC NĂNG THÊM MỚI (CREATE)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            // Đổ dữ liệu vào Dropdown chọn Nhà Cung Cấp và Đơn Vị Tính
            ViewBag.MaNCC = new SelectList(_context.NhaCungCap, "MaNCC", "TenNCC");
            ViewBag.MaDVT = new SelectList(_context.DonViTinh, "MaDVT", "TenDVT");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Thuoc thuoc)
        {
            if (ModelState.IsValid)
            {
                _context.Add(thuoc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.MaNCC = new SelectList(_context.NhaCungCap, "MaNCC", "TenNCC", thuoc.MaNCC);
            ViewBag.MaDVT = new SelectList(_context.DonViTinh, "MaDVT", "TenDVT", thuoc.MaDVT);
            return View(thuoc);
        }

        // ==========================================
        // 2. CHỨC NĂNG CHỈNH SỬA (EDIT)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var thuoc = await _context.Thuoc.FindAsync(id);
            if (thuoc == null) return NotFound();

            ViewBag.MaNCC = new SelectList(_context.NhaCungCap, "MaNCC", "TenNCC", thuoc.MaNCC);
            ViewBag.MaDVT = new SelectList(_context.DonViTinh, "MaDVT", "TenDVT", thuoc.MaDVT);
            return View(thuoc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Thuoc thuoc)
        {
            if (id != thuoc.MaThuoc) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(thuoc);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Thuoc.Any(e => e.MaThuoc == thuoc.MaThuoc)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.MaNCC = new SelectList(_context.NhaCungCap, "MaNCC", "TenNCC", thuoc.MaNCC);
            ViewBag.MaDVT = new SelectList(_context.DonViTinh, "MaDVT", "TenDVT", thuoc.MaDVT);
            return View(thuoc);
        }

        // ==========================================
        // 3. CHỨC NĂNG XÓA (DELETE)
        // ==========================================
        public async Task<IActionResult> Delete(int id)
        {
            var thuoc = await _context.Thuoc.FindAsync(id);
            if (thuoc != null)
            {
                _context.Thuoc.Remove(thuoc);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}