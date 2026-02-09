using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using TravelCardFunctionApp.Data;
using TravelCardFunctionApp.Models;

namespace TravelCardFunctionApp.Repositories
{
    public class TravelCardRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TravelCardRepository> _logger;

        public TravelCardRepository(AppDbContext context, ILogger<TravelCardRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TravelCard?> GetByIdAsync(Guid cardId)
        {
            // Ensure DB connection is opened explicitly as required
            var conn = _context.Database.GetDbConnection();
            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                // Using EF Core query - column mapping handled in OnModelCreating
                var card = await _context.TravelCards.AsNoTracking().FirstOrDefaultAsync(t => t.Id == cardId);
                return card;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching travel card {CardId}", cardId);
                throw;
            }
            finally
            {
                try
                {
                    if (conn.State == System.Data.ConnectionState.Open)
                        await conn.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to close DB connection cleanly");
                }
            }
        }
    }
}
