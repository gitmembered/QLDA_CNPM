using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThuocBenhVien.Models
{
    [Table("BAOCAO")]
    public class BaoCao
    {
        [Key]
        public int MaBaoCao { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên báo cáo")]
        [StringLength(150)]
        public string TenBaoCao { get; set; }

        [Required]
        [StringLength(50)]
        public string LoaiBaoCao { get; set; } // 'TongHop', 'NhapKho', 'XuatKho'

        [Required]
        public DateTime TuNgay { get; set; }

        [Required]
        public DateTime DenNgay { get; set; }

        public DateTime NgayLap { get; set; }

        public int MaTaiKhoan { get; set; }

        [StringLength(255)]
        public string GhiChu { get; set; }
    }
}