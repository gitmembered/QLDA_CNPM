using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThuocBenhVien.Models
{
    [Table("CHITIET_PHIEUNHAP")]
    public class ChiTietPhieuNhap
    {
        [Key]
        public int MaCTPN { get; set; }
        public int MaPN { get; set; }
        [Required]
        public string TenThuoc { get; set; }
        public int MaDVT { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }

        [ForeignKey("MaPN")]
        public virtual PhieuNhap? PhieuNhap { get; set; }

        [ForeignKey("MaDVT")]
        public virtual DonViTinh? DonViTinh { get; set; }
    }
}