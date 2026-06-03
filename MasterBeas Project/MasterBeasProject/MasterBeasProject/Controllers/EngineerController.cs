using MasterBeasProject.Data;
using MasterBeasProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MasterBeasProject.Controllers
{
    public class EngineerController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EngineerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
     string? specialization,
     decimal? maxPrice,
     string? city,
     string? engineerName)
        {
            var query = _context.EngineerProfiles
                .Include(e => e.User)
                .Where(e => e.IsAvailable && e.User.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(specialization))
                query = query.Where(e => e.Specialization.Contains(specialization));

            if (maxPrice.HasValue)
                query = query.Where(e => e.InspectionPrice <= maxPrice.Value);

            if (!string.IsNullOrEmpty(city))
                query = query.Where(e => e.City != null && e.City.Contains(city));

            if (!string.IsNullOrEmpty(engineerName))
                query = query.Where(e =>
                    e.User.FullName.Contains(engineerName));

            var engineers = await query
                .OrderByDescending(e => e.AverageRating)
                .ToListAsync();

            return View(engineers);
        }
        public async Task<IActionResult> Details(int id)
        {
            var engineer = await _context.EngineerProfiles
                .Include(e => e.User)
                .Include(e => e.Reviews)
                    .ThenInclude(r => r.Client)
                        .Include(e => e.Bookings)

                .FirstOrDefaultAsync(e => e.Id == id);

            if (engineer == null)
                return NotFound();

            return View(engineer);
        }


        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> CompleteProfile()
        {
            var userId = _userManager.GetUserId(User);
            var existing = await _context.EngineerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (existing != null)
                return RedirectToAction("Dashboard", "Engineer");

            return View(new EngineerProfile());
        }
        [Authorize(Roles = "Engineer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProfile(EngineerProfile model, IFormFile? profileImage)
        {
            var userId = _userManager.GetUserId(User);

            // إزالة validation للـ navigation properties
            ModelState.Remove("User");
            ModelState.Remove("Bookings");
            ModelState.Remove("Reviews");
            ModelState.Remove("UserId");

            if (!ModelState.IsValid)
                return View(model);

            // رفع صورة البروفايل
            if (profileImage != null && profileImage.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
                if (!allowedTypes.Contains(profileImage.ContentType))
                {
                    ModelState.AddModelError("", "Only JPG, PNG, and WebP images are allowed.");
                    return View(model);
                }

                if (profileImage.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Image size must not exceed 2MB.");
                    return View(model);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(profileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await profileImage.CopyToAsync(stream);

                var user = await _userManager.FindByIdAsync(userId!);
                user!.ProfileImageUrl = $"/uploads/profiles/{fileName}";
                await _userManager.UpdateAsync(user);
            }

            model.UserId = userId!;
            model.CreatedAt = DateTime.UtcNow;

            _context.EngineerProfiles.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile completed successfully!";
            return RedirectToAction("Dashboard");
        }

        // ==============================
        // Dashboard المهندس
        // ==============================
        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);

            var profile = await _context.EngineerProfiles
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (profile == null)
                return RedirectToAction("CompleteProfile");

            var bookings = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.PropertyDetails)
                .Where(b => b.EngineerProfileId == profile.Id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.Profile = profile;
            ViewBag.PendingCount = bookings.Count(b => b.Status == BookingStatus.Pending);
            ViewBag.CompletedCount = bookings.Count(b => b.Status == BookingStatus.Completed);

            return View(bookings);
        }

        // ==============================
        // قبول / رفض الحجز
        // ==============================
        [Authorize(Roles = "Engineer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, string action, string? rejectionReason)
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _context.EngineerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (profile == null) return Unauthorized();

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.EngineerProfileId == profile.Id);

            if (booking == null) return NotFound();

            if (action == "accept")
            {
                booking.Status = BookingStatus.Accepted;
            }
            else if (action == "reject")
            {
                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    TempData["Error"] = "Rejection reason is required.";
                    return RedirectToAction("Dashboard");
                }
                booking.Status = BookingStatus.Rejected;
                booking.RejectionReason = rejectionReason;
            }

            await _context.SaveChangesAsync();

            // إشعار للعميل
            var notification = new Notification
            {
                UserId = booking.ClientId,
                Title = action == "accept" ? "Booking Accepted" : "Booking Rejected",
                Body = action == "accept"
                    ? $"Your inspection booking has been accepted."
                    : $"Your inspection booking was rejected. Reason: {rejectionReason}",
                Type = action == "accept" ? NotificationType.BookingAccepted : NotificationType.BookingRejected,
                Link = $"/Booking/Details/{bookingId}"
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = action == "accept" ? "Booking accepted." : "Booking rejected.";
            return RedirectToAction("Dashboard");
        }

        // ==============================
        // تبديل حالة التوفر
        // ==============================
        [Authorize(Roles = "Engineer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability()
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _context.EngineerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (profile == null) return NotFound();

            profile.IsAvailable = !profile.IsAvailable;
            await _context.SaveChangesAsync();

            TempData["Success"] = profile.IsAvailable ? "You are now available." : "You are now unavailable.";
            return RedirectToAction("Dashboard");
        }


        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> EditProfile()
        {
            var userId = _userManager.GetUserId(User);

            var profile = await _context.EngineerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (profile == null)
                return RedirectToAction("CompleteProfile");

            return View(profile);
        }

        [Authorize(Roles = "Engineer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EngineerProfile model, IFormFile? profileImage)
        {
            ModelState.Remove("User");
            ModelState.Remove("Bookings");
            ModelState.Remove("Reviews");
            ModelState.Remove("UserId");

            if (!ModelState.IsValid)
                return View(model);

            var existingProfile = await _context.EngineerProfiles
                .FirstOrDefaultAsync(e => e.Id == model.Id);

            if (existingProfile == null)
                return NotFound();

            existingProfile.Specialization = model.Specialization;
            existingProfile.YearsOfExperience = model.YearsOfExperience;
            existingProfile.InspectionPrice = model.InspectionPrice;
            existingProfile.Bio = model.Bio;
            existingProfile.LicenseNumber = model.LicenseNumber;
            existingProfile.City = model.City;

            if (profileImage != null && profileImage.Length > 0)
            {
                var user = await _userManager.FindByIdAsync(existingProfile.UserId);

                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/profiles");

                Directory.CreateDirectory(uploadsFolder);

                var fileName =
                    $"{existingProfile.UserId}_{Guid.NewGuid()}{Path.GetExtension(profileImage.FileName)}";

                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                user!.ProfileImageUrl = $"/uploads/profiles/{fileName}";
                await _userManager.UpdateAsync(user);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully.";

            return RedirectToAction("Dashboard");
        }

        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> ManageAvailability()
        {
            var userId = _userManager.GetUserId(User);

            var profile = await _context.EngineerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (profile == null)
                return RedirectToAction("CompleteProfile");

            var availability = await _context.EngineerAvailabilities
                .Where(a => a.EngineerProfileId == profile.Id)
                .ToListAsync();

            return View(availability);
        }




        [Authorize(Roles = "Engineer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageAvailability(
    DayOfWeek dayOfWeek,
    TimeSpan startTime,
    TimeSpan endTime)
        {
            var userId = _userManager.GetUserId(User);

            var profile = await _context.EngineerProfiles
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (profile == null)
                return RedirectToAction("CompleteProfile");

            var availability = new EngineerAvailability
            {
                EngineerProfileId = profile.Id,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime
            };

            _context.EngineerAvailabilities.Add(availability);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManageAvailability));
        }


        [Authorize(Roles = "Engineer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvailability(int id)
        {
            var availability = await _context.EngineerAvailabilities
                .FirstOrDefaultAsync(a => a.Id == id);

            if (availability != null)
            {
                _context.EngineerAvailabilities.Remove(availability);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ManageAvailability));
        }


    }
}
