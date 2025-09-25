using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Pr4MVC.Models
{
    [Index(nameof(CustomerId), nameof(Date))]
    public class Order
    {
        public int Id { get; set; }

        [Required, StringLength(450)]
        public string CustomerId { get; set; } = "";
        public ApplicationUser? Customer { get; set; }

        
        public DateTime Date { get; set; } = DateTime.UtcNow;

        
        public const string ST_PENDIENTE = "Pendiente";
        public const string ST_PROCESADO = "Procesado";
        public const string ST_ENVIADO = "Enviado";
        public const string ST_ENTREGADO = "Entregado";
        public const string ST_CANCELADO = "Cancelado";

        [Required, StringLength(20)]
        public string Status { get; set; } = ST_PENDIENTE;

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; } = 0m;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
