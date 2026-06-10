using System.ComponentModel.DataAnnotations;

namespace QLThuocBenhVien.Models
{
    public class Thuoc
    {
        [Key]
        public int MaThuoc { get; set; }
        public string TenThuoc { get; set; }
        public string HoatChat { get; set; }
        public string CongDung { get; set; }
        public decimal GiaBan { get; set; }
        public int SoLuongTon { get; set; }
        public DateTime HanSuDung { get; set; }

        public ICollection<ThuocNhomBenh> ThuocNhomBenhs { get; set; }
    }
}