using MasterBeasProject.Data;
using MasterBeasProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace MasterBeasProject.Controllers
{
    [Authorize(Roles = "Client")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public PaymentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
        }




        public async Task<IActionResult> CheckOut(int bookingId)
        {
        var userId = _userManager.GetUserId(User);
            var booking =await _context.Bookings.Include(b => b.EngineerProfile).FirstOrDefaultAsync(b => b.Id == bookingId && b.ClientId == userId);

            if(booking == null)
            {
                return NotFound();
            }
            if (booking.Status != BookingStatus.Accepted)
            {
                TempData["Error"] = "Booking must be accepted before payment.";
                return RedirectToAction("MyBookings", "Booking");
            }

            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            var domain = $"{Request.Scheme}://{Request.Host}";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(booking.Price * 100), // بالسنت
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Property Inspection — {booking.PropertyAddress}",
                                Description = $"Engineer: {booking.EngineerProfile?.User?.FullName}"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = $"{domain}/Payment/Success?bookingId={bookingId}&session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/Payment/Cancel?bookingId={bookingId}",
                Metadata = new Dictionary<string, string>
                {
                    { "bookingId", bookingId.ToString() },
                    { "userId", userId! }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }

        // ==============================
        // بعد الدفع الناجح
        // ==============================
        public async Task<IActionResult> Success(int bookingId, string session_id)
        {
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            var service = new SessionService();
            var session = await service.GetAsync(session_id);

            if (session.PaymentStatus != "paid")
            {
                TempData["Error"] = "Payment was not completed.";
                return RedirectToAction("MyBookings", "Booking");
            }

            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.ClientId == userId);

            if (booking == null) return NotFound();

            booking.Status = BookingStatus.InProgress;
            await _context.SaveChangesAsync();

            // إشعار للمهندس
            var engineer = await _context.EngineerProfiles
                .FirstOrDefaultAsync(e => e.Id == booking.EngineerProfileId);

            if (engineer != null)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = engineer.UserId,
                    Title = "Payment Received",
                    Body = $"Client has paid for booking #{bookingId}. You can now proceed with the inspection.",
                    Type = NotificationType.BookingAccepted,
                    Link = $"/Engineer/Dashboard"
                });
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Payment successful! The engineer will contact you shortly.";
            return RedirectToAction("Details", "Booking", new { id = bookingId });
        }

        // ==============================
        // إلغاء الدفع
        // ==============================
        public IActionResult Cancel(int bookingId)
        {
            TempData["Error"] = "Payment was cancelled. You can try again from your bookings.";
            return RedirectToAction("Details", "Booking", new { id = bookingId });
        }





    }

    }
