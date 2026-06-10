namespace QLThuocBenhVien.Models
{
    public class ThuocNhomBenh
    {
        public int MaThuoc { get; set; }
        public Thuoc Thuoc { get; set; }

        public int MaNhomBenh { get; set; }
        public NhomBenh NhomBenh { get; set; }
    }
}