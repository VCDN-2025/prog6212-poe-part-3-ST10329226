using CMCS_Prototype.Data;
using CMCS_Prototype.Models;
using CMCS_Prototype.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CMCS_Prototype;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // =============================================
        // 1. MVC & API Controllers with JSON Configuration
        // =============================================
        builder.Services.AddControllersWithViews()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
        builder.Services.AddControllers(); // Enables Web API endpoints

        // =============================================
        // 2. Database & Entity Framework Core
        // =============================================
        builder.Services.AddDbContext<CMCSDbContext>(options =>
            options.UseInMemoryDatabase("CMCS_Prototype_DB"));

        // =============================================
        // 3. ASP.NET Core Identity Configuration
        // =============================================
        builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.SignIn.RequireConfirmedAccount = false; // For prototype
        })
        .AddRoles<IdentityRole<int>>()
        .AddEntityFrameworkStores<CMCSDbContext>()
        .AddDefaultTokenProviders()
        .AddDefaultUI();

        // =============================================
        // 4. Custom Business Logic Services
        // =============================================
        builder.Services.AddScoped<ClaimValidationService>();
        builder.Services.AddScoped<ApprovalWorkflowService>();
        builder.Services.AddScoped<ReportingService>();
        builder.Services.AddScoped<ClaimPolicyService>();

        // =============================================
        // 5. FluentValidation Configuration
        // =============================================
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddFluentValidationClientsideAdapters();

        // =============================================
        // 6. Security & Infrastructure
        // =============================================
        builder.Services.AddAntiforgery();
        builder.Services.AddLogging();

        var app = builder.Build();

        // =============================================
        // 7. Database Seeding with Error Handling
        // =============================================
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<CMCSDbContext>();
                CMCSDbContext.SeedData(context);
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Database seeding failed during application startup.");
            }
        }

        // =============================================
        // 8. ASP.NET Core Middleware Pipeline (ORDER IS CRITICAL)
        // =============================================
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        // Authentication MUST come before Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // =============================================
        // 9. Route Configuration
        // =============================================
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}