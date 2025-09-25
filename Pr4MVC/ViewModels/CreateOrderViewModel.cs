using System.ComponentModel.DataAnnotations;

namespace Pr4MVC.ViewModels
{
    public class CreateOrderItemInput
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Cantidad inválida")]
        public int Quantity { get; set; } 
    }

    public class CreateOrderViewModel
    {
        public List<CreateOrderItemInput> Items { get; set; } = new();
    }
}
