using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using QLThuocBenhVien.Models;
using ClosedXML.Excel;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        // ================= GET: /BaoCao =================
        public async Task<IActionResult> Index(string chartFilter = "month")
        {
            ViewBag.CurrentFilter = chartFilter;

            // 1. CHỈ SỐ TỔNG QUAN CỐ ĐỊNH
            ViewBag.TongSoThuoc = await _context.Thuoc.CountAsync();
            ViewBag.ThuocSapHet = await _context.Thuoc.Where(t => t.SoLuongTon <= 50 && t.SoLuongTon > 0).CountAsync();
            ViewBag.ThuocHetHang = await _context.Thuoc.Where(t => t.SoLuongTon == 0).CountAsync();

            // 2. KHAI BÁO BIẾN LỌC (Chỉ khai báo 1 lần duy nhất)
            DateTime startDate;
            List<string> labels = new List<string>();
            List<decimal> data = new List<decimal>();

            // 3. XỬ LÝ LỌC MỐC THỜI GIAN THEO NGÀY/TUẦN/THÁNG
            if (chartFilter == "day")
            {
                startDate = DateTime.Now.AddDays(-6).Date;
                var raw = await _context.PhieuXuat.Where(p => p.NgayXuat >= startDate).ToListAsync();
                var grouped = raw.GroupBy(p => p.NgayXuat.Date).ToDictionary(g => g.Key, g => g.Sum(p => p.TongTien));

                for (int i = 0; i <= 6; i++)
                {
                    var date = startDate.AddDays(i);
                    labels.Add(date.ToString("dd/MM"));
                    data.Add(grouped.ContainsKey(date) ? grouped[date] : 0);
                }
            }
            else if (chartFilter == "week")
            {
                startDate = DateTime.Now.AddDays(-27).Date;
                var raw = await _context.PhieuXuat.Where(p => p.NgayXuat >= startDate).ToListAsync();

                for (int i = 3; i >= 0; i--)
                {
                    var weekStart = DateTime.Now.AddDays(-(i * 7 + 6)).Date;
                    var weekEnd = DateTime.Now.AddDays(-(i * 7)).Date.AddDays(1).AddTicks(-1);
                    labels.Add($"{weekStart:dd/MM}-{weekEnd:dd/MM}");
                    data.Add(raw.Where(p => p.NgayXuat >= weekStart && p.NgayXuat <= weekEnd).Sum(p => p.TongTien));
                }
            }
            else // Mặc định là month
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-5);
                var raw = await _context.PhieuXuat.Where(p => p.NgayXuat >= startDate).ToListAsync();
                var grouped = raw.GroupBy(p => new { p.NgayXuat.Year, p.NgayXuat.Month })
                                 .ToDictionary(g => new DateTime(g.Key.Year, g.Key.Month, 1), g => g.Sum(p => p.TongTien));

                for (int i = 0; i <= 5; i++)
                {
                    var month = startDate.AddMonths(i);
                    labels.Add($"Tháng {month.Month}/{month.Year}");
                    data.Add(grouped.ContainsKey(month) ? grouped[month] : 0);
                }
            }

            // 4. TỔNG TIỀN NHẬP / XUẤT (Lọc theo mốc thời gian đã chọn)
            ViewBag.TongTienNhap = await _context.PhieuNhap
                .Where(p => p.NgayNhap >= startDate)
                .SumAsync(p => (decimal?)p.TongTien) ?? 0;

            ViewBag.TongTienXuat = await _context.PhieuXuat
                .Where(p => p.NgayXuat >= startDate)
                .SumAsync(p => (decimal?)p.TongTien) ?? 0;

            // 5. TOP 5 THUỐC XUẤT NHIỀU NHẤT
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

            // 6. LỊCH SỬ BÁO CÁO
            ViewBag.LichSuBaoCao = await _context.BaoCao
                .OrderByDescending(b => b.NgayLap)
                .Take(10)
                .ToListAsync();

            ViewBag.XuHuongLabels = labels.ToArray();
            ViewBag.XuHuongData = data.ToArray();

            return View();
        }

        // ================= XUẤT BÁO CÁO EXCEL =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportExcelReport(string tenBaoCao, string loaiBaoCao, DateTime tuNgay, DateTime denNgay, string ghiChu)
        {
            if (string.IsNullOrEmpty(tenBaoCao)) tenBaoCao = "Báo cáo tổng hợp kho dược";
            if (string.IsNullOrEmpty(loaiBaoCao)) loaiBaoCao = "Tổng Hợp";

            DateTime denNgayFull = denNgay.Date.AddDays(1).AddTicks(-1);
            int maTaiKhoan = int.Parse(User.FindFirst("UserId")?.Value ?? "1");

            var baoCaoLog = new BaoCao
            {
                TenBaoCao = tenBaoCao,
                LoaiBaoCao = loaiBaoCao,
                TuNgay = tuNgay,
                DenNgay = denNgay,
                NgayLap = DateTime.Now,
                MaTaiKhoan = maTaiKhoan,
                GhiChu = ghiChu
            };
            _context.BaoCao.Add(baoCaoLog);

            var nguoiDung = User.FindFirst("FullName")?.Value ?? "Hệ thống";
            _context.NhatKyHeThong.Add(new NhatKyHeThong
            {
                ThoiGian = DateTime.Now,
                Loai = "Info",
                NoiDung = $"Xuất báo cáo ({loaiBaoCao}): {tenBaoCao}",
                NguoiThucHien = nguoiDung
            });
            await _context.SaveChangesAsync();

            var phieuNhaps = await _context.PhieuNhap
                .Where(p => p.NgayNhap >= tuNgay && p.NgayNhap <= denNgayFull)
                .ToListAsync();

            var phieuXuats = await _context.PhieuXuat
                .Include(p => p.ChiTietPhieuXuats)
                .ThenInclude(c => c.Thuoc)
                .Where(p => p.NgayXuat >= tuNgay && p.NgayXuat <= denNgayFull)
                .OrderBy(p => p.NgayXuat)
                .ToListAsync();

            var thuocDungTich = await _context.Thuoc.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                // ================= SHEET 1: TỔNG QUAN TÀI CHÍNH =================
                var wsSummary = workbook.Worksheets.Add("Tổng Quan");
                wsSummary.Cell(1, 1).Value = tenBaoCao.ToUpper();
                wsSummary.Range("A1:D1").Merge().Style.Font.SetBold().Font.SetFontSize(16);
                wsSummary.Range("A1:D1").Style.Font.FontColor = XLColor.DarkBlue;

                wsSummary.Cell(3, 1).Value = $"Loại báo cáo: Theo {loaiBaoCao}";
                wsSummary.Cell(4, 1).Value = $"Khoảng thời gian: {tuNgay:dd/MM/yyyy} đến {denNgay:dd/MM/yyyy}";
                wsSummary.Cell(5, 1).Value = $"Ngày lập báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}";
                wsSummary.Cell(6, 1).Value = $"Người trích xuất: {nguoiDung}";

                wsSummary.Cell(9, 1).Value = "Chỉ số cấu trúc";
                wsSummary.Cell(9, 2).Value = "Giá trị thống kê";
                wsSummary.Range("A9:B9").Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightCornflowerBlue);

                wsSummary.Cell(10, 1).Value = "Tổng giá trị nhập kho trong kỳ";
                wsSummary.Cell(10, 2).Value = phieuNhaps.Sum(p => p.TongTien);
                wsSummary.Cell(10, 2).Style.NumberFormat.Format = "#,##0 VNĐ";

                wsSummary.Cell(11, 1).Value = "Tổng giá trị điều phối xuất kho trong kỳ";
                wsSummary.Cell(11, 2).Value = phieuXuats.Sum(p => p.TongTien);
                wsSummary.Cell(11, 2).Style.NumberFormat.Format = "#,##0 VNĐ";

                wsSummary.Cell(12, 1).Value = "Cân cân giá trị (Nhập - Xuất)";
                wsSummary.Cell(12, 2).Value = phieuNhaps.Sum(p => p.TongTien) - phieuXuats.Sum(p => p.TongTien);
                wsSummary.Cell(12, 2).Style.NumberFormat.Format = "#,##0 VNĐ";
                wsSummary.Cell(12, 2).Style.Font.SetBold();

                wsSummary.Columns().AdjustToContents();

                // ================= SHEET 2: CHI TIẾT XUẤT KHO =================
                var wsExportDetails = workbook.Worksheets.Add("Chi Tiết Xuất Kho");
                wsExportDetails.Cell(1, 1).Value = "BÁO CÁO CHI TIẾT CHỨNG TỪ VÀ SỐ LƯỢNG THUỐC XUẤT KHO";
                wsExportDetails.Range("A1:G1").Merge().Style.Font.SetBold().Font.SetFontSize(14);
                wsExportDetails.Range("A1:G1").Style.Font.FontColor = XLColor.DarkBlue;

                wsExportDetails.Cell(3, 1).Value = "Ngày Xuất";
                wsExportDetails.Cell(3, 2).Value = "Mã Phiếu";
                wsExportDetails.Cell(3, 3).Value = "Khoa Nhận";
                wsExportDetails.Cell(3, 4).Value = "Tên Thuốc / Dược Phẩm";
                wsExportDetails.Cell(3, 5).Value = "Số Lượng";
                wsExportDetails.Cell(3, 6).Value = "Đơn Giá Xuất";
                wsExportDetails.Cell(3, 7).Value = "Thành Tiền";

                var headerRange = wsExportDetails.Range("A3:G3");
                headerRange.Style.Font.SetBold();
                headerRange.Style.Fill.SetBackgroundColor(XLColor.LightCyan);
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                int exportRow = 4;
                foreach (var px in phieuXuats)
                {
                    foreach (var ct in px.ChiTietPhieuXuats)
                    {
                        wsExportDetails.Cell(exportRow, 1).Value = px.NgayXuat.ToString("dd/MM/yyyy");
                        wsExportDetails.Cell(exportRow, 2).Value = $"PX-{px.MaPhieuXuat:D4}";
                        wsExportDetails.Cell(exportRow, 3).Value = $"Khoa số {px.MaKhoa}";
                        wsExportDetails.Cell(exportRow, 4).Value = ct.Thuoc?.TenThuoc ?? "Thuốc đã xóa";
                        wsExportDetails.Cell(exportRow, 5).Value = ct.SoLuong;
                        wsExportDetails.Cell(exportRow, 6).Value = ct.DonGia;
                        wsExportDetails.Cell(exportRow, 6).Style.NumberFormat.Format = "#,##0 VNĐ";

                        decimal thanhTien = ct.SoLuong * ct.DonGia;
                        wsExportDetails.Cell(exportRow, 7).Value = thanhTien;
                        wsExportDetails.Cell(exportRow, 7).Style.NumberFormat.Format = "#,##0 VNĐ";
                        exportRow++;
                    }
                }

                wsExportDetails.Cell(exportRow, 4).Value = "TỔNG CỘNG GIÁ TRỊ XUẤT TRONG KỲ:";
                wsExportDetails.Cell(exportRow, 4).Style.Font.SetBold();
                wsExportDetails.Cell(exportRow, 7).Value = phieuXuats.Sum(p => p.TongTien);
                wsExportDetails.Cell(exportRow, 7).Style.Font.SetBold().NumberFormat.Format = "#,##0 VNĐ";
                wsExportDetails.Columns().AdjustToContents();

                // ================= SHEET 3: BÁO CÁO TỒN KHO =================
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

                // ================= TRẢ FILE VỀ =================
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileDownloadName = $"BaoCao_{loaiBaoCao}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName);
                }
            }
        }
    }
}