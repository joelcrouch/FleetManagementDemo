using Microsoft.EntityFrameworkCore;
using FleetManagement.API.Models;

namespace FleetManagement.API.Data
{
    public class FleetContext : DbContext
    {
        public FleetContext(DbContextOptions<FleetContext> options) : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<ServiceAlert> ServiceAlerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Vehicle
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasIndex(e => e.VIN).IsUnique();
                entity.Property(e => e.Status).HasDefaultValue("Active");
            });

            // Configure MaintenanceRecord relationships
            modelBuilder.Entity<MaintenanceRecord>(entity =>
            {
                entity.HasOne(m => m.Vehicle)
                    .WithMany(v => v.MaintenanceRecords)
                    .HasForeignKey(m => m.VehicleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ServiceDate);
            });

            // Configure ServiceAlert relationships
            modelBuilder.Entity<ServiceAlert>(entity =>
            {
                entity.HasOne(a => a.Vehicle)
                    .WithMany(v => v.ServiceAlerts)
                    .HasForeignKey(a => a.VehicleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IsResolved);
                entity.HasIndex(e => e.Priority);
            });
        }
    }
}