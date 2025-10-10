using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetManagement.API.Models
{
    public class ServiceAlert
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        [StringLength(50)]
        public string AlertType { get; set; } = string.Empty; // Inspection Due, Maintenance Overdue, etc.

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical

        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? DueDate { get; set; }

        public bool IsResolved { get; set; } = false;

        public DateTime? ResolvedDate { get; set; }

        [StringLength(100)]
        public string? ResolvedBy { get; set; }

        [StringLength(250)]
        public string? ResolutionNotes { get; set; }

        // Navigation property
        [ForeignKey("VehicleId")]
        public Vehicle Vehicle { get; set; } = null!;
    }
}