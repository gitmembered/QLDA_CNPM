using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using System.Text.Json.Serialization; // Điểm thêm 1: Khai báo thư viện xử lý JSON

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. ĐĂNG KÝ CÁC DỊCH VỤ (SERVICES)
// ==========================================

// Cấu hình kết nối CSDL SQL Server qua Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Điểm thêm 2: Thêm dịch vụ hỗ trợ mô hình MVC VÀ chống vòng lặp JSON
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Điểm thêm 3: Cấu hình CORS (Cho phép mọi thiết bị, tên miền khác truy cập API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// ==========================================
// 2. CẤU HÌNH ĐƯỜNG ỐNG XỬ LÝ HTTP (MIDDLEWARE)
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Điểm thêm 4: Bật CORS (Lưu ý bắt buộc phải đặt ở giữa UseRouting và UseAuthorization)
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapStaticAssets();

// Định tuyến mặc định của MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

// ==========================================
// 3. CHẠY ỨNG DỤNG
// ==========================================
app.Run();