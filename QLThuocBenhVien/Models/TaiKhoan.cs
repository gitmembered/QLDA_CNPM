using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThuocBenhVien.Models
{
    [Table("TAIKHOAN")]
    public class TaiKhoan
    {
        [Key]
        public int MaTaiKhoan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50)]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(255)]
        public string MatKhau { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100)]
        public string HoTen { get; set; }

        [Required]
        [StringLength(20)]
        public string VaiTro { get; set; } // 'Admin', 'BacSi', 'DuocSi', 'QuanLy'

        public bool? TrangThai { get; set; } // 1: Hoạt động, 0: Bị khóa
        [StringLength(255)]
        public string? Avatar { get; set; } // Thêm dấu ? để cho phép null
    }
}