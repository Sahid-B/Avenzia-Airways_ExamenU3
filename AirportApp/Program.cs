using Microsoft.EntityFrameworkCore;
using AirportApp.Models;
using AirportApp.Data;
using Microsoft.AspNetCore.Identity;
using DotNetEnv;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Load .env file variables
Env.Load();
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AirportApp.Filters.AdminOnlyMutationsFilter>();
});
builder.Services.AddRazorPages();

builder.Services.Configure<AirportApp.Settings.PayPalSettings>(
    builder.Configuration.GetSection("PayPal"));
builder.Services.AddHttpClient<AirportApp.Services.Payments.PayPalService>();

builder.Services.Configure<AirportApp.Settings.PayPhoneSettings>(
    builder.Configuration.GetSection("PayPhone"));
builder.Services.AddHttpClient<AirportApp.Services.Payments.PayPhoneApiLinkService>();

builder.Services.AddDbContext<AirportDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<AirportApp.Data.ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<Microsoft.AspNetCore.Identity.IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
    })
    .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
    .AddEntityFrameworkStores<AirportApp.Data.ApplicationDbContext>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });

builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, AirportApp.Services.EmailSender>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await AirportApp.Data.IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();
