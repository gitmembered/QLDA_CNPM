using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThuocBenhVien.Models
{
    [Table("PHIEUXUAT")] // Chỉ định rõ tên bảng trong SQL
    public class PhieuXuat
    {
        [Key]
        public int MaPhieuXuat { get; set; }

        public DateTime NgayXuat { get; set; }

        public int MaKhoa { get; set; }

        public int MaTaiKhoan { get; set; }

        public decimal TongTien { get; set; }

        public string LyDoXuat { get; set; }

        // Liên kết 1-nhiều với Chi tiết phiếu xuất
        public virtual ICollection<ChiTietPhieuXuat> ChiTietPhieuXuats { get; set; } = new List<ChiTietPhieuXuat>();
    }
}