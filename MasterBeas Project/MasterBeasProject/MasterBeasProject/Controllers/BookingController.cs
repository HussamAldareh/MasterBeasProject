using MasterBeasProject.Data;
using MasterBeasProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MasterBeasProject.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==============================
        // حجز جديد — للعميل فقط
        // ==============================
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Create(int engineerId)
        {
            var engineer = await _context.EngineerProfiles
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == engineerId);

            if (engineer == null || !engineer.IsAvailable)
                return NotFound();


            var availability = await _context.EngineerAvailabilities
    .Where(a => a.EngineerProfileId == engineerId)
    .OrderBy(a => a.DayOfWeek)
    .ToListAsync();

            ViewBag.Availability = availability;

            ViewBag.Engineer = engineer;
            ViewBag.EngineerId = engineerId;



            return View(new PropertyDetails());
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int engineerId, PropertyDetails propertyDetails,
            string propertyAddress, DateTime inspectionDate,
            string? notes, decimal? latitude, decimal? longitude)
        {
            var engineer = await _context.EngineerProfiles
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == engineerId);

            if (engineer == null || !engineer.IsAvailable)
                return NotFound();

            var selectedDay = inspectionDate.DayOfWeek;

            var isAvailableOnThisDay = await _context.EngineerAvailabilities
                .AnyAsync(a =>
                    a.EngineerProfileId == engineerId &&
                    a.DayOfWeek == selectedDay);

            if (!isAvailableOnThisDay)
            {
                ModelState.AddModelError("inspectionDate",
                    "Engineer is not available on the selected day.");
            }

            // Validation
            ModelState.Remove("Booking");
            ModelState.Remove("BookingId");

            if (string.IsNullOrWhiteSpace(propertyAddress))
                ModelState.AddModelError("propertyAddress", "Property address is required.");



            if (!ModelState.IsValid)
            {
                var availability = await _context.EngineerAvailabilities
                    .Where(a => a.EngineerProfileId == engineerId)
                    .OrderBy(a => a.DayOfWeek)
                    .ToListAsync();

                ViewBag.Availability = availability;
                ViewBag.Engineer = engineer;
                ViewBag.EngineerId = engineerId;

                return View(propertyDetails);
            }

            var userId = _userManager.GetUserId(User);

            // إنشاء الحجز
            var booking = new Booking
            {
                ClientId = userId!,
                EngineerProfileId = engineerId,
                PropertyAddress = propertyAddress,
                InspectionDate = inspectionDate,
                Notes = notes,
                Latitude = latitude,
                Longitude = longitude,
                Price = engineer.InspectionPrice,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // إضافة تفاصيل الشقة
            propertyDetails.BookingId = booking.Id;
            _context.PropertyDetails.Add(propertyDetails);

            // إشعار للمهندس
            var notification = new Notification
            {
                UserId = engineer.UserId,
                Title = "New Booking Request",
                Body = $"You have a new inspection booking for {propertyAddress}.",
                Type = NotificationType.NewBooking,
                Link = $"/Engineer/Dashboard"
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking submitted successfully! Waiting for engineer confirmation.";
            return RedirectToAction("MyBookings");
        }

        // ==============================
        // قائمة حجوزات العميل
        // ==============================
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);

            var bookings = await _context.Bookings
                .Include(b => b.EngineerProfile)
                    .ThenInclude(e => e.User)
                .Include(b => b.PropertyDetails)
                .Include(b => b.InspectionReport)
                .Include(b => b.Review)
                .Where(b => b.ClientId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // ==============================
        // تفاصيل حجز واحد
        // ==============================
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.EngineerProfile)
                    .ThenInclude(e => e.User)
                .Include(b => b.PropertyDetails)
                .Include(b => b.InspectionReport)
                    .ThenInclude(r => r!.Images)
                .Include(b => b.Review)
                .Include(b => b.ChatMessages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            // التأكد أن المستخدم طرف في الحجز
            bool isClient = booking.ClientId == userId;
            bool isEngineer = booking.EngineerProfile.UserId == userId;
            bool isAdmin = User.IsInRole("Admin");

            if (!isClient && !isEngineer && !isAdmin)
                return Forbid();

            ViewBag.IsClient = isClient;
            ViewBag.IsEngineer = isEngineer;

            return View(booking);
        }

        // ==============================
        // إلغاء الحجز — للعميل فقط
        // ==============================
        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == userId);

            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "Only pending bookings can be cancelled.";
                return RedirectToAction("MyBookings");
            }

            booking.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking cancelled successfully.";
            return RedirectToAction("MyBookings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePrice(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.EngineerProfile)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return NotFound();

            booking.Price = booking.FinalPrice ?? booking.Price;
            booking.IsPriceApproved = true;
            booking.Status = BookingStatus.Accepted;

            var notification = new Notification
            {
                UserId = booking.EngineerProfile.UserId,
                Title = "Price Approved",
                Body = "The client approved your proposed price.",
                Type = NotificationType.NewBooking,
                Link = "/Engineer/Dashboard"
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Price approved successfully.";

            return RedirectToAction("Details", new { id = bookingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPrice(int bookingId)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return NotFound();

            booking.Status = BookingStatus.Cancelled;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Price rejected.";

            return RedirectToAction("Details", new { id = bookingId });
        }
    }
}