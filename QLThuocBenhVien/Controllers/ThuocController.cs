using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using QLThuocBenhVien.Models;
using System.Security.Claims;

namespace QLThuocBenhVien.Controllers
{
    public class ThuocController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ThuocController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET: Hiển thị danh sách thuốc (Có Tìm kiếm & Lọc)
        // ==========================================
        public async Task<IActionResult> Index(string searchString, int? maNhomBenh, string trangThai)
        {
            // 1. Lưu lại các giá trị lọc để hiển thị giữ nguyên trên giao diện sau khi load lại
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentNhomBenh"] = maNhomBenh;
            ViewData["CurrentTrangThai"] = trangThai;

            // 2. Đổ dữ liệu ra Dropdown list Nhóm bệnh
            ViewBag.NhomBenhList = new SelectList(await _context.NhomBenh.ToListAsync(), "MaNhomBenh", "TenNhomBenh", maNhomBenh);

            // 3. Lấy toàn bộ Query gốc
            var query = _context.Thuoc
                .Include(t => t.ThuocNhomBenhs)
                .ThenInclude(tnb => tnb.NhomBenh)
                .AsQueryable();

            // --- BẮT ĐẦU LỌC DỮ LIỆU ---

            // A. Lọc theo Tên thuốc
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t => t.TenThuoc.Contains(searchString));
            }

            // B. Lọc theo Nhóm bệnh
            if (maNhomBenh.HasValue)
            {
                query = query.Where(t => t.ThuocNhomBenhs.Any(tnb => tnb.MaNhomBenh == maNhomBenh));
            }

            // C. Lọc theo Trạng thái (Dựa vào số lượng tồn kho thực tế)
            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "HetHang")
                    query = query.Where(t => t.SoLuongTon == 0);
                else if (trangThai == "SapHet")
                    query = query.Where(t => t.SoLuongTon > 0 && t.SoLuongTon <= 50); // Mức cảnh báo sắp hết là <= 50
                else if (trangThai == "AnToan")
                    query = query.Where(t => t.SoLuongTon > 50);
            }

            // Thực thi truy vấn và trả về
            var danhSachThuoc = await query.ToListAsync();
            return View(danhSachThuoc);
        }

        // ==========================================
        // 1. CHỨC NĂNG THÊM MỚI (CREATE)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
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

                // --- BỔ SUNG GHI LOG ---
                var nguoiDung = User.FindFirst("FullName")?.Value ?? "Hệ thống";
                _context.NhatKyHeThong.Add(new NhatKyHeThong
                {
                    ThoiGian = DateTime.Now,
                    Loai = "Info",
                    NoiDung = $"Thêm mới thuốc vào kho: {thuoc.TenThuoc}",
                    NguoiThucHien = nguoiDung
                });
                // -----------------------

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

                    // --- BỔ SUNG GHI LOG ---
                    var nguoiDung = User.FindFirst("FullName")?.Value ?? "Hệ thống";
                    _context.NhatKyHeThong.Add(new NhatKyHeThong
                    {
                        ThoiGian = DateTime.Now,
                        Loai = "Warning",
                        NoiDung = $"Cập nhật thông tin thuốc: {thuoc.TenThuoc}",
                        NguoiThucHien = nguoiDung
                    });
                    // -----------------------

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
                var tenThuocDaXoa = thuoc.TenThuoc; // Lưu lại tên để ghi log
                _context.Thuoc.Remove(thuoc);

                // --- BỔ SUNG GHI LOG ---
                var nguoiDung = User.FindFirst("FullName")?.Value ?? "Hệ thống";
                _context.NhatKyHeThong.Add(new NhatKyHeThong
                {
                    ThoiGian = DateTime.Now,
                    Loai = "Danger",
                    NoiDung = $"Đã xóa thuốc khỏi hệ thống: {tenThuocDaXoa}",
                    NguoiThucHien = nguoiDung
                });
                // -----------------------

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}