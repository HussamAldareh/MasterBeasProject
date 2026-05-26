using MasterBeasProject.Data;
using MasterBeasProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MasterBeasProject.Controllers
{
    [Authorize(Roles = "Client")]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Create(int bookingId)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .Include(b => b.EngineerProfile)
                    .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.ClientId == userId);

            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Completed)
            {
                TempData["Error"] = "Can only review completed bookings.";
                return RedirectToAction("MyBookings", "Booking");
            }

            if (await _context.Reviews.AnyAsync(r => r.BookingId == bookingId))
            {
                TempData["Error"] = "You have already reviewed this booking.";
                return RedirectToAction("MyBookings", "Booking");
            }

            ViewBag.Booking = booking;
            return View(new Review { BookingId = bookingId, EngineerProfileId = booking.EngineerProfileId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review model)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .Include(b => b.EngineerProfile)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.ClientId == userId);

            if (booking == null) return NotFound();

            ModelState.Remove("Booking");
            ModelState.Remove("Client");
            ModelState.Remove("EngineerProfile");
            ModelState.Remove("ClientId");

            if (!ModelState.IsValid)
            {
                ViewBag.Booking = booking;
                return View(model);
            }

            model.ClientId = userId!;
            model.EngineerProfileId = booking.EngineerProfileId;
            model.CreatedAt = DateTime.UtcNow;

            _context.Reviews.Add(model);
            await _context.SaveChangesAsync();

            // تحديث متوسط التقييم
            var engineer = await _context.EngineerProfiles
                .Include(e => e.Reviews)
                .FirstOrDefaultAsync(e => e.Id == booking.EngineerProfileId);

            if (engineer != null)
            {
                engineer.TotalReviews = engineer.Reviews.Count;
                engineer.AverageRating = (decimal)engineer.Reviews.Average(r => r.Rating);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Thank you for your review!";
            return RedirectToAction("MyBookings", "Booking");
        }
    }
}