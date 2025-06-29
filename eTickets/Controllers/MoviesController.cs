using eTickets.Data;
using eTickets.Data.Services;
using eTickets.Data.Static;
using eTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
namespace eTickets.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class MoviesController : Controller
    {
        private readonly IMoviesService _service;
        // Add these methods to your MoviesController.cs
        private readonly AppDbContext _context;

        // Update constructor to inject DbContext
        public MoviesController(IMoviesService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var allMovies = await _service.GetAllAsync(n => n.Cinema);
            return View(allMovies);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Filter(string searchString)
        {
            var allMovies = await _service.GetAllAsync(n => n.Cinema);

            if (!string.IsNullOrEmpty(searchString))
            {

                var filteredResultNew = allMovies.Where(n => string.Equals(n.Name, searchString, StringComparison.CurrentCultureIgnoreCase) || string.Equals(n.Description, searchString, StringComparison.CurrentCultureIgnoreCase)).ToList();

                return View("Index", filteredResultNew);
            }

            return View("Index", allMovies);
        }

        //GET: Movies/Details/1
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var movieDetail = await _service.GetMovieByIdAsync(id);
            return View(movieDetail);
        }
        

        [AllowAnonymous]
        public async Task<IActionResult> MyMovies()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get user's purchased movies that haven't expired
            var userMovies = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Movie)
                .Where(o => o.UserId == userId)
                .SelectMany(o => o.OrderItems)
                .Where(oi => oi.Movie.EndDate > DateTime.Now) // Only show non-expired movies
                .Select(oi => new
                {
                    Id = oi.Movie.Id,
                    Name = oi.Movie.Name,
                    Description = oi.Movie.Description,
                    ImageURL = oi.Movie.ImageURL,
                    Price = oi.Price,
                    Category = oi.Movie.MovieCategory.ToString(),
                    EndDate = oi.Movie.EndDate,
                    PurchaseDate = DateTime.Now // You might want to add a purchase date field to Order
                })
                .ToListAsync();

            return View(userMovies);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Watch(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if user has purchased this movie and it's not expired
            var userMovie = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Movie)
                .ThenInclude(m => m.Cinema)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Movie)
                .ThenInclude(m => m.Producer)
                .Where(o => o.UserId == userId)
                .SelectMany(o => o.OrderItems)
                .Where(oi => oi.MovieId == id && oi.Movie.EndDate > DateTime.Now)
                .Select(oi => new
                {
                    Id = oi.Movie.Id,
                    Name = oi.Movie.Name,
                    Description = oi.Movie.Description,
                    ImageURL = oi.Movie.ImageURL,
                    Price = oi.Price,
                    Category = oi.Movie.MovieCategory.ToString(),
                    EndDate = oi.Movie.EndDate,
                    StartDate = oi.Movie.StartDate,
                    Cinema = oi.Movie.Cinema.Name,
                    Producer = oi.Movie.Producer.FullName
                })
                .FirstOrDefaultAsync();

            if (userMovie == null)
            {
                TempData["Error"] = "Movie not found or access denied.";
                return RedirectToAction("MyMovies");
            }

            return View(userMovie);
        }

        //GET: Movies/Create
        public async Task<IActionResult> Create()
        {
            var movieDropdownsData = await _service.GetNewMovieDropdownsValues();

            ViewBag.Cinemas = new SelectList(movieDropdownsData.Cinemas, "Id", "Name");
            ViewBag.Producers = new SelectList(movieDropdownsData.Producers, "Id", "FullName");
            ViewBag.Actors = new SelectList(movieDropdownsData.Actors, "Id", "FullName");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(NewMovieVM movie)
        {
            if (!ModelState.IsValid)
            {
                var movieDropdownsData = await _service.GetNewMovieDropdownsValues();

                ViewBag.Cinemas = new SelectList(movieDropdownsData.Cinemas, "Id", "Name");
                ViewBag.Producers = new SelectList(movieDropdownsData.Producers, "Id", "FullName");
                ViewBag.Actors = new SelectList(movieDropdownsData.Actors, "Id", "FullName");

                return View(movie);
            }

            // Handle Image Upload
            if (movie.ImageFile != null)
            {
                string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string imagesPath = Path.Combine(wwwRootPath, "images");

                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + movie.ImageFile.FileName;
                string filePath = Path.Combine(imagesPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await movie.ImageFile.CopyToAsync(stream);
                }

                // Set image path in the ViewModel
                movie.ImageURL = "/images/" + uniqueFileName;
            }

            await _service.AddNewMovieAsync(movie);
            return RedirectToAction(nameof(Index));
        }

        //GET: Movies/Edit/1
        public async Task<IActionResult> Edit(int id)
        {
            var movieDetails = await _service.GetMovieByIdAsync(id);
            if (movieDetails == null) return View("NotFound");

            var response = new NewMovieVM()
            {
                Id = movieDetails.Id,
                Name = movieDetails.Name,
                Description = movieDetails.Description,
                Price = movieDetails.Price,
                StartDate = movieDetails.StartDate,
                EndDate = movieDetails.EndDate,
                ImageURL = movieDetails.ImageURL,
                MovieCategory = movieDetails.MovieCategory,
                CinemaId = movieDetails.CinemaId,
                ProducerId = movieDetails.ProducerId,
                ActorIds = movieDetails.Actors_Movies.Select(n => n.ActorId).ToList(),
            };

            var movieDropdownsData = await _service.GetNewMovieDropdownsValues();
            ViewBag.Cinemas = new SelectList(movieDropdownsData.Cinemas, "Id", "Name");
            ViewBag.Producers = new SelectList(movieDropdownsData.Producers, "Id", "FullName");
            ViewBag.Actors = new SelectList(movieDropdownsData.Actors, "Id", "FullName");

            return View(response);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, NewMovieVM movie)
        {
            if (id != movie.Id) return View("NotFound");

            if (!ModelState.IsValid)
            {
                var movieDropdownsData = await _service.GetNewMovieDropdownsValues();

                ViewBag.Cinemas = new SelectList(movieDropdownsData.Cinemas, "Id", "Name");
                ViewBag.Producers = new SelectList(movieDropdownsData.Producers, "Id", "FullName");
                ViewBag.Actors = new SelectList(movieDropdownsData.Actors, "Id", "FullName");

                return View(movie);
            }

            await _service.UpdateMovieAsync(movie);
            return RedirectToAction(nameof(Index));
        }
    }
}