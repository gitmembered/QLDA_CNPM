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
            // Danh sách nhà cung cấp (bạn đã có)
            ViewBag.NhaCungCapList = new SelectList(await _context.NhaCungCap.ToListAsync(), "MaNCC", "TenNCC");

            // THÊM DÒNG NÀY: Lấy danh sách thuốc từ danh mục để hiển thị ra bảng chọn
            ViewBag.ThuocDanhMucList = await _context.Thuoc.ToListAsync();

            return View();
        }

        // POST: PhieuNhap/Create (Xử lý lưu thực tế vào CSDL và tự ghi Log)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormCollection form, int MaNCC, string GhiChu)
        {
            var maThuocs = form["MaThuoc[]"];
            var soLuongs = form["SoLuong[]"];
            var donGiaNhaps = form["DonGiaNhap[]"];
            var donGiaXuats = form["DonGiaXuat[]"];

            if (maThuocs.Count == 0 || MaNCC == 0) return RedirectToAction(nameof(Create));

            var phieuNhap = new PhieuNhap
            {
                MaNCC = MaNCC,
                NgayNhap = DateTime.Now,
                GhiChu = GhiChu,
                TongTien = 0
            };
            _context.PhieuNhap.Add(phieuNhap);
            await _context.SaveChangesAsync();

            decimal tongTienPhieu = 0;

            // 2. Xử lý từng dòng thuốc nhập
            for (int i = 0; i < maThuocs.Count; i++)
            {
                if (int.TryParse(maThuocs[i], out int maThuoc) && maThuoc > 0)
                {
                    int q = int.TryParse(soLuongs[i], out int qty) ? qty : 0;

                    // FIX 1: Sửa chữ 'price' thành 'prc' cho khớp với biến out
                    decimal pNhap = decimal.TryParse(donGiaNhaps[i], out decimal prc) ? prc : 0;
                    decimal pXuat = decimal.TryParse(donGiaXuats[i], out decimal sale) ? sale : 0;

                    // Lấy thuốc từ kho ra TRƯỚC để lấy thông tin Tên Thuốc và Đơn Vị Tính
                    var thuocTrongKho = await _context.Thuoc.FindAsync(maThuoc);

                    if (thuocTrongKho != null)
                    {
                        var ct = new ChiTietPhieuNhap
                        {
                            MaPN = phieuNhap.MaPN,
                            TenThuoc = thuocTrongKho.TenThuoc,
                            MaDVT = thuocTrongKho.MaDVT ?? 0, // THÊM '?? 0' VÀO ĐÂY ĐỂ FIX LỖI
                            SoLuong = q,
                            DonGia = pNhap
                        };
                        _context.ChiTietPhieuNhap.Add(ct);
                        tongTienPhieu += (q * pNhap);
                        thuocTrongKho.SoLuongTon += q;

                        // Cập nhật tồn kho và đơn giá xuất viện mới nhất
                        thuocTrongKho.GiaNhap = pNhap; // Cập nhật Giá nhập mới nhất
                        thuocTrongKho.GiaBan = pXuat;  // Cập nhật Giá xuất/bán mới nhất
                        _context.Thuoc.Update(thuocTrongKho);
                    }
                }
            }

            phieuNhap.TongTien = tongTienPhieu;
            _context.PhieuNhap.Update(phieuNhap);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}