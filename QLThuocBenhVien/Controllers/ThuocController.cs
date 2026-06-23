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
        // 1. CHỨC NĂNG DANH SÁCH & LỌC (INDEX)
        // ==========================================
        public async Task<IActionResult> Index(string searchString, int? maNhomBenh, string trangThai)
        {
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentTrangThai"] = trangThai;
            ViewBag.NhomBenhList = new SelectList(await _context.NhomBenh.ToListAsync(), "MaNhomBenh", "TenNhomBenh", maNhomBenh);

            var query = _context.Thuoc
                .Include(t => t.DonViTinh)
                .Include(t => t.ThuocNhomBenhs).ThenInclude(tn => tn.NhomBenh)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string key = searchString.Trim().ToLower();
                query = query.Where(t => t.TenThuoc.ToLower().Contains(key) || t.HoatChat.ToLower().Contains(key));
            }

            if (maNhomBenh.HasValue && maNhomBenh.Value > 0)
            {
                query = query.Where(t => t.ThuocNhomBenhs.Any(tn => tn.MaNhomBenh == maNhomBenh.Value));
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                var today = DateTime.Now.Date;
                var baThangToi = today.AddMonths(3);

                switch (trangThai)
                {
                    case "AnToan":
                        query = query.Where(t => t.SoLuongTon > 50 && t.HanSuDung > baThangToi);
                        break;
                    case "SapHetHang":
                        query = query.Where(t => t.SoLuongTon > 0 && t.SoLuongTon <= 50);
                        break;
                    case "HetHang":
                        query = query.Where(t => t.SoLuongTon == 0);
                        break;
                    case "SapHetHan":
                        query = query.Where(t => t.HanSuDung >= today && t.HanSuDung <= baThangToi);
                        break;
                    case "DaHetHan":
                        query = query.Where(t => t.HanSuDung < today);
                        break;
                }
            }

            return View(await query.OrderByDescending(t => t.MaThuoc).ToListAsync());
        }

        // ==========================================
        // 2. CHỨC NĂNG THÊM MỚI (CREATE)
        // ==========================================
        public IActionResult Create()
        {
            ViewBag.DonViTinhList = new SelectList(_context.DonViTinh.ToList(), "MaDVT", "TenDVT");
            ViewBag.NhomBenhList = new SelectList(_context.NhomBenh.ToList(), "MaNhomBenh", "TenNhomBenh");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Thuoc thuoc, int MaNhomBenh)
        {
            // Tắt bắt lỗi ngầm cho các trường liên kết và trường không nhập trên Form Create
            ModelState.Remove("DonViTinh");
            ModelState.Remove("ThuocNhomBenhs");
            ModelState.Remove("NhaCungCap");
            ModelState.Remove("ChiTietPhieuNhaps");
            ModelState.Remove("ChiTietPhieuXuats");
            ModelState.Remove("MaNCC");

            if (ModelState.IsValid)
            {
                thuoc.SoLuongTon = 0;
                _context.Add(thuoc);
                await _context.SaveChangesAsync();

                if (MaNhomBenh > 0)
                {
                    _context.ThuocNhomBenh.Add(new ThuocNhomBenh
                    {
                        MaThuoc = thuoc.MaThuoc,
                        MaNhomBenh = MaNhomBenh
                    });
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Khai báo dược phẩm mới thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DonViTinhList = new SelectList(_context.DonViTinh.ToList(), "MaDVT", "TenDVT");
            ViewBag.NhomBenhList = new SelectList(_context.NhomBenh.ToList(), "MaNhomBenh", "TenNhomBenh");
            return View(thuoc);
        }

        // ==========================================
        // 3. CHỨC NĂNG CHỈNH SỬA (EDIT)
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var thuoc = await _context.Thuoc
                .Include(t => t.ThuocNhomBenhs)
                .FirstOrDefaultAsync(m => m.MaThuoc == id);

            if (thuoc == null) return NotFound();

            int currentNhomBenhId = thuoc.ThuocNhomBenhs?.FirstOrDefault()?.MaNhomBenh ?? 0;

            ViewBag.DonViTinhList = new SelectList(await _context.DonViTinh.ToListAsync(), "MaDVT", "TenDVT", thuoc.MaDVT);
            ViewBag.NhomBenhList = new SelectList(await _context.NhomBenh.ToListAsync(), "MaNhomBenh", "TenNhomBenh", currentNhomBenhId);

            return View(thuoc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Thuoc thuoc, int MaNhomBenh)
        {
            if (id != thuoc.MaThuoc) return NotFound();

            // ĐÃ FIX TRIỆT ĐỂ: Tắt validation cho toàn bộ các trường không có mặt trên giao diện
            ModelState.Remove("DonViTinh");
            ModelState.Remove("ThuocNhomBenhs");
            ModelState.Remove("NhaCungCap");
            ModelState.Remove("ChiTietPhieuNhaps");
            ModelState.Remove("ChiTietPhieuXuats");
            ModelState.Remove("MaNCC");
            ModelState.Remove("SoLuongTon");

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy thuốc từ CSDL lên để bảo toàn Số lượng tồn và các trường ẩn
                    var thuocDb = await _context.Thuoc.FindAsync(id);
                    if (thuocDb == null) return NotFound();

                    // Chỉ ghi đè những trường có hiển thị cho phép sửa trên giao diện
                    thuocDb.TenThuoc = thuoc.TenThuoc;
                    thuocDb.HoatChat = thuoc.HoatChat;
                    thuocDb.MaDVT = thuoc.MaDVT;
                    thuocDb.CongDung = thuoc.CongDung;
                    thuocDb.GiaNhap = thuoc.GiaNhap;
                    thuocDb.GiaBan = thuoc.GiaBan;
                    thuocDb.HanSuDung = thuoc.HanSuDung;

                    // Cập nhật lại Nhóm Bệnh
                    var oldNhomBenhs = _context.ThuocNhomBenh.Where(tn => tn.MaThuoc == id).ToList();
                    _context.ThuocNhomBenh.RemoveRange(oldNhomBenhs);

                    if (MaNhomBenh > 0)
                    {
                        _context.ThuocNhomBenh.Add(new ThuocNhomBenh
                        {
                            MaThuoc = id,
                            MaNhomBenh = MaNhomBenh
                        });
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật thành công thông tin thuốc {thuocDb.TenThuoc}!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Thuoc.Any(e => e.MaThuoc == thuoc.MaThuoc)) return NotFound();
                    else throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu: " + ex.Message);
                }
            }

            ViewBag.DonViTinhList = new SelectList(_context.DonViTinh.ToList(), "MaDVT", "TenDVT", thuoc.MaDVT);
            ViewBag.NhomBenhList = new SelectList(_context.NhomBenh.ToList(), "MaNhomBenh", "TenNhomBenh", MaNhomBenh);
            return View(thuoc);
        }

        // ==========================================
        // 4. CHỨC NĂNG XÓA (DELETE)
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var thuoc = await _context.Thuoc.FindAsync(id);
                if (thuoc != null)
                {
                    // Xóa liên kết nhóm bệnh trước
                    var danhSachLienKet = _context.ThuocNhomBenh.Where(tn => tn.MaThuoc == id).ToList();
                    if (danhSachLienKet.Any())
                    {
                        _context.ThuocNhomBenh.RemoveRange(danhSachLienKet);
                    }

                    // Sau đó mới xóa thuốc
                    _context.Thuoc.Remove(thuoc);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Đã xóa thuốc {thuoc.TenThuoc} khỏi hệ thống thành công!";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // Bắt lỗi khi thuốc đã được nhập/xuất kho (ràng buộc khóa ngoại)
                TempData["ErrorMessage"] = "Không thể xóa! Thuốc này đã phát sinh chứng từ Nhập/Xuất kho. Vui lòng giữ lại để đảm bảo lịch sử đối soát kế toán.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}