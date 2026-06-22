using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using QLThuocBenhVien.Models; // Cần có dòng này để nhận diện class Thuoc
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QLThuocBenhVien.Controllers
{
    // Tạo một class nhỏ để chứa dữ liệu thông báo hỗn hợp
    public class ThongBaoItem
    {
        public string Loai { get; set; }
        public string Icon { get; set; }
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public DateTime ThoiGian { get; set; }
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TongMaThuoc = await _context.Thuoc.CountAsync();

            var sapHetHangList = await _context.Thuoc.Where(t => t.SoLuongTon <= 50).ToListAsync();
            ViewBag.SapHetHangCount = sapHetHangList.Count;
            ViewBag.SapHetHangList = sapHetHangList;

            // ================= FIX LỖI: Khai báo list ở bên ngoài khối try =================
            var sapHetHanList = new List<Thuoc>();
            try
            {
                var thirtyDaysLater = DateTime.Now.AddDays(30);
                sapHetHanList = await _context.Thuoc.Where(t => t.HanSuDung <= thirtyDaysLater).ToListAsync();
                ViewBag.SapHetHanCount = sapHetHanList.Count;
                ViewBag.SapHetHanList = sapHetHanList;
            }
            catch
            {
                ViewBag.SapHetHanCount = 0;
                ViewBag.SapHetHanList = null;
            }

            ViewBag.TongGiaTriKho = await _context.Thuoc.SumAsync(t => (decimal?)(t.SoLuongTon * t.GiaBan)) ?? 0;

            var chartData = await _context.ThuocNhomBenh
                .Include(t => t.NhomBenh)
                .GroupBy(t => t.NhomBenh.TenNhomBenh)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .ToListAsync();
            ViewBag.ChartLabels = chartData.Select(c => c.Label).ToArray();
            ViewBag.ChartData = chartData.Select(c => c.Count).ToArray();

            // ================= LOGIC XỬ LÝ THÔNG BÁO THÔNG MINH =================
            var thongBaoList = new List<ThongBaoItem>();

            // 1. Quét thuốc sắp hết hạn (Ưu tiên cao)
            // Lúc này biến sapHetHanList đã được nhận diện hợp lệ
            if (sapHetHanList != null && sapHetHanList.Any())
            {
                foreach (var t in sapHetHanList)
                {
                    thongBaoList.Add(new ThongBaoItem
                    {
                        Loai = "danger",
                        Icon = "bi-clock-history",
                        TieuDe = "Cảnh báo hết hạn",
                        NoiDung = $"{t.TenThuoc} sẽ hết hạn vào {t.HanSuDung:dd/MM/yyyy}",
                        ThoiGian = DateTime.Now
                    });
                }
            }

            // 2. Quét thuốc sắp hết hàng
            if (sapHetHangList != null && sapHetHangList.Any())
            {
                foreach (var t in sapHetHangList)
                {
                    thongBaoList.Add(new ThongBaoItem
                    {
                        Loai = "warning",
                        Icon = "bi-exclamation-triangle",
                        TieuDe = "Cảnh báo tồn kho",
                        NoiDung = $"{t.TenThuoc} sắp hết (Chỉ còn {t.SoLuongTon})",
                        ThoiGian = DateTime.Now
                    });
                }
            }

            // 3. Lọc chỉ lấy Phiếu Nhập & Phiếu Xuất từ Nhật ký hệ thống
            var nhatKyList = await _context.NhatKyHeThong
                .Where(l => l.NoiDung.Contains("Lập phiếu nhập") || l.NoiDung.Contains("Lập phiếu xuất") || l.NoiDung.Contains("xuất kho"))
                .OrderByDescending(l => l.ThoiGian)
                .Take(10)
                .ToListAsync();

            foreach (var nk in nhatKyList)
            {
                bool isNhap = nk.NoiDung.Contains("nhập");
                thongBaoList.Add(new ThongBaoItem
                {
                    Loai = "primary",
                    Icon = isNhap ? "bi-box-arrow-in-down" : "bi-box-arrow-up",
                    TieuDe = isNhap ? "Phiếu nhập mới" : "Phiếu xuất mới",
                    NoiDung = nk.NoiDung,
                    ThoiGian = nk.ThoiGian
                });
            }

            // Gộp lại, sắp xếp theo thời gian mới nhất và lấy 6 cái
            var danhSachSapXep = thongBaoList.OrderByDescending(x => x.ThoiGian).ToList();

            // Gửi toàn bộ danh sách vào ViewBag này để dùng cho Modal "Xem tất cả"
            ViewBag.TatCaThongBao = danhSachSapXep;

            // Chỉ lấy 3 cái hiển thị ngoài màn hình chính Dashboard
            ViewBag.ThongBaoGanDay = danhSachSapXep.Take(3).ToList();

            return View();
        }
    }
}