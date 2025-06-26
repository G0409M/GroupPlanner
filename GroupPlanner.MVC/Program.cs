using GroupPlanner.Infrastructure.Persistance;
using GroupPlanner.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using GroupPlanner.Infrastructure.Seeders;
using Microsoft.AspNetCore.Identity;
using GroupPlanner.Application;
using GroupPlanner.Application.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddSignalR();

var app = builder.Build();

// Run seeder
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<GroupPlannerSeeder>();
    await seeder.Seed();
}

// Configure the HTTP request pipeline
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

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapHub<AlgorithmHub>("/algorithmHub");

app.Run();
