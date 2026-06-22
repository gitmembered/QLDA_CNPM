using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThuocBenhVien.Models
{
    [Table("PHIEUNHAP")]
    public class PhieuNhap
    {
        [Key]
        public int MaPN { get; set; }
        public DateTime NgayNhap { get; set; }
        public string? GhiChu { get; set; }
        public int MaNCC { get; set; }
        public decimal TongTien { get; set; }

        [ForeignKey("MaNCC")]
        public virtual NhaCungCap? NhaCungCap { get; set; }
        public virtual ICollection<ChiTietPhieuNhap> ChiTietPhieuNhaps { get; set; } = new List<ChiTietPhieuNhap>();
    }
}