using System.ComponentModel.DataAnnotations.Schema;
using backend.Models;

namespace backend.Models
{
    public class CardApplication
    {
        public int Id { get; set; }
        public decimal GrossAnnualIncome { get; set; }
        public string Occupation { get; set; } = string.Empty;
        public string IncomeSource { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string IdCardPath { get; set; } = string.Empty;  // Path to uploaded ID card
        public string SalarySlipPath { get; set; } = string.Empty;  // Path to uploaded salary slip
        public int CardTypeId { get; set; }
        [ForeignKey("CardTypeId")]
        public CardType CardType { get; set; } = null!;
        public string Status { get; set; } = "Pending";  // Pending, Approved, Rejected
        public DateTime ApplicationDate { get; set; } = DateTime.Now;
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}