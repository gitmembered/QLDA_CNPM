using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Models;

namespace QLThuocBenhVien.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Thuoc> Thuoc { get; set; }
        public DbSet<NhomBenh> NhomBenh { get; set; }
        public DbSet<ThuocNhomBenh> ThuocNhomBenh { get; set; }
        public DbSet<NhaCungCap> NhaCungCap { get; set; }
        public DbSet<DonViTinh> DonViTinh { get; set; }
        public DbSet<TaiKhoan> TaiKhoan { get; set; }
        public DbSet<NhatKyHeThong> NhatKyHeThong { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ThuocNhomBenh>().ToTable("THUOC_NHOMBENH");
            // Định nghĩa Khóa chính kép (Composite Key) cho bảng trung gian
            modelBuilder.Entity<ThuocNhomBenh>()
                .HasKey(tnb => new { tnb.MaThuoc, tnb.MaNhomBenh });

            modelBuilder.Entity<ThuocNhomBenh>()
                .HasOne(tnb => tnb.Thuoc)
                .WithMany(t => t.ThuocNhomBenhs)
                .HasForeignKey(tnb => tnb.MaThuoc);

            modelBuilder.Entity<ThuocNhomBenh>()
                .HasOne(tnb => tnb.NhomBenh)
                .WithMany(nb => nb.ThuocNhomBenhs)
                .HasForeignKey(tnb => tnb.MaNhomBenh);
        }
    }
}