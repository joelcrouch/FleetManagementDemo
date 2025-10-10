using Bogus;
using FleetManagement.API.Models;

namespace FleetManagement.API.Data
{
    public static class DbInitializer
    {
        public static void Initialize(FleetContext context)
        {
            // Check if database already has data
            if (context.Vehicles.Any())
            {
                return; // Database has been seeded
            }

            // Generate fake vehicles using Bogus
            var vehicleFaker = new Faker<Vehicle>()
                .RuleFor(v => v.VIN, f => f.Random.Replace("?????????????????")) // 17 chars
                .RuleFor(v => v.Make, f => f.Vehicle.Manufacturer())
                .RuleFor(v => v.Model, f => f.Vehicle.Model())
                .RuleFor(v => v.Year, f => f.Random.Int(2015, 2024))
                .RuleFor(v => v.Mileage, f => f.Random.Int(1000, 150000))
                .RuleFor(v => v.Status, f => f.PickRandom(new[] { "Active", "Maintenance", "Retired" }))
                .RuleFor(v => v.Department, f => f.PickRandom(new[] { "Transportation", "Maintenance", "Admin", "Field Operations" }))
                .RuleFor(v => v.DateAcquired, f => f.Date.Past(5))
                .RuleFor(v => v.LastServiceDate, f => f.Date.Recent(90));

            var vehicles = vehicleFaker.Generate(50);
            context.Vehicles.AddRange(vehicles);
            context.SaveChanges();

            // Generate maintenance records
            var maintenanceFaker = new Faker<MaintenanceRecord>()
                .RuleFor(m => m.VehicleId, f => f.PickRandom(vehicles).Id)
                .RuleFor(m => m.ServiceDate, f => f.Date.Past(2))
                .RuleFor(m => m.ServiceType, f => f.PickRandom(new[] {
                    "Oil Change", "Tire Rotation", "Brake Service",
                    "Engine Repair", "Transmission Service", "Inspection",
                    "Battery Replacement", "Air Filter Replacement"
                }))
                .RuleFor(m => m.PerformedBy, f => f.Name.FullName())
                .RuleFor(m => m.Cost, f => f.Random.Decimal(50, 2500))
                .RuleFor(m => m.MileageAtService, f => f.Random.Int(10000, 150000))
                .RuleFor(m => m.Notes, f => f.Lorem.Sentence())
                .RuleFor(m => m.PartsReplaced, f => f.Commerce.ProductName())
                .RuleFor(m => m.IsWarrantyCovered, f => f.Random.Bool(0.2f))
                .RuleFor(m => m.CreatedDate, f => f.Date.Past(2));

            var maintenanceRecords = maintenanceFaker.Generate(200);
            context.MaintenanceRecords.AddRange(maintenanceRecords);
            context.SaveChanges();

            // Generate service alerts
            var alertFaker = new Faker<ServiceAlert>()
                .RuleFor(a => a.VehicleId, f => f.PickRandom(vehicles).Id)
                .RuleFor(a => a.AlertType, f => f.PickRandom(new[] {
                    "Inspection Due", "Maintenance Overdue", "Registration Expiring",
                    "Safety Recall", "Emissions Test Due"
                }))
                .RuleFor(a => a.Priority, f => f.PickRandom(new[] { "Low", "Medium", "High", "Critical" }))
                .RuleFor(a => a.Description, f => f.Lorem.Sentence())
                .RuleFor(a => a.CreatedDate, f => f.Date.Recent(60))
                .RuleFor(a => a.DueDate, f => f.Date.Future(1))
                .RuleFor(a => a.IsResolved, f => f.Random.Bool(0.3f))
                .RuleFor(a => a.ResolvedDate, (f, a) => a.IsResolved ? f.Date.Recent(30) : null)
                .RuleFor(a => a.ResolvedBy, (f, a) => a.IsResolved ? f.Name.FullName() : null)
                .RuleFor(a => a.ResolutionNotes, (f, a) => a.IsResolved ? f.Lorem.Sentence() : null);

            var alerts = alertFaker.Generate(75);
            context.ServiceAlerts.AddRange(alerts);
            context.SaveChanges();
        }
    }
}