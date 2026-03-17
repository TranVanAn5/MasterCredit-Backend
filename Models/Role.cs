using System.ComponentModel.DataAnnotations;
using backend.Models;

namespace backend.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}