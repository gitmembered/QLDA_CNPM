using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. ĐĂNG KÝ CÁC DỊCH VỤ (SERVICES)
// ==========================================

// Cấu hình kết nối CSDL SQL Server qua Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Thêm dịch vụ hỗ trợ mô hình MVC
builder.Services.AddControllersWithViews();

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

app.UseAuthorization();

app.MapStaticAssets();

// Định tuyến mặc định của MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


// ==========================================
// 3. CHẠY ỨNG DỤNG
// ==========================================
app.Run();