using System.ComponentModel.DataAnnotations;

namespace QLThuocBenhVien.Models
{
    public class NhomBenh
    {
        [Key]
        public int MaNhomBenh { get; set; }
        public string TenNhomBenh { get; set; }
        public string MoTa { get; set; }

        public ICollection<ThuocNhomBenh> ThuocNhomBenhs { get; set; }
    }
}