using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class BillPayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string CustomerCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string CustomerAddress { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal BillAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Success, Failed

        [MaxLength(100)]
        public string ReferenceNumber { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign keys
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public int CardId { get; set; }

        [ForeignKey("CardId")]
        public Card Card { get; set; } = null!;

        public int ProviderId { get; set; }

        [ForeignKey("ProviderId")]
        public BillProvider Provider { get; set; } = null!;
    }
}
