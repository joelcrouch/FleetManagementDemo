using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetManagement.API.Models
{
    public class MaintenanceRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        public DateTime ServiceDate { get; set; }

        [Required]
        [StringLength(100)]
        public string ServiceType { get; set; } = string.Empty; // Oil Change, Tire Rotation, Brake Service, etc.

        [StringLength(50)]
        public string? PerformedBy { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Cost { get; set; }

        public int MileageAtService { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(100)]
        public string? PartsReplaced { get; set; }

        public bool IsWarrantyCovered { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("VehicleId")]
        public Vehicle Vehicle { get; set; } = null!;
    }
}