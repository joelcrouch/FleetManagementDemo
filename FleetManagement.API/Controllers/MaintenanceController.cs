using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FleetManagement.API.Data;
using FleetManagement.API.Models;
using Microsoft.ApplicationInsights;

namespace FleetManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        private readonly FleetContext _context;
        private readonly ILogger<MaintenanceController> _logger;
        private readonly TelemetryClient _telemetry;

        public MaintenanceController(FleetContext context, ILogger<MaintenanceController> logger, TelemetryClient telemetry)
        {
            _context = context;
            _logger = logger;
            _telemetry = telemetry;
        }

        // GET: api/maintenance
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaintenanceRecord>>> GetMaintenanceRecords()
        {
            _logger.LogInformation("Getting all maintenance records");
            _telemetry.TrackEvent("GetAllMaintenanceRecords");

            return await _context.MaintenanceRecords
                .OrderByDescending(m => m.ServiceDate)
                .Take(100)
                .ToListAsync();
        }

        // GET: api/maintenance/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MaintenanceRecord>> GetMaintenanceRecord(int id)
        {
            _logger.LogInformation("Getting maintenance record with ID: {RecordId}", id);
            _telemetry.TrackEvent("GetMaintenanceRecord");

            var record = await _context.MaintenanceRecords
                .Include(m => m.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (record == null)
            {
                _logger.LogWarning("Maintenance record {RecordId} not found", id);
                _telemetry.TrackEvent("MaintenanceRecordNotFound");
                return NotFound();
            }

            return record;
        }

        // GET: api/maintenance/vehicle/5
        [HttpGet("vehicle/{vehicleId}")]
        public async Task<ActionResult<IEnumerable<MaintenanceRecord>>> GetMaintenanceByVehicle(int vehicleId)
        {
            _logger.LogInformation("Getting maintenance records for vehicle: {VehicleId}", vehicleId);
            _telemetry.TrackEvent("GetMaintenanceByVehicle");

            return await _context.MaintenanceRecords
                .Where(m => m.VehicleId == vehicleId)
                .OrderByDescending(m => m.ServiceDate)
                .ToListAsync();
        }

        // POST: api/maintenance
        [HttpPost]
        public async Task<ActionResult<MaintenanceRecord>> PostMaintenanceRecord(MaintenanceRecord record)
        {
            _logger.LogInformation("Creating maintenance record for vehicle: {VehicleId}", record.VehicleId);

            _context.MaintenanceRecords.Add(record);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Maintenance record created with ID: {RecordId}", record.Id);
            _telemetry.TrackEvent("MaintenanceRecordCreated");
            _telemetry.TrackMetric("MaintenanceCost", (double)record.Cost);

            return CreatedAtAction(nameof(GetMaintenanceRecord), new { id = record.Id }, record);
        }

        // PUT: api/maintenance/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMaintenanceRecord(int id, MaintenanceRecord record)
        {
            if (id != record.Id)
            {
                return BadRequest();
            }

            _context.Entry(record).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Maintenance record {RecordId} updated", id);
                _telemetry.TrackEvent("MaintenanceRecordUpdated");
                _telemetry.TrackMetric("MaintenanceCostUpdated", (double)record.Cost);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.MaintenanceRecords.Any(e => e.Id == id))
                {
                    _telemetry.TrackEvent("MaintenanceRecordUpdateNotFound");
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/maintenance/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaintenanceRecord(int id)
        {
            _logger.LogInformation("Deleting maintenance record: {RecordId}", id);

            var record = await _context.MaintenanceRecords.FindAsync(id);
            if (record == null)
            {
                _telemetry.TrackEvent("MaintenanceRecordDeleteNotFound");
                return NotFound();
            }

            _context.MaintenanceRecords.Remove(record);
            await _context.SaveChangesAsync();

            _telemetry.TrackEvent("MaintenanceRecordDeleted");
            _telemetry.TrackMetric("DeletedMaintenanceCost", (double)record.Cost);

            return NoContent();
        }
    }
}


