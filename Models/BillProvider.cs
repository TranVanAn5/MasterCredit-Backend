using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class BillProvider
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ProviderName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ProviderCode { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceFee { get; set; }

        [MaxLength(200)]
        public string LogoUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public BillCategory Category { get; set; } = null!;

        // Navigation property
        public ICollection<BillPayment> Payments { get; set; } = new List<BillPayment>();
    }
}
