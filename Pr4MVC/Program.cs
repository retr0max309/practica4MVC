using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pr4MVC.Data;
using Pr4MVC.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = new[] { "admin", "cliente", "empleado" };
    foreach (var r in roles)
        if (!await roleMgr.RoleExistsAsync(r))
            await roleMgr.CreateAsync(new IdentityRole(r));

    var adminEmail = "admin@local.test";
    var admin = await userMgr.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FullName = "Administrador" };
        await userMgr.CreateAsync(admin, "Admin123!");
        await userMgr.AddToRoleAsync(admin, "admin");
    }                                           
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
