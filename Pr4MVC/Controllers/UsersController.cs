using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pr4MVC.Models;
using Pr4MVC.ViewModels;

namespace Pr4MVC.Controllers
{
    [Authorize(Roles = "admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index(string? q = null)
        {
            var users = _userManager.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
                users = users.Where(u => u.Email!.Contains(q) || u.UserName!.Contains(q));
            var list = await users.OrderBy(u => u.Email).ToListAsync();
            var rolesDict = new Dictionary<string, IList<string>>();
            foreach (var u in list)
                rolesDict[u.Id] = await _userManager.GetRolesAsync(u);
            ViewBag.RolesByUser = rolesDict;
            ViewBag.Query = q;
            return View(list);
        }

        public IActionResult Create()
        {
            var vm = new CreateUserVM();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVM vm)
        {
            if (!ModelState.IsValid) return View(vm);
            if (!await _roleManager.RoleExistsAsync(vm.Role))
                ModelState.AddModelError(string.Empty, "Rol inválido");
            if (!ModelState.IsValid) return View(vm);

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FullName = vm.FullName
            };
            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }
            await _userManager.AddToRoleAsync(user, vm.Role);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            ViewBag.Roles = await _userManager.GetRolesAsync(user);
            return View(user);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            var vm = new EditUserVM
            {
                Id = user.Id,
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? "cliente"
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserVM vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            user.FullName = vm.FullName;
            user.Email = vm.Email;
            user.UserName = vm.Email;

            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                foreach (var e in update.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(vm.Role) || currentRoles.Count != 1)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, vm.Role);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.Roles = await _userManager.GetRolesAsync(user);
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "No se pudo eliminar el usuario.";
                return RedirectToAction(nameof(Index));
            }
            if (_signInManager.IsSignedIn(User) && user.Email == User.Identity!.Name)
                await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
