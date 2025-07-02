using eTickets.Data.Base;
using eTickets.Data.ViewModels;
using eTickets.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eTickets.Data.Services
{
    public class MoviesService : EntityBaseRepository<Movie>, IMoviesService
    {
        private readonly AppDbContext _context;
        public MoviesService(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddNewMovieAsync(NewMovieVM data)
        {
            var newMovie = new Movie()
            {
                Name = data.Name,
                Description = data.Description,
                Price = data.Price,
                ImageURL = data.ImageURL,
                CinemaId = data.CinemaId,
                StartDate = data.StartDate,
                MovieCategory = data.MovieCategory,
                ProducerId = data.ProducerId
            };
            await _context.Movies.AddAsync(newMovie);
            await _context.SaveChangesAsync();

            foreach (var actorId in data.ActorIds)
            {
                var newActorMovie = new Actor_Movie()
                {
                    MovieId = newMovie.Id,
                    ActorId = actorId
                };
                await _context.Actors_Movies.AddAsync(newActorMovie);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<Movie> GetMovieByIdAsync(int id)
        {
            var movieDetails = await _context.Movies
                .Include(c => c.Cinema)
                .Include(p => p.Producer)
                .Include(am => am.Actors_Movies).ThenInclude(a => a.Actor)
                .FirstOrDefaultAsync(n => n.Id == id);

            return movieDetails;
        }

        public async Task<NewMovieDropdownsVM> GetNewMovieDropdownsValues()
        {
            var response = new NewMovieDropdownsVM()
            {
                Actors = await _context.Actors.OrderBy(n => n.FullName).ToListAsync(),
                Cinemas = await _context.Cinemas.OrderBy(n => n.Name).ToListAsync(),
                Producers = await _context.Producers.OrderBy(n => n.FullName).ToListAsync()
            };

            return response;
        }

        public async Task UpdateMovieAsync(NewMovieVM data)
        {
            // Start a transaction to ensure data consistency
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var dbMovie = await _context.Movies.FirstOrDefaultAsync(n => n.Id == data.Id);

                if (dbMovie != null)
                {
                    dbMovie.Name = data.Name;
                    dbMovie.Description = data.Description;
                    dbMovie.Price = data.Price;

                    // Only update ImageURL if a new one is provided
                    if (!string.IsNullOrEmpty(data.ImageURL))
                    {
                        dbMovie.ImageURL = data.ImageURL;
                    }

                    dbMovie.CinemaId = data.CinemaId;
                    dbMovie.StartDate = data.StartDate;
                    dbMovie.MovieCategory = data.MovieCategory;
                    dbMovie.ProducerId = data.ProducerId;

                    // Mark the entity as modified
                    _context.Movies.Update(dbMovie);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new ArgumentException($"Movie with ID {data.Id} not found.");
                }

                // Remove existing actor relationships
                var existingActorsDb = await _context.Actors_Movies
                    .Where(n => n.MovieId == data.Id)
                    .ToListAsync();

                if (existingActorsDb.Any())
                {
                    _context.Actors_Movies.RemoveRange(existingActorsDb);
                    await _context.SaveChangesAsync();
                }

                // Add new actor relationships
                if (data.ActorIds != null && data.ActorIds.Any())
                {
                    foreach (var actorId in data.ActorIds)
                    {
                        var newActorMovie = new Actor_Movie()
                        {
                            MovieId = data.Id,
                            ActorId = actorId
                        };
                        await _context.Actors_Movies.AddAsync(newActorMovie);
                    }
                    await _context.SaveChangesAsync();
                }

                // Commit the transaction
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // Rollback the transaction in case of any error
                await transaction.RollbackAsync();
                throw; // Re-throw the exception to be handled by the controller
            }
        }
    }
}