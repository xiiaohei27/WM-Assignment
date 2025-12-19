global using Main.Models;
using Main;
using QuestPDF.Infrastructure;
using Stripe;

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

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024 * 1024 * 500; // 500 MB max
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024 * 1024 * 500; // 500 MB
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();

// Add Session middleware
app.UseSession();

app.MapDefaultControllerRoute();

// Seed food data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    FoodDataSeeder.SeedFoodData(db);
}

QuestPDF.Settings.License = LicenseType.Community;

app.Run();