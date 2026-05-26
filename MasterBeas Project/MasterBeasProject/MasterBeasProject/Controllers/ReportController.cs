using MasterBeasProject.Data;
using MasterBeasProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MasterBeasProject.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportController(ApplicationDbContext contextc, UserManager<ApplicationUser> userManager)
        {
            _context = contextc;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> Create(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.PropertyDetails)
                .Include(b => b.EngineerProfile)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.EngineerProfile.UserId == userId);

            if (booking == null)
                return NotFound();

            if (booking.Status != BookingStatus.Accepted)
            {
                TempData["Error"] = "Can only create report for accepted bookings.";
                return RedirectToAction("Dashboard", "Engineer");
            }

            if (await _context.InspectionReports.AnyAsync(r => r.BookingId == bookingId))
            {
                TempData["Error"] = "Report already exists for this booking.";
                return RedirectToAction("Dashboard", "Engineer");
            }

            ViewBag.Booking = booking;
            return View(new InspectionReport { BookingId = bookingId });
        }

        [Authorize(Roles = "Engineer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InspectionReport model, List<IFormFile> images)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.EngineerProfile)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.EngineerProfile.UserId == userId);

            if (booking == null) return NotFound();

            ModelState.Remove("Booking");
            ModelState.Remove("Images");
            ModelState.Remove("ReportNumber");

            if (!ModelState.IsValid)
            {
                ViewBag.Booking = booking;
                return View(model);
            }

            model.ReportNumber = $"PC-{DateTime.UtcNow:yyyyMMdd}-{booking.Id:D4}";
            model.IssuedAt = DateTime.UtcNow;

            _context.InspectionReports.Add(model);
            await _context.SaveChangesAsync();

            // رفع الصور
            if (images != null && images.Any())
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/reports");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var image in images.Take(10))
                {
                    if (image.Length == 0) continue;

                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
                    if (!allowedTypes.Contains(image.ContentType)) continue;
                    if (image.Length > 5 * 1024 * 1024) continue;

                    var fileName = $"{model.Id}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await image.CopyToAsync(stream);

                    _context.ReportImages.Add(new ReportImage
                    {
                        InspectionReportId = model.Id,
                        ImageUrl = $"/uploads/reports/{fileName}",
                        UploadedAt = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }

            booking.Status = BookingStatus.Completed;
            await _context.SaveChangesAsync();

            _context.Notifications.Add(new Notification
            {
                UserId = booking.ClientId,
                Title = "Inspection Report Ready",
                Body = $"Your inspection report for {booking.PropertyAddress} is ready.",
                Type = NotificationType.ReportReady,
                Link = $"/Booking/Details/{booking.Id}"
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Report submitted successfully!";
            return RedirectToAction("Dashboard", "Engineer");
        }

        public async Task<IActionResult> Download(int id)
        {
            var userId = _userManager.GetUserId(User);

            var report = await _context.InspectionReports
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Client)
                .Include(r => r.Booking)
                    .ThenInclude(b => b.EngineerProfile)
                        .ThenInclude(e => e.User)
                .Include(r => r.Booking)
                    .ThenInclude(b => b.PropertyDetails)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return NotFound();

            bool isClient = report.Booking.ClientId == userId;
            bool isEngineer = report.Booking.EngineerProfile.UserId == userId;
            bool isAdmin = User.IsInRole("Admin");

            if (!isClient && !isEngineer && !isAdmin)
                return Forbid();

            var pdf = GeneratePdf(report);
            return File(pdf, "application/pdf", $"PropCheck-Report-{report.ReportNumber}.pdf");
        }

        private byte[] GeneratePdf(InspectionReport report)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var b = report.Booking;

            string ConditionText(ConditionStatus status) => status switch
            {
                ConditionStatus.Good => "Good ✓",
                ConditionStatus.NeedsWork => "Needs Work ⚠",
                ConditionStatus.Poor => "Poor ✗",
                _ => status.ToString()
            };

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Content().Column(col =>
                    {
                        // Header
                        col.Item().Background("#1D9E75").Padding(15).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PropCheck — Inspection Report")
                                    .FontSize(18).Bold().FontColor("#FFFFFF");
                                c.Item().Text($"Report #{report.ReportNumber} | Issued: {report.IssuedAt:MMM dd, yyyy}")
                                    .FontSize(10).FontColor("#E0F5EE");
                            });
                        });

                        col.Item().Height(15);

                        // Property Info
                        col.Item().Border(1).BorderColor("#DDDDDD").Padding(12).Column(c =>
                        {
                            c.Item().Text("Property Information").Bold().FontColor("#1D9E75");
                            c.Item().Height(8);
                            c.Item().Row(r => { r.ConstantItem(150).Text("Address:").FontColor("#666666"); r.RelativeItem().Text(b.PropertyAddress).Bold(); });
                            c.Item().Row(r => { r.ConstantItem(150).Text("Type:").FontColor("#666666"); r.RelativeItem().Text(b.PropertyDetails?.PropertyType.ToString() ?? "N/A").Bold(); });
                            c.Item().Row(r => { r.ConstantItem(150).Text("Area:").FontColor("#666666"); r.RelativeItem().Text($"{b.PropertyDetails?.Area} m²").Bold(); });
                            c.Item().Row(r => { r.ConstantItem(150).Text("Floor:").FontColor("#666666"); r.RelativeItem().Text(b.PropertyDetails?.FloorNumber.ToString() ?? "N/A").Bold(); });
                            c.Item().Row(r => { r.ConstantItem(150).Text("Building Age:").FontColor("#666666"); r.RelativeItem().Text($"{b.PropertyDetails?.BuildingAge} years").Bold(); });
                        });

                        col.Item().Height(10);

                        // Inspection Results
                        col.Item().Border(1).BorderColor("#DDDDDD").Padding(12).Column(c =>
                        {
                            c.Item().Text("Inspection Results").Bold().FontColor("#1D9E75");
                            c.Item().Height(8);
                            c.Item().Row(r => { r.ConstantItem(150).Text("Structural:").FontColor("#666666"); r.RelativeItem().Text(ConditionText(report.StructuralCondition)).Bold(); });
                            if (!string.IsNullOrEmpty(report.StructuralNotes))
                                c.Item().Text($"  → {report.StructuralNotes}").FontColor("#555555").Italic();
                            c.Item().Row(r => { r.ConstantItem(150).Text("Electrical:").FontColor("#666666"); r.RelativeItem().Text(ConditionText(report.ElectricalCondition)).Bold(); });
                            if (!string.IsNullOrEmpty(report.ElectricalNotes))
                                c.Item().Text($"  → {report.ElectricalNotes}").FontColor("#555555").Italic();
                            c.Item().Row(r => { r.ConstantItem(150).Text("Plumbing:").FontColor("#666666"); r.RelativeItem().Text(ConditionText(report.PlumbingCondition)).Bold(); });
                            if (!string.IsNullOrEmpty(report.PlumbingNotes))
                                c.Item().Text($"  → {report.PlumbingNotes}").FontColor("#555555").Italic();
                            c.Item().Row(r => { r.ConstantItem(150).Text("Insulation:").FontColor("#666666"); r.RelativeItem().Text(ConditionText(report.InsulationCondition)).Bold(); });
                            if (!string.IsNullOrEmpty(report.InsulationNotes))
                                c.Item().Text($"  → {report.InsulationNotes}").FontColor("#555555").Italic();
                            c.Item().Row(r => { r.ConstantItem(150).Text("Finishing:").FontColor("#666666"); r.RelativeItem().Text(ConditionText(report.FinishingCondition)).Bold(); });
                            if (!string.IsNullOrEmpty(report.FinishingNotes))
                                c.Item().Text($"  → {report.FinishingNotes}").FontColor("#555555").Italic();
                        });

                        col.Item().Height(10);

                        // Overall Score
                        col.Item().Border(1).BorderColor("#DDDDDD").Padding(12).AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("Overall Score").Bold().FontColor("#1D9E75");
                            c.Item().AlignCenter().Text($"{report.OverallScore} / 100").FontSize(36).Bold().FontColor("#1D9E75");
                            if (!string.IsNullOrEmpty(report.Summary))
                                c.Item().AlignCenter().Text(report.Summary).FontColor("#555555");
                        });

                        col.Item().Height(10);

                        // Engineer Info
                        col.Item().Border(1).BorderColor("#DDDDDD").Padding(12).Column(c =>
                        {
                            c.Item().Text("Engineer").Bold().FontColor("#1D9E75");
                            c.Item().Height(8);
                            c.Item().Row(r => { r.ConstantItem(150).Text("Name:").FontColor("#666666"); r.RelativeItem().Text(b.EngineerProfile?.User?.FullName ?? "N/A").Bold(); });
                            c.Item().Row(r => { r.ConstantItem(150).Text("Specialization:").FontColor("#666666"); r.RelativeItem().Text(b.EngineerProfile?.Specialization ?? "N/A").Bold(); });
                            c.Item().Row(r => { r.ConstantItem(150).Text("License:").FontColor("#666666"); r.RelativeItem().Text(b.EngineerProfile?.LicenseNumber ?? "N/A").Bold(); });
                        });

                        col.Item().Height(20);

                        // Footer
                        col.Item().AlignCenter().Text($"Generated by PropCheck — prop-check.jo | {DateTime.UtcNow:yyyy}")
                            .FontSize(9).FontColor("#999999");
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}