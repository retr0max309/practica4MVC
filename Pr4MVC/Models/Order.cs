using System.ComponentModel.DataAnnotations;

namespace Pr4MVC.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string CustomerId { get; set; } = "";
        public ApplicationUser? Customer { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required, StringLength(20)]
        public string Status { get; set; } = "Pendiente"; 

        [Range(0, double.MaxValue)]
        public decimal Total { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
