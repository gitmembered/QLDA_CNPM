using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThuocBenhVien.Models
{
    [Table("DONVITINH")]
    public class DonViTinh
    {
        [Key]
        public int MaDVT { get; set; }

        [Required]
        [StringLength(50)]
        public string TenDVT { get; set; }
    }
}