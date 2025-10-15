using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FleetManagement.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AdminController> _logger;
        private readonly TelemetryClient _telemetry;

        public AdminController(IConfiguration config, ILogger<AdminController> logger, TelemetryClient telemetry)
        {
            _config = config;
            _logger = logger;
            _telemetry = telemetry;
        }

        [HttpGet("database-health")]
        public async Task<IActionResult> GetDatabaseHealth()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("Starting database health check...");
            _telemetry.TrackEvent("DatabaseHealthCheckStarted");

            string connectionString = _config.GetConnectionString("DefaultConnection");
            var result = new Dictionary<string, object>();

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                async Task<List<Dictionary<string, object>>> RunQuery(string name, string query)
                {
                    _logger.LogInformation("Running diagnostic query: {QueryName}", name);
                    using var cmd = new SqlCommand(query, connection);
                    using var reader = await cmd.ExecuteReaderAsync();
                    var table = new List<Dictionary<string, object>>();

                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                            row[reader.GetName(i)] = reader.GetValue(i);
                        table.Add(row);
                    }

                    result[name] = table;
                    _telemetry.TrackEvent($"QueryExecuted_{name}");
                    return table;
                }

                await RunQuery("CurrentlyExecuting", "SELECT session_id, status, command FROM sys.dm_exec_requests WHERE session_id > 50");
                await RunQuery("DatabaseSize", "SELECT DB_NAME(database_id) AS DBName, SUM(size)*8/1024 AS SizeMB FROM sys.master_files GROUP BY database_id");
                await RunQuery("ActiveConnections", "SELECT DB_NAME(dbid) AS DBName, COUNT(dbid) AS Connections FROM sys.sysprocesses WHERE dbid > 0 GROUP BY dbid");

                stopwatch.Stop();
                _logger.LogInformation("Database health check completed in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
                _telemetry.TrackMetric("DatabaseHealthCheckDurationMs", stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running database health check");
                _telemetry.TrackException(ex);
                return StatusCode(500, new { message = "Error retrieving database diagnostics", error = ex.Message });
            }
        }
    }
}

