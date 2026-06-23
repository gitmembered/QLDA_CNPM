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

        // GET: Thuoc/Index
        // Thêm các tham số để hứng dữ liệu từ Form tìm kiếm gửi lên
        // GET: Thuoc/Index
        public async Task<IActionResult> Index(string searchString, int? maNhomBenh, string trangThai)
        {
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentTrangThai"] = trangThai;
            ViewBag.NhomBenhList = new SelectList(await _context.NhomBenh.ToListAsync(), "MaNhomBenh", "TenNhomBenh", maNhomBenh);

            var query = _context.Thuoc
                .Include(t => t.DonViTinh)
                .Include(t => t.ThuocNhomBenhs).ThenInclude(tn => tn.NhomBenh)
                .AsQueryable();

            // SỬA TẠI ĐÂY: Ép về ToLower() để tìm kiếm chuẩn xác trên Server nếu form bị submit
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
        // 1. CHỨC NĂNG THÊM MỚI (CREATE)
        // ==========================================
        // GET: Thuoc/Create
        public IActionResult Create()
        {
            // Lấy danh sách từ CSDL gửi ra giao diện để đưa vào Dropdown
            ViewBag.DonViTinhList = new SelectList(_context.DonViTinh.ToList(), "MaDVT", "TenDVT");
            ViewBag.NhomBenhList = new SelectList(_context.NhomBenh.ToList(), "MaNhomBenh", "TenNhomBenh");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Thuoc thuoc, int MaNhomBenh) // Thêm tham số MaNhomBenh vào đây
        {
            if (ModelState.IsValid)
            {
                thuoc.SoLuongTon = 0; // Mặc định tồn kho bằng 0 khi khai báo mới
                _context.Add(thuoc);
                await _context.SaveChangesAsync(); // Lưu để EF Core sinh ra MaThuoc tự động

                // Sau khi có MaThuoc, tiến hành lưu liên kết Nhóm Bệnh
                if (MaNhomBenh > 0)
                {
                    _context.ThuocNhomBenh.Add(new ThuocNhomBenh
                    {
                        MaThuoc = thuoc.MaThuoc,
                        MaNhomBenh = MaNhomBenh
                    });
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi, load lại danh sách cho Dropdown
            ViewBag.DonViTinhList = new SelectList(_context.DonViTinh.ToList(), "MaDVT", "TenDVT");
            ViewBag.NhomBenhList = new SelectList(_context.NhomBenh.ToList(), "MaNhomBenh", "TenNhomBenh");
            return View(thuoc);
        }
        // ==========================================
        // 2. CHỨC NĂNG CHỈNH SỬA (EDIT)
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var thuoc = await _context.Thuoc
                .Include(t => t.ThuocNhomBenhs)
                .FirstOrDefaultAsync(m => m.MaThuoc == id);

            if (thuoc == null) return NotFound();

            // Lấy ID nhóm bệnh hiện tại để hiển thị sẵn trên Dropdown
            int currentNhomBenhId = thuoc.ThuocNhomBenhs?.FirstOrDefault()?.MaNhomBenh ?? 0;

            ViewBag.DonViTinhList = new SelectList(await _context.DonViTinh.ToListAsync(), "MaDVT", "TenDVT", thuoc.MaDVT);
            ViewBag.NhomBenhList = new SelectList(await _context.NhomBenh.ToListAsync(), "MaNhomBenh", "TenNhomBenh", currentNhomBenhId);

            return View(thuoc);
        }

        // POST: Thuoc/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Thuoc thuoc, int MaNhomBenh)
        {
            if (id != thuoc.MaThuoc) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Cập nhật thông tin cơ bản của thuốc
                    _context.Update(thuoc);

                    // 2. Cập nhật Nhóm Bệnh Khuyên Dùng (Xóa cũ, Thêm mới)
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
                    TempData["SuccessMessage"] = "Cập nhật thông tin thuốc thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Thuoc.Any(e => e.MaThuoc == thuoc.MaThuoc)) return NotFound();
                    else throw;
                }
            }

            ViewBag.DonViTinhList = new SelectList(_context.DonViTinh.ToList(), "MaDVT", "TenDVT", thuoc.MaDVT);
            ViewBag.NhomBenhList = new SelectList(_context.NhomBenh.ToList(), "MaNhomBenh", "TenNhomBenh", MaNhomBenh);
            return View(thuoc);
        }

        // ==========================================
        // 3. CHỨC NĂNG XÓA (DELETE)
        // ==========================================
        // POST: Thuoc/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var thuoc = await _context.Thuoc.FindAsync(id);
                if (thuoc != null)
                {
                    // 1. XÓA BẢNG CON TRƯỚC: Tìm và xóa các liên kết Nhóm Bệnh của thuốc này
                    var danhSachLienKet = _context.ThuocNhomBenh.Where(tn => tn.MaThuoc == id).ToList();
                    if (danhSachLienKet.Any())
                    {
                        _context.ThuocNhomBenh.RemoveRange(danhSachLienKet);
                    }

                    // 2. XÓA BẢNG CHA: Sau khi bảng con đã dọn sạch, tiến hành xóa Thuốc
                    _context.Thuoc.Remove(thuoc);

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã xóa thuốc {thuoc.TenThuoc} khỏi hệ thống thành công!";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // 3. XỬ LÝ NGOẠI LỆ THỰC TẾ (RẤT QUAN TRỌNG)
                // Nếu thuốc này ĐÃ TỪNG ĐƯỢC LẬP PHIẾU NHẬP hoặc PHIẾU XUẤT, SQL Server sẽ tiếp tục chặn không cho xóa để bảo vệ dữ liệu kế toán.
                // Thay vì sập trang web (như ảnh bạn chụp), ta bắt lỗi và báo ra màn hình cho người dùng biết.

                TempData["ErrorMessage"] = "Không thể xóa! Thuốc này đã phát sinh chứng từ Nhập/Xuất kho. Vui lòng giữ lại để đảm bảo lịch sử đối soát kế toán.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}