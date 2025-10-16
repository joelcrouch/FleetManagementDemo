using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FleetManagement.API.Data;
using FleetManagement.API.Models;
using Microsoft.ApplicationInsights;

namespace FleetManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController : ControllerBase
    {
        private readonly FleetContext _context;
        private readonly ILogger<VehiclesController> _logger;
        private readonly TelemetryClient _telemetry;

        public VehiclesController(FleetContext context, ILogger<VehiclesController> logger, TelemetryClient telemetry)
        {
            _context = context;
            _logger = logger;
            _telemetry = telemetry;
        }

        // GET: api/vehicles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles()
        {
            _logger.LogInformation("Getting all vehicles");
            _telemetry.TrackEvent("GetAllVehicles");

            return await _context.Vehicles.ToListAsync();
        }

        // GET: api/vehicles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Vehicle>> GetVehicle(int id)
        {
            _logger.LogInformation("Getting vehicle with ID: {VehicleId}", id);
            _telemetry.TrackEvent("GetVehicle");

            var vehicle = await _context.Vehicles.FindAsync(id);

            if (vehicle == null)
            {
                _logger.LogWarning("Vehicle with ID {VehicleId} not found", id);
                _telemetry.TrackEvent("VehicleNotFound");
                return NotFound();
            }

            return vehicle;
        }

        // GET: api/vehicles/slow
        [HttpGet("slow")]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehiclesSlow()
        {
            _logger.LogWarning("Slow endpoint called - simulating 3 second delay");
            _telemetry.TrackEvent("SlowEndpointCalled");

            Thread.Sleep(3000); // Simulate slow query
            return await _context.Vehicles.ToListAsync();
        }

        // GET: api/vehicles/error
        [HttpGet("error")]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehiclesError()
        {
            var random = new Random();
            if (random.Next(0, 2) == 0)
            {
                _logger.LogError("Intentional error thrown for monitoring demo");
                _telemetry.TrackEvent("IntentionalErrorThrown");
                throw new Exception("Simulated error for monitoring purposes");
            }

            _logger.LogInformation("Error endpoint succeeded this time");
            _telemetry.TrackEvent("ErrorEndpointSucceeded");

            return await _context.Vehicles.ToListAsync();
        }

        // PUT: api/vehicles/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVehicle(int id, Vehicle vehicle)
        {
            if (id != vehicle.Id)
            {
                _logger.LogWarning("Vehicle ID mismatch: {UrlId} vs {BodyId}", id, vehicle.Id);
                _telemetry.TrackEvent("VehicleUpdateBadRequest");
                return BadRequest();
            }

            _context.Entry(vehicle).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Vehicle {VehicleId} updated successfully", id);
                _telemetry.TrackEvent("VehicleUpdated");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleExists(id))
                {
                    _logger.LogWarning("Vehicle {VehicleId} not found during update", id);
                    _telemetry.TrackEvent("VehicleUpdateNotFound");
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/vehicles
        [HttpPost]
        public async Task<ActionResult<Vehicle>> PostVehicle(Vehicle vehicle)
        {
            _logger.LogInformation("Creating new vehicle: {VIN}", vehicle.VIN);
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Vehicle created with ID: {VehicleId}", vehicle.Id);
            _telemetry.TrackEvent("VehicleCreated");

            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
        }

        // DELETE: api/vehicles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            _logger.LogInformation("Deleting vehicle with ID: {VehicleId}", id);

            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                _logger.LogWarning("Vehicle {VehicleId} not found for deletion", id);
                _telemetry.TrackEvent("VehicleDeleteNotFound");
                return NotFound();
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Vehicle {VehicleId} deleted successfully", id);
            _telemetry.TrackEvent("VehicleDeleted");

            return NoContent();
        }

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.Id == id);
        }
    }
}


