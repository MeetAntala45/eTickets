// MoviesController.cs
using eTickets.Data;
using eTickets.Data.Services;
using eTickets.Data.Static;
using eTickets.Data.ViewModels;
using eTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace eTickets.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class MoviesController : Controller
    {
        private readonly IMoviesService _service;
        private readonly AppDbContext _context;

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
                var filteredResultNew = allMovies.Where(n => string.Equals(n.Name, searchString, StringComparison.CurrentCultureIgnoreCase) ||
                                                             string.Equals(n.Description, searchString, StringComparison.CurrentCultureIgnoreCase)).ToList();

                return View("Index", filteredResultNew);
            }

            return View("Index", allMovies);
        }

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
                return RedirectToAction("Login", "Account");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userMovies = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Movie)
                .Where(o => o.UserId == userId)
                .SelectMany(o => o.OrderItems, (order, orderItem) => new
                {
                    Id = orderItem.Movie.Id,
                    Name = orderItem.Movie.Name,
                    Description = orderItem.Movie.Description,
                    ImageURL = orderItem.Movie.ImageURL,
                    Price = orderItem.Price,
                    Category = orderItem.Movie.MovieCategory.ToString(),
                    StartDate = order.OrderDate // Purchase date
                })
                .ToListAsync();

            return View(userMovies);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Watch(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                    return RedirectToAction("Login", "Account");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var userMovie = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Movie)
                            .ThenInclude(m => m.Cinema)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Movie)
                            .ThenInclude(m => m.Producer)
                    .Where(o => o.UserId == userId)
                    .SelectMany(o => o.OrderItems, (order, orderItem) => new MovieWatchViewModel
                    {
                        MovieId = orderItem.Movie.Id,
                        Name = orderItem.Movie.Name,
                        Description = orderItem.Movie.Description,
                        ImageURL = orderItem.Movie.ImageURL,
                        Price = orderItem.Price,
                        Category = orderItem.Movie.MovieCategory.ToString(),
                        StartDate = orderItem.Movie.StartDate,
                        OrderDate = order.OrderDate,
                        PurchaseDate = order.OrderDate,
                        EndDate = order.OrderDate.AddDays(7),
                        Cinema = orderItem.Movie.Cinema.Name,
                        Producer = orderItem.Movie.Producer.FullName
                    })
                    .Where(m => m.MovieId == id && m.OrderDate.AddDays(7) >= DateTime.Now)
                    .FirstOrDefaultAsync();

                if (userMovie == null)
                {
                    TempData["Error"] = "You do not have access to this movie or it has expired.";
                    return RedirectToAction("MyMovies");
                }

                return View(userMovie);
            }
            catch (Exception ex)
            {
                return Content($"Exception: {ex.Message}");
            }
        }

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

            if (movie.ImageFile != null)
            {
                string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string imagesPath = Path.Combine(wwwRootPath, "images");

                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + movie.ImageFile.FileName;
                string filePath = Path.Combine(imagesPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await movie.ImageFile.CopyToAsync(stream);
                }

                movie.ImageURL = "/images/" + uniqueFileName;
            }

            await _service.AddNewMovieAsync(movie);
            return RedirectToAction(nameof(Index));
        }

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

            // Handle image file upload if a new image is provided
            if (movie.ImageFile != null)
            {
                string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string imagesPath = Path.Combine(wwwRootPath, "images");

                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + movie.ImageFile.FileName;
                string filePath = Path.Combine(imagesPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await movie.ImageFile.CopyToAsync(stream);
                }

                movie.ImageURL = "/images/" + uniqueFileName;
            }

            try
            {
                await _service.UpdateMovieAsync(movie);
                TempData["Success"] = "Movie updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the exception and show error message
                TempData["Error"] = "An error occurred while updating the movie: " + ex.Message;

                var movieDropdownsData = await _service.GetNewMovieDropdownsValues();
                ViewBag.Cinemas = new SelectList(movieDropdownsData.Cinemas, "Id", "Name");
                ViewBag.Producers = new SelectList(movieDropdownsData.Producers, "Id", "FullName");
                ViewBag.Actors = new SelectList(movieDropdownsData.Actors, "Id", "FullName");

                return View(movie);
            }
        }
    }
}