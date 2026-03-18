using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class BillCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CategoryCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string IconUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<BillProvider> Providers { get; set; } = new List<BillProvider>();
    }
}
