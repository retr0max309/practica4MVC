using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pr4MVC.Data;
using Pr4MVC.Models;

namespace Pr4MVC.Controllers
{
    [Authorize(Roles = "admin,empleado")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Products
        [AllowAnonymous] // quita si quieres todo protegido
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.Products.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Name.Contains(q));
            var items = await query.OrderBy(p => p.Name).ToListAsync();
            return View(items);
        }

        // GET: /Products/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // GET: /Products/Create
        public IActionResult Create() => View();

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,Stock,Category")] Product product)
        {
            if (!ModelState.IsValid) return View(product);
            _context.Add(product);
            await _context.SaveChangesAsync();
            TempData["ok"] = "Producto creado";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Stock,Category")] Product product)
        {
            if (id != product.Id) return NotFound();
            if (!ModelState.IsValid) return View(product);

            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["ok"] = "Producto actualizado";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Products.AnyAsync(e => e.Id == product.Id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["ok"] = "Producto eliminado";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
