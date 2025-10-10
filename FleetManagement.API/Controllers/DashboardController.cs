using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FleetManagement.API.Data;

namespace FleetManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly FleetContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(FleetContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/dashboard/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetDashboardStats()
        {
            _logger.LogInformation("Fetching dashboard statistics");

            var totalVehicles = await _context.Vehicles.CountAsync();
            var activeVehicles = await _context.Vehicles.CountAsync(v => v.Status == "Active");
            var vehiclesInMaintenance = await _context.Vehicles.CountAsync(v => v.Status == "Maintenance");
            var unresolvedAlerts = await _context.ServiceAlerts.CountAsync(a => !a.IsResolved);
            var criticalAlerts = await _context.ServiceAlerts.CountAsync(a => !a.IsResolved && a.Priority == "Critical");

            var recentMaintenance = await _context.MaintenanceRecords
                .OrderByDescending(m => m.ServiceDate)
                .Take(5)
                .Select(m => new {
                    m.Id,
                    m.VehicleId,
                    m.ServiceType,
                    m.ServiceDate,
                    m.Cost
                })
                .ToListAsync();

            var totalMaintenanceCost = await _context.MaintenanceRecords
                .Where(m => m.ServiceDate >= DateTime.UtcNow.AddMonths(-12))
                .SumAsync(m => m.Cost);

            var stats = new
            {
                TotalVehicles = totalVehicles,
                ActiveVehicles = activeVehicles,
                VehiclesInMaintenance = vehiclesInMaintenance,
                UnresolvedAlerts = unresolvedAlerts,
                CriticalAlerts = criticalAlerts,
                RecentMaintenance = recentMaintenance,
                AnnualMaintenanceCost = totalMaintenanceCost
            };

            _logger.LogInformation("Dashboard stats retrieved successfully");
            return Ok(stats);
        }
    }
}