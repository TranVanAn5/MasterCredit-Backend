using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Card
    {
        [Key]
        public int Id { get; set;}
        public string CardNumber { get; set;} = string.Empty;
        public string CVV { get; set;} = string.Empty;
        public DateOnly ExpiryDate { get; set;}
        public string CardStatus { get; set;} = string.Empty;
        public int CardTypeId { get; set;}
        [ForeignKey("CardTypeId")]
        public CardType CardType { get; set;} = null!;
        public int UserId { get; set;}
        [ForeignKey("UserId")]
        public User User { get; set;} = null!;

    }
}