using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Momentix.Data.Data;
using Momentix.Data.Models;
using Momentix.API.Services;

namespace Momentix.API
{
    public class Program
    {
        private const string AdminRole = "Admin";
        private const string TestAdminUserName = "admin";
        private const string TestAdminPassword = "admin";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Database
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            // Identity
            builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // JWT
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secret = jwtSettings["Secret"]!;

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };
            });

            // Services
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddHttpClient<ChallengeVisionService>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(10);
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter: Bearer {token}"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.Migrate();

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                SeedTestAdminUser(userManager, roleManager).GetAwaiter().GetResult();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }

        private static async Task SeedTestAdminUser(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync(AdminRole))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(AdminRole));
                ThrowIfFailed(roleResult, "Could not create Admin role.");
            }

            var admin = await userManager.FindByNameAsync(TestAdminUserName)
                ?? await userManager.FindByEmailAsync(TestAdminUserName);

            if (admin == null)
            {
                admin = new User
                {
                    UserName = TestAdminUserName,
                    Email = TestAdminUserName,
                    EmailConfirmed = true,
                    FullName = "Admin",
                    ThemeColor = "#111111"
                };

                var createResult = await userManager.CreateAsync(admin);
                ThrowIfFailed(createResult, "Could not create test admin user.");
            }

            admin.UserName = TestAdminUserName;
            admin.Email = TestAdminUserName;
            admin.EmailConfirmed = true;
            admin.FullName = "Admin";
            admin.ThemeColor = string.IsNullOrWhiteSpace(admin.ThemeColor) ? "#111111" : admin.ThemeColor;
            admin.PasswordHash = userManager.PasswordHasher.HashPassword(admin, TestAdminPassword);
            admin.SecurityStamp = Guid.NewGuid().ToString();

            var updateResult = await userManager.UpdateAsync(admin);
            ThrowIfFailed(updateResult, "Could not update test admin user.");

            if (!await userManager.IsInRoleAsync(admin, AdminRole))
            {
                var addRoleResult = await userManager.AddToRoleAsync(admin, AdminRole);
                ThrowIfFailed(addRoleResult, "Could not add test admin user to Admin role.");
            }
        }

        private static void ThrowIfFailed(IdentityResult result, string message)
        {
            if (result.Succeeded)
                return;

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"{message} {errors}");
        }
    }
}
