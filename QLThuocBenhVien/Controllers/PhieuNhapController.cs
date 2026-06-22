using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using QLThuocBenhVien.Models;
using System.Security.Claims;

namespace QLThuocBenhVien.Controllers
{
    public class PhieuNhapController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PhieuNhapController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PhieuNhap (Xem lại danh sách các phiếu đã lập)
        public async Task<IActionResult> Index()
        {
            var dsPhieu = await _context.PhieuNhap
                .Include(p => p.NhaCungCap)
                .OrderByDescending(p => p.NgayNhap)
                .ToListAsync();
            return View(dsPhieu);
        }

        // GET: PhieuNhap/Details/5 (Xem chi tiết 1 phiếu nhập cụ thể)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var phieu = await _context.PhieuNhap
                .Include(p => p.NhaCungCap)
                .Include(p => p.ChiTietPhieuNhaps)
                    .ThenInclude(ct => ct.DonViTinh)
                .FirstOrDefaultAsync(m => m.MaPN == id);

            if (phieu == null) return NotFound();

            return View(phieu);
        }

        // GET: PhieuNhap/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.NhaCungCapList = new SelectList(await _context.NhaCungCap.ToListAsync(), "MaNCC", "TenNCC");
            ViewBag.DonViTinhList = new SelectList(await _context.DonViTinh.ToListAsync(), "MaDVT", "TenDVT");
            return View();
        }

        // POST: PhieuNhap/Create (Xử lý lưu thực tế vào CSDL và tự ghi Log)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormCollection form, int MaNCC, string GhiChu)
        {
            var tenThuocs = form["TenThuoc[]"];
            var maDVTs = form["MaDVT[]"];
            var soLuongs = form["SoLuong[]"];
            var donGias = form["DonGia[]"];

            if (tenThuocs.Count == 0 || MaNCC == 0) return RedirectToAction(nameof(Create));

            // 1. Khởi tạo đối tượng Phiếu Nhập Gốc
            var phieuNhap = new PhieuNhap
            {
                MaNCC = MaNCC,
                NgayNhap = DateTime.Now, // NÂNG CẤP: Lấy chính xác ngày, giờ, phút, giây thực tế lúc bấm Lưu
                GhiChu = GhiChu,
                TongTien = 0
            };

            _context.PhieuNhap.Add(phieuNhap);
            await _context.SaveChangesAsync(); // Lưu trước để sinh MaPN tự động

            decimal tongTien = 0;

            // 2. Lưu danh sách chi tiết hàng nhập và CỘNG DỒN VÀO KHO
            for (int i = 0; i < tenThuocs.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(tenThuocs[i]))
                {
                    int q = int.TryParse(soLuongs[i], out int qty) ? qty : 0;
                    decimal p = decimal.TryParse(donGias[i], out decimal price) ? price : 0;
                    int dvt = int.TryParse(maDVTs[i], out int d) ? d : 0;

                    // NÂNG CẤP: Cắt bỏ mọi khoảng trắng thừa ở đầu/cuối chuỗi người dùng nhập
                    string tenThuocNhap = tenThuocs[i].Trim();

                    var ct = new ChiTietPhieuNhap
                    {
                        MaPN = phieuNhap.MaPN,
                        TenThuoc = tenThuocNhap,
                        MaDVT = dvt,
                        SoLuong = q,
                        DonGia = p
                    };
                    _context.ChiTietPhieuNhap.Add(ct);
                    tongTien += (q * p);

                    // =========================================================================
                    // 3. TỰ ĐỘNG CỘNG DỒN VÀO KHO THUỐC (MASTER DATA)
                    // NÂNG CẤP: So sánh không phân biệt chữ hoa/chữ thường để tránh lỗi do gõ sai
                    // =========================================================================
                    var thuocTrongKho = await _context.Thuoc
                        .FirstOrDefaultAsync(t => t.TenThuoc.ToLower() == tenThuocNhap.ToLower());

                    if (thuocTrongKho != null)
                    {
                        thuocTrongKho.SoLuongTon += q; // Cộng dồn số lượng
                        _context.Thuoc.Update(thuocTrongKho);
                    }
                }
            }

            // 4. Cập nhật lại tổng tiền thực của phiếu nhập
            phieuNhap.TongTien = tongTien;
            _context.PhieuNhap.Update(phieuNhap);

            // 5. Đồng bộ ghi nhận vào Nhật ký hệ thống (Logs)
            var nguoiDung = User.FindFirst("FullName")?.Value ?? "Hệ thống";
            _context.NhatKyHeThong.Add(new NhatKyHeThong
            {
                ThoiGian = DateTime.Now,
                Loai = "Info",
                NoiDung = $"Lập phiếu nhập kho #{phieuNhap.MaPN:D4}. Tổng giá trị: {tongTien:N0} đ",
                NguoiThucHien = nguoiDung
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã lập thành công phiếu nhập #{phieuNhap.MaPN:D4} và cập nhật tồn kho!";

            return RedirectToAction(nameof(Index));
        }
    }
}