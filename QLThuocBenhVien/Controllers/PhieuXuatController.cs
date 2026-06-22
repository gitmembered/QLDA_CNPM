using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using QLThuocBenhVien.Models;
using ClosedXML.Excel;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace QLThuocBenhVien.Controllers
{
    [Authorize] // Bắt buộc phải đăng nhập mới được dùng Controller này
    public class PhieuXuatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PhieuXuatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= 1. TRANG LỊCH SỬ XUẤT KHO (CHỈ ADMIN & QUANLY) =================
        [Authorize(Roles = "Admin,QuanLy")]
        public async Task<IActionResult> Index()
        {
            var danhSachPhieuXuat = await _context.PhieuXuat
                .OrderByDescending(p => p.NgayXuat)
                .ToListAsync();
            return View(danhSachPhieuXuat);
        }

        // ================= 2. TRANG XEM CHI TIẾT PHIẾU XUẤT (CHỈ ADMIN & QUANLY) =================
        [Authorize(Roles = "Admin,QuanLy")]
        public async Task<IActionResult> Details(int id)
        {
            var phieuXuat = await _context.PhieuXuat
                .Include(p => p.ChiTietPhieuXuats)
                .ThenInclude(c => c.Thuoc)
                .FirstOrDefaultAsync(p => p.MaPhieuXuat == id);

            if (phieuXuat == null) return NotFound();
            return View(phieuXuat);
        }
        // ================= 3. GIAO DIỆN LẬP PHIẾU XUẤT KHO (AI CŨNG ĐƯỢC LẬP) =================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Đổ danh sách thuốc ra, kết nối thêm bảng Nhóm Bệnh để hiển thị tên nhóm hỗ trợ tìm kiếm
            ViewBag.ThuocList = await _context.Thuoc
                .Include(t => t.ThuocNhomBenhs)
                .ThenInclude(tnb => tnb.NhomBenh)
                .OrderBy(t => t.TenThuoc)
                .ToListAsync();

            return View();
        }

        // POST: Xử lý lưu phiếu xuất kho vào CSDL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhieuXuat phieuXuat, List<ChiTietPhieuXuat> chiTietList)
        {
            if (chiTietList == null || !chiTietList.Any())
            {
                ViewBag.ErrorMessage = "Vui lòng thêm ít nhất một loại thuốc vào danh sách xuất kho!";
                ViewBag.ThuocList = await _context.Thuoc.OrderBy(t => t.TenThuoc).ToListAsync();
                return View(phieuXuat);
            }

            int maTaiKhoan = int.Parse(User.FindFirst("UserId")?.Value ?? "1");

            // Lưu phiếu xuất tổng tổng
            phieuXuat.NgayXuat = DateTime.Now;
            phieuXuat.MaTaiKhoan = maTaiKhoan;
            phieuXuat.TongTien = chiTietList.Sum(item => item.SoLuong * item.DonGia);

            _context.PhieuXuat.Add(phieuXuat);
            await _context.SaveChangesAsync();

            // Lưu chi tiết và trừ kho
            foreach (var item in chiTietList)
            {
                item.MaPhieuXuat = phieuXuat.MaPhieuXuat;
                _context.ChiTietPhieuXuat.Add(item);

                var thuoc = await _context.Thuoc.FindAsync(item.MaThuoc);
                if (thuoc != null)
                {
                    thuoc.SoLuongTon -= item.SoLuong; // Trừ kho thực tế
                }
            }

            // Ghi Log hệ thống
            var nguoiDung = User.FindFirst("FullName")?.Value ?? "Hệ thống";
            _context.NhatKyHeThong.Add(new NhatKyHeThong
            {
                ThoiGian = DateTime.Now,
                Loai = "Warning",
                NoiDung = $"Đã xuất kho phiếu #{phieuXuat.MaPhieuXuat} cho Khoa {phieuXuat.MaKhoa} - Tổng giá trị: {phieuXuat.TongTien:N0}đ",
                NguoiThucHien = nguoiDung
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Lập phiếu xuất kho và cập nhật số lượng tồn kho thành công!";

            // Nếu là quyền Admin hoặc Quản lý thì cho xem hóa đơn, ngược lại quay về trang chủ
            if (User.IsInRole("Admin") || User.IsInRole("QuanLy"))
            {
                return RedirectToAction("Details", new { id = phieuXuat.MaPhieuXuat });
            }
            return RedirectToAction("Index", "Home");
        }

        // ================= 4. XUẤT HÓA ĐƠN EXCEL (CHỈ ADMIN & QUANLY) =================
        [HttpGet]
        [Authorize(Roles = "Admin,QuanLy")]
        public async Task<IActionResult> ExportExcel(int id)
        {
            var phieuXuat = await _context.PhieuXuat
                .Include(p => p.ChiTietPhieuXuats)
                .ThenInclude(c => c.Thuoc)
                .FirstOrDefaultAsync(p => p.MaPhieuXuat == id);

            if (phieuXuat == null) return NotFound();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("PhieuXuatKho");
                worksheet.Cell(1, 1).Value = "HÓA ĐƠN XUẤT KHO DƯỢC PHẨM";
                worksheet.Range("A1:E1").Merge().Style.Font.SetBold().Font.FontSize = 16;
                worksheet.Range("A1:E1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(3, 1).Value = $"Mã Phiếu: PX-{phieuXuat.MaPhieuXuat:D4}";
                worksheet.Cell(4, 1).Value = $"Ngày Xuất: {phieuXuat.NgayXuat:dd/MM/yyyy HH:mm}";
                worksheet.Cell(5, 1).Value = $"Mã Khoa Nhận: Khoa {phieuXuat.MaKhoa}";
                worksheet.Cell(6, 1).Value = $"Lý Do Xuất: {phieuXuat.LyDoXuat}";

                int currentRow = 8;
                worksheet.Cell(currentRow, 1).Value = "STT";
                worksheet.Cell(currentRow, 2).Value = "Tên Thuốc";
                worksheet.Cell(currentRow, 3).Value = "Số Lượng";
                worksheet.Cell(currentRow, 4).Value = "Đơn Giá";
                worksheet.Cell(currentRow, 5).Value = "Thành Tiền";
                worksheet.Range($"A{currentRow}:E{currentRow}").Style.Font.Bold = true;

                int stt = 1;
                foreach (var item in phieuXuat.ChiTietPhieuXuats)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = stt++;
                    worksheet.Cell(currentRow, 2).Value = item.Thuoc.TenThuoc;
                    worksheet.Cell(currentRow, 3).Value = item.SoLuong;
                    worksheet.Cell(currentRow, 4).Value = item.DonGia;
                    worksheet.Cell(currentRow, 5).Value = item.SoLuong * item.DonGia;
                }

                currentRow++;
                worksheet.Cell(currentRow, 4).Value = "TỔNG CỘNG:";
                worksheet.Cell(currentRow, 5).Value = phieuXuat.TongTien;

                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PhieuXuat_{phieuXuat.MaPhieuXuat}.xlsx");
                }
            }
        }
    }
}