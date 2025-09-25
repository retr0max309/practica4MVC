using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pr4MVC.Data;
using Pr4MVC.Models;
using Pr4MVC.ViewModels;

namespace Pr4MVC.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var isAdminOrEmpleado = User.IsInRole("admin") || User.IsInRole("empleado");
            IQueryable<Order> query = _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .AsNoTracking();

            if (!isAdminOrEmpleado)
            {
                var uid = _userManager.GetUserId(User);
                query = query.Where(o => o.CustomerId == uid);
            }

            var data = await query
                .OrderByDescending(o => o.Date)
                .ToListAsync();

            return View(data);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (!(User.IsInRole("admin") || User.IsInRole("empleado")))
            {
                var uid = _userManager.GetUserId(User);
                if (order.CustomerId != uid) return Forbid();
            }

            return View(order);
        }

        [Authorize(Roles = "admin,empleado,cliente")]
        public async Task<IActionResult> Create()
        {
            var products = await _db.Products
                .OrderBy(p => p.Name)
                .AsNoTracking()
                .ToListAsync();

            var vm = new CreateOrderViewModel
            {
                Items = products.Select(p => new CreateOrderItemInput
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Stock = p.Stock,
                    Quantity = 0
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,empleado,cliente")]
        public async Task<IActionResult> Create(CreateOrderViewModel vm)
        {
            var requested = vm.Items.Where(i => i.Quantity > 0).ToList();
            if (!requested.Any())
                ModelState.AddModelError(string.Empty, "Debes seleccionar al menos un producto con cantidad > 0.");

            if (!ModelState.IsValid)
            {
                var prods = await _db.Products.AsNoTracking().ToListAsync();
                foreach (var item in vm.Items)
                {
                    var p = prods.FirstOrDefault(x => x.Id == item.ProductId);
                    if (p != null)
                    {
                        item.Name = p.Name;
                        item.Price = p.Price;
                        item.Stock = p.Stock;
                    }
                }
                return View(vm);
            }

            var userId = _userManager.GetUserId(User);

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var productIds = requested.Select(r => r.ProductId).ToList();
                var products = await _db.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                var order = new Order
                {
                    CustomerId = userId!,
                    Date = DateTime.UtcNow,
                    Status = "Pendiente",
                    Total = 0m,
                    Items = new List<OrderItem>()
                };

                foreach (var r in requested)
                {
                    if (!products.TryGetValue(r.ProductId, out var prod))
                        throw new InvalidOperationException("Producto no encontrado.");

                    if (prod.Stock < r.Quantity)
                    {
                        ModelState.AddModelError(string.Empty, $"Stock insuficiente para {prod.Name}. Disponible: {prod.Stock}.");
                        var all = await _db.Products.AsNoTracking().ToListAsync();
                        foreach (var item in vm.Items)
                        {
                            var p = all.FirstOrDefault(x => x.Id == item.ProductId);
                            if (p != null)
                            {
                                item.Name = p.Name;
                                item.Price = p.Price;
                                item.Stock = p.Stock;
                            }
                        }
                        return View(vm);
                    }

                    var subtotal = prod.Price * r.Quantity;

                    order.Items.Add(new OrderItem
                    {
                        ProductId = prod.Id,
                        Quantity = r.Quantity,
                        Subtotal = subtotal
                    });

                    prod.Stock -= r.Quantity;
                }

                order.Total = order.Items.Sum(i => i.Subtotal);

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Ocurrió un error al crear el pedido. Intenta nuevamente.");
                var prods = await _db.Products.AsNoTracking().ToListAsync();
                foreach (var item in vm.Items)
                {
                    var p = prods.FirstOrDefault(x => x.Id == item.ProductId);
                    if (p != null)
                    {
                        item.Name = p.Name;
                        item.Price = p.Price;
                        item.Stock = p.Stock;
                    }
                }
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,empleado")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            var isAdminOrEmpleado = User.IsInRole("admin") || User.IsInRole("empleado");
            var uid = _userManager.GetUserId(User);
            if (!isAdminOrEmpleado && order.CustomerId != uid) return Forbid();

            if (order.Status != "Pendiente")
            {
                TempData["Error"] = "Sólo puedes cancelar pedidos en estado Pendiente.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var ids = order.Items.Select(i => i.ProductId).ToList();
                var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
                foreach (var it in order.Items)
                    if (products.TryGetValue(it.ProductId, out var p)) p.Stock += it.Quantity;

                order.Status = "Cancelado";
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Ok"] = "Pedido cancelado y stock devuelto.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["Error"] = "No se pudo cancelar el pedido.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [Authorize(Roles = "admin,empleado")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,empleado")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var order = await _db.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);
                if (order == null) return NotFound();

                if (order.Status == "Pendiente")
                {
                    var ids = order.Items.Select(i => i.ProductId).ToList();
                    var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
                    foreach (var it in order.Items)
                        if (products.TryGetValue(it.ProductId, out var p)) p.Stock += it.Quantity;
                }

                _db.OrderItems.RemoveRange(order.Items);
                _db.Orders.Remove(order);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await tx.RollbackAsync();
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
