global using Main.Models;
using Main;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\DB.mdf;
");
builder.Services.AddScoped<Helper>();

// Add Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddAuthentication().AddCookie();
builder.Services.AddHttpContextAccessor();

// Add Session support for shopping cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add Session middleware
app.UseSession();

app.MapDefaultControllerRoute();

// Seed food data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    FoodDataSeeder.SeedFoodData(db);
}

app.Run();