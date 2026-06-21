using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThuocBenhVien.Models
{
    [Table("THUOC")]
    public class Thuoc
    {
        [Key]
        public int MaThuoc { get; set; }

        [Required]
        [StringLength(100)]
        public string TenThuoc { get; set; }

        [Required]
        [StringLength(100)]
        public string HoatChat { get; set; }

        [StringLength(255)]
        public string CongDung { get; set; }

        // Đã bổ sung GiaNhap
        [Required]
        [Range(0, double.MaxValue)]
        public decimal GiaNhap { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal GiaBan { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int SoLuongTon { get; set; }

        [Required]
        public DateTime HanSuDung { get; set; }

        // Đã bổ sung Khóa ngoại
        public int? MaNCC { get; set; }
        public int? MaDVT { get; set; }

        // Điều hướng Entity Framework (Navigation properties)
        [ForeignKey("MaNCC")]
        public virtual NhaCungCap NhaCungCap { get; set; }

        [ForeignKey("MaDVT")]
        public virtual DonViTinh DonViTinh { get; set; }

        public virtual ICollection<ThuocNhomBenh> ThuocNhomBenhs { get; set; } = new List<ThuocNhomBenh>();
    }
}