using System.Diagnostics;
using MasterBeasProject.Models;
using Microsoft.AspNetCore.Mvc;
using MasterBeasProject.Data;
using Microsoft.EntityFrameworkCore; 

namespace MasterBeasProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;


        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context )
        {
            _logger = logger;
            _context = context;

        }

        public async Task<IActionResult> Index()
        {
            var topEngineers = await _context.EngineerProfiles
                .Include(e => e.User)
                .Where(e => e.IsAvailable && e.User.IsActive)
                .OrderByDescending(e => e.AverageRating)
                .ThenByDescending(e => e.TotalReviews)
                .Take(4)
                .ToListAsync();

            ViewBag.TopEngineers = topEngineers;

            return View();
        }


        [HttpGet]
        public IActionResult SearchEngineers(string? city, string? engineerName, string? officeName)
        {
            return RedirectToAction(
                "Index",
                "Engineer",
                new
                {
                    city,
                    engineerName,
                    officeName
                });
        }






        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
