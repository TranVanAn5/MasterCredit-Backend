namespace backend.Models
{
    public class CardType
    {
        public int Id { get; set; }
        public string CardName { get; set; } = string.Empty;
        public string CardNetwork { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; } 
        public decimal AnnualFee { get; set;}
        public decimal CashbackRate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

    }
}