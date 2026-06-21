using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;

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
        // Phần này sẽ được viết chi tiết hơn khi bạn làm chức năng lưu dữ liệu thực tế
        [HttpPost]
        public IActionResult Create(object formData)
        {
            // Logic xử lý lưu Phiếu Nhập và Chi Tiết Phiếu Nhập sẽ nằm ở đây
            return RedirectToAction("Index", "Thuoc");
        }
    }
}