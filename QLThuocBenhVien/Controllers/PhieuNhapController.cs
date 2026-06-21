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

        // GET: PhieuNhap/Create (Hiển thị form nhập kho)
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách nhà cung cấp từ database để đổ vào thẻ <select>
            var dsNhaCungCap = await _context.NhaCungCap.ToListAsync();
            ViewBag.NhaCungCapList = new SelectList(dsNhaCungCap, "MaNCC", "TenNCC");

            // Lấy danh sách Đơn vị tính
            var dsDonViTinh = await _context.DonViTinh.ToListAsync();
            ViewBag.DonViTinhList = new SelectList(dsDonViTinh, "MaDVT", "TenDVT");

            return View();
        }

        // POST: PhieuNhap/Create (Xử lý khi bấm nút Lưu & Xác nhận)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormCollection form)
        {
            // 1. Nhận các mảng dữ liệu từ các dòng động trên giao diện HTML
            var tenThuocs = form["TenThuoc[]"];
            var soLuongs = form["SoLuong[]"];
            var donGias = form["DonGia[]"];

            decimal tongTien = 0;
            int soLoaiThuoc = 0;

            // 2. Vòng lặp quét qua từng dòng thuốc được nhập
            for (int i = 0; i < tenThuocs.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(tenThuocs[i]))
                {
                    // Ép kiểu an toàn để tránh lỗi nếu người dùng để trống
                    int soLuong = int.TryParse(soLuongs[i], out int q) ? q : 0;
                    decimal donGia = decimal.TryParse(donGias[i], out decimal p) ? p : 0;

                    tongTien += (soLuong * donGia);
                    soLoaiThuoc++;

                    // --- Lưu ý quan trọng ---
                    // Khi bạn tạo xong bảng PHIEUNHAP và CHITIET_PHIEUNHAP trong CSDL,
                    // Bạn sẽ chèn lệnh _context.PhieuNhap.Add() và _context.ChiTietPhieuNhap.Add() ở ngay đây.
                }
            }

            // 3. Thực hiện ghi Log nếu có nhập thuốc
            if (soLoaiThuoc > 0)
            {
                var nguoiDung = User.FindFirst("FullName")?.Value ?? "Hệ thống";

                _context.NhatKyHeThong.Add(new NhatKyHeThong
                {
                    ThoiGian = DateTime.Now,
                    Loai = "Info",
                    NoiDung = $"Lập phiếu nhập kho: {soLoaiThuoc} loại thuốc. Tổng trị giá: {tongTien:N0} VNĐ",
                    NguoiThucHien = nguoiDung
                });

                await _context.SaveChangesAsync();

                // Trả về thông báo thành công cho màn hình Kho Thuốc
                TempData["SuccessMessage"] = $"Đã lập phiếu nhập kho thành công ({tongTien:N0} đ)!";
            }

            return RedirectToAction("Index", "Thuoc");
        }
    }
}