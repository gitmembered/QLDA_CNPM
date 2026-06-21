using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThuocBenhVien.Models
{
    [Table("NHATKY_HETHONG")]
    public class NhatKyHeThong
    {
        [Key]
        public int MaLog { get; set; }
        public DateTime ThoiGian { get; set; }
        public string Loai { get; set; }
        public string NoiDung { get; set; }
        public string NguoiThucHien { get; set; }
    }
}