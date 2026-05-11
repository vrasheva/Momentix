using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Momentix.Data.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var connectionString = "Server=bx6baoj00tcloacutkmw-mysql.services.clever-cloud.com;Port=3306;Database=bx6baoj00tcloacutkmw;User=uwrbrtilsz3sfzsj;Password=NPRZaWE80Ccx7uFvtR7X;SslMode=None;";

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}