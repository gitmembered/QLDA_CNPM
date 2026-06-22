using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using QLThuocBenhVien.Models;
using ClosedXML.Excel;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QLThuocBenhVien.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,QuanLy")]
    public class BaoCaoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BaoCaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /BaoCao
        public async Task<IActionResult> Index()
        {
            ViewBag.TongSoThuoc = await _context.Thuoc.CountAsync();
            ViewBag.ThuocSapHet = await _context.Thuoc.Where(t => t.SoLuongTon <= 50 && t.SoLuongTon > 0).CountAsync();
            ViewBag.ThuocHetHang = await _context.Thuoc.Where(t => t.SoLuongTon == 0).CountAsync();

            ViewBag.TongTienNhap = await _context.PhieuNhap.SumAsync(p => (decimal?)p.TongTien) ?? 0;
            ViewBag.TongTienXuat = await _context.PhieuXuat.SumAsync(p => (decimal?)p.TongTien) ?? 0;

            // Lấy danh sách lịch sử các báo cáo đã xuất gần đây để hiển thị lên bảng
            ViewBag.LichSuBaoCao = await _context.BaoCao
                .OrderByDescending(b => b.NgayLap)
                .Take(10)
                .ToListAsync();

            // Thống kê đồ thị tròn Top 5
            var topThuocXuat = await _context.ChiTietPhieuXuat
                .Include(c => c.Thuoc)
                .GroupBy(c => c.MaThuoc)
                .Select(g => new
                {
                    TenThuoc = g.First().Thuoc.TenThuoc,
                    TongSoLuong = g.Sum(c => c.SoLuong)
                })
                .OrderByDescending(x => x.TongSoLuong).Take(5).ToListAsync();

            ViewBag.TopThuocLabels = topThuocXuat.Select(x => x.TenThuoc).ToArray();
            ViewBag.TopThuocData = topThuocXuat.Select(x => x.TongSoLuong).ToArray();

            // Thống kê xu hướng 6 tháng
            var sauThangTruoc = DateTime.Now.AddMonths(-5);
            var xuHuongXuat = await _context.PhieuXuat
                .Where(p => p.NgayXuat >= new DateTime(sauThangTruoc.Year, sauThangTruoc.Month, 1))
                .GroupBy(p => new { p.NgayXuat.Year, p.NgayXuat.Month })
                .Select(g => new
                {
                    ThangNam = $"Tháng {g.Key.Month}/{g.Key.Year}",
                    DoanhThu = g.Sum(p => p.TongTien),
                    SapXep = g.Key.Year * 100 + g.Key.Month
                })
                .OrderBy(x => x.SapXep).ToListAsync();

            ViewBag.XuHuongLabels = xuHuongXuat.Select(x => x.ThangNam).ToArray();
            ViewBag.XuHuongData = xuHuongXuat.Select(x => x.DoanhThu).ToArray();

            return View();
        }

        // ================= XỬ LÝ LƯU DB VÀ XUẤT FILE EXCEL BÁO CÁO TỔNG HỢP =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportExcelReport(string tenBaoCao, DateTime tuNgay, DateTime denNgay, string ghiChu)
        {
            if (string.IsNullOrEmpty(tenBaoCao)) tenBaoCao = "Báo cáo tổng hợp kho dược";

            // Đặt mốc thời gian cuối ngày cho 'denNgay' để quét trọn vẹn dữ liệu trong ngày đó
            DateTime denNgayFull = denNgay.Date.AddDays(1).AddTicks(-1);
            int maTaiKhoan = int.Parse(User.FindFirst("UserId")?.Value ?? "1");

            // 1. GHI NHẬN HỒ SƠ BÁO CÁO VÀO DATABASE
            var baoCaoLog = new BaoCao
            {
                TenBaoCao = tenBaoCao,
                LoaiBaoCao = "TongHop",
                TuNgay = tuNgay,
                DenNgay = denNgay,
                NgayLap = DateTime.Now,
                MaTaiKhoan = maTaiKhoan,
                GhiChu = ghiChu
            };
            _context.BaoCao.Add(baoCaoLog);

            // Ghi nhận nhật ký hệ thống
            var nguoiDung = User.FindFirst("FullName")?.Value ?? "Hệ thống";
            _context.NhatKyHeThong.Add(new NhatKyHeThong
            {
                ThoiGian = DateTime.Now,
                Loai = "Info",
                NoiDung = $"Xuất báo cáo dữ liệu dạng Excel: {tenBaoCao}",
                NguoiThucHien = nguoiDung
            });
            await _context.SaveChangesAsync();

            // 2. TRUY VẤN DỮ LIỆU TỔNG HỢP TỪ CSDL TRONG KHOẢNG THỜI GIAN LỰA CHỌN
            var phieuNhaps = await _context.PhieuNhap
                .Where(p => p.NgayNhap >= tuNgay && p.NgayNhap <= denNgayFull)
                .ToListAsync();

            var phieuXuats = await _context.PhieuXuat
                .Where(p => p.NgayXuat >= tuNgay && p.NgayXuat <= denNgayFull)
                .ToListAsync();

            var thuocDungTich = await _context.Thuoc.ToListAsync();

            // 3. KHỞI TẠO FILE EXCEL BẰNG CLOSEDXML
            using (var workbook = new XLWorkbook())
            {
                // --- SHEET 1: TỔNG QUAN TÀI CHÍNH ---
                var wsSummary = workbook.Worksheets.Add("Tổng Quan");

                wsSummary.Cell(1, 1).Value = tenBaoCao.ToUpper();
                wsSummary.Range("A1:D1").Merge().Style.Font.SetBold().Font.SetFontSize(16);
                wsSummary.Range("A1:D1").Style.Font.FontColor = XLColor.DarkBlue; // Đã sửa FontColor

                wsSummary.Cell(3, 1).Value = $"Khoảng thời gian: {tuNgay:dd/MM/yyyy} đến {denNgay:dd/MM/yyyy}";
                wsSummary.Cell(4, 1).Value = $"Ngày lập báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}";
                wsSummary.Cell(5, 1).Value = $"Người trích xuất: {nguoiDung}";

                // Tạo bảng tổng hợp số liệu tài chính
                wsSummary.Cell(8, 1).Value = "Chỉ số cấu trúc";
                wsSummary.Cell(8, 2).Value = "Giá trị thống kê";
                wsSummary.Range("A8:B8").Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightCornflowerBlue);

                wsSummary.Cell(9, 1).Value = "Tổng giá trị nhập kho trong kỳ";
                wsSummary.Cell(9, 2).Value = phieuNhaps.Sum(p => p.TongTien);
                wsSummary.Cell(9, 2).Style.NumberFormat.Format = "#,##0 VNĐ";

                wsSummary.Cell(10, 1).Value = "Tổng giá trị điều phối xuất kho trong kỳ";
                wsSummary.Cell(10, 2).Value = phieuXuats.Sum(p => p.TongTien);
                wsSummary.Cell(10, 2).Style.NumberFormat.Format = "#,##0 VNĐ";

                wsSummary.Cell(11, 1).Value = "Cân cân giá trị (Nhập - Xuất)";
                wsSummary.Cell(11, 2).Value = phieuNhaps.Sum(p => p.TongTien) - phieuXuats.Sum(p => p.TongTien);
                wsSummary.Cell(11, 2).Style.NumberFormat.Format = "#,##0 VNĐ";
                wsSummary.Cell(11, 2).Style.Font.SetBold();

                wsSummary.Columns().AdjustToContents();

                // --- SHEET 2: BIẾN ĐỘNG CHI TIẾT TỒN KHO THỰC TẾ ---
                var wsInventory = workbook.Worksheets.Add("Báo Cáo Tồn Kho");
                wsInventory.Cell(1, 1).Value = "DANH SÁCH THEO DÕI TỒN KHO HIỆN TẠI";
                wsInventory.Range("A1:D1").Merge().Style.Font.SetBold().Font.SetFontSize(14);

                wsInventory.Cell(3, 1).Value = "Mã Dược Phẩm";
                wsInventory.Cell(3, 2).Value = "Tên Thuốc";
                wsInventory.Cell(3, 3).Value = "Số Lượng Tồn Thực Tế";
                wsInventory.Cell(3, 4).Value = "Đánh Giá Trạng Thái";
                wsInventory.Range("A3:D3").Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);

                int row = 4;
                foreach (var t in thuocDungTich)
                {
                    wsInventory.Cell(row, 1).Value = $"MED-{t.MaThuoc:D3}";
                    wsInventory.Cell(row, 2).Value = t.TenThuoc;
                    wsInventory.Cell(row, 3).Value = t.SoLuongTon;

                    // Đã sửa thuộc tính FontColor ở đây
                    if (t.SoLuongTon == 0)
                    {
                        wsInventory.Cell(row, 4).Value = "Hết Hàng";
                        wsInventory.Cell(row, 4).Style.Font.FontColor = XLColor.Red;
                    }
                    else if (t.SoLuongTon <= 50)
                    {
                        wsInventory.Cell(row, 4).Value = "Cảnh Báo Sắp Hết";
                        wsInventory.Cell(row, 4).Style.Font.FontColor = XLColor.Orange;
                    }
                    else
                    {
                        wsInventory.Cell(row, 4).Value = "An Toàn";
                        wsInventory.Cell(row, 4).Style.Font.FontColor = XLColor.Green;
                    }
                    row++;
                }
                wsInventory.Columns().AdjustToContents();

                // 4. TRẢ FILE CHỨA LUỒNG DỮ LIỆU VỀ TRÌNH DUYỆT ĐỂ DOWNLOAD
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileDownloadName = $"BaoCaoTongHop_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName);
                }
            }
        }
    }
}