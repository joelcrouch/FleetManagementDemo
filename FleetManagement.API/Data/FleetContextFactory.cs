using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FleetManagement.API.Data
{
    public class FleetContextFactory : IDesignTimeDbContextFactory<FleetContext>
    {
        public FleetContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FleetContext>();
            optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=FleetManagement;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

            return new FleetContext(optionsBuilder.Options);
        }
    }
}