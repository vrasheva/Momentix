using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Momentix.Data.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var apiProjectPath = FindApiProjectPath();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is missing.");

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new AppDbContext(optionsBuilder.Options);
        }

        private static string FindApiProjectPath()
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "Momentix.API");
                if (File.Exists(Path.Combine(candidate, "appsettings.json")))
                    return candidate;

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not find Momentix.API/appsettings.json.");
        }
    }
}
