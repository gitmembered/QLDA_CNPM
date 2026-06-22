using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThuocBenhVien.Models
{
    [Table("CHITIETPHIEUXUAT")]
    public class ChiTietPhieuXuat
    {
        [Key]
        public int MaCTPX { get; set; }

        public int MaPhieuXuat { get; set; }
        [ForeignKey("MaPhieuXuat")]
        public virtual PhieuXuat PhieuXuat { get; set; }

        public int MaThuoc { get; set; }
        [ForeignKey("MaThuoc")]
        public virtual Thuoc Thuoc { get; set; }

        public int SoLuong { get; set; }

        public decimal DonGia { get; set; }
    }
}