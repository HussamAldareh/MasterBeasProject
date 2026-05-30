using MasterBeasProject.Data;
using MasterBeasProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MasterBeasProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==============================
        // Dashboard
        // ==============================
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalEngineers = await _context.EngineerProfiles.CountAsync();
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalReports = await _context.InspectionReports.CountAsync();
            ViewBag.PendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending);
            ViewBag.CompletedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Completed);

            var recentBookings = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.EngineerProfile)
                    .ThenInclude(e => e.User)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .ToListAsync();

            return View(recentBookings);
        }

        // ==============================
        // قائمة المستخدمين
        // ==============================
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.EngineerProfile)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // ==============================
        // تفعيل / تعطيل مستخدم
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = user.IsActive ? "User activated." : "User deactivated.";
            return RedirectToAction("Users");
        }

        // ==============================
        // قائمة الحجوزات
        // ==============================
        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.EngineerProfile)
                    .ThenInclude(e => e.User)
                .Include(b => b.PropertyDetails)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // ==============================
        // قائمة المهندسين
        // ==============================
        public async Task<IActionResult> Engineers()
        {
            var engineers = await _context.EngineerProfiles
                .Include(e => e.User)
                .Include(e => e.Reviews)
                .OrderByDescending(e => e.AverageRating)
                .ToListAsync();

            return View(engineers);
        }
    }
}