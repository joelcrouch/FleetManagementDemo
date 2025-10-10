using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;

namespace FleetManagement.API.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(17, MinimumLength = 17)]
        public string VIN { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Make { get; set; } = string.Empty;


        [Required]
        [Range(1900, 2100)]
        public int Year { get; set; }

        [Required]
        [Range(0,999999)]
        public int Mileage { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; //active, Maintenancace, Retired

        [StringLength(100)]
        public string? Department { get; set; }

        public DateTime DateAcquired { get; set; } = DateTime.UtcNow;

        public DateTime LastServiceDate { get; set; }

        // Navigation property
        public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

        public ICollection<ServiceAlert> ServiceAlerts { get; set; } = new List<ServiceAlert>();
    }
}

