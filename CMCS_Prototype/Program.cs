using Microsoft.EntityFrameworkCore;
using CMCS_Prototype.Data;
using Microsoft.AspNetCore.Builder; // Added for clarity
using Microsoft.Extensions.DependencyInjection; // Added for clarity
using Microsoft.Extensions.Hosting; // Added for clarity

namespace CMCS_Prototype
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register the CMCSDbContext service
            // This is required to resolve the dependency injection error.
            builder.Services.AddDbContext<CMCSDbContext>(options =>
            {
                // Use an In-Memory Database for the prototype
                options.UseInMemoryDatabase("CMCS_Prototype_DB");
            });
            

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<CMCSDbContext>();
                CMCSDbContext.SeedData(context);
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
