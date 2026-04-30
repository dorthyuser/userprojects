using System;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using azurefunction318.Models;

namespace azurefunction318.Repositories
{
    public class TravelcardRepository : ITravelcardRepository
    {
        private readonly ILogger<TravelcardRepository> _logger;

        public TravelcardRepository(ILogger<TravelcardRepository> logger)
        {
            _logger = logger;
        }

        private string BuildConnectionString()
        {
            // Sample commented connection string (DO NOT USE HARD-CODED CREDENTIALS)
            // var sample = "Host=localhost;Port=5432;Database=mydb;Username=myuser;Password=mypassword;";

            var host = Environment.GetEnvironmentVariable("POSTGRESQL_HOST");
            var port = Environment.GetEnvironmentVariable("POSTGRESQL_PORT");
            var database = Environment.GetEnvironmentVariable("POSTGRESQL_DATABASE");
            var username = Environment.GetEnvironmentVariable("POSTGRESQL_USERNAME");
            var password = Environment.GetEnvironmentVariable("POSTGRESQL_PASSWORD");

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(port) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("One or more PostgreSQL environment variables are not set");
            }

            var sb = new StringBuilder();
            sb.Append($"Host={host};");
            sb.Append($"Port={port};");
            sb.Append($"Database={database};");
            sb.Append($"Username={username};");
            sb.Append($"Password={password};");
            sb.Append("Pooling=true;SSL Mode=Prefer;Trust Server Certificate=true;");
            return sb.ToString();
        }

        public async Task InsertAsync(TravelcardRequest request)
        {
            var connString = BuildConnectionString();
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            using var tran = await conn.BeginTransactionAsync();
            try
            {
                var insertTravelcard = @"
INSERT INTO public.travelcards
(travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@TravelcardType, @ValidFrom, @ValidTo, @Name, @Number, @RequestedDate, @TransactionReference, @UsableTo)
RETURNING id;";

                var travelcardId = await conn.ExecuteScalarAsync<int>(insertTravelcard, new
                {
                    TravelcardType = request.TravelcardType,
                    ValidFrom = request.TravelcardValidFrom,
                    ValidTo = request.TravelcardValidTo,
                    Name = request.TravelcardName,
                    Number = request.TravelcardNumber,
                    RequestedDate = request.TravelcardRequestedDate,
                    TransactionReference = request.TravelcardTransactionReference,
                    UsableTo = request.TravelcardUsableTo
                }, transaction: tran);

                var insertCardholder = @"
INSERT INTO public.cardholders
(travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@TravelcardId, @Title, @Forename, @Surname, @Type, @PhotoName, @RrsKey, @Url, @Key);";

                foreach (var ch in request.Cardholders)
                {
                    await conn.ExecuteAsync(insertCardholder, new
                    {
                        TravelcardId = travelcardId,
                        Title = ch.CardholderTitle,
                        Forename = ch.CardholderForename,
                        Surname = ch.CardholderSurname,
                        Type = ch.CardholderType,
                        PhotoName = ch.CardholderPhotoName,
                        RrsKey = ch.CardholderPhotoRRSKey,
                        Url = ch.CardholderPhotoURL,
                        Key = ch.CardholderPhotoKey
                    }, transaction: tran);
                }

                await tran.CommitAsync();
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError(ex, "Error inserting travelcard or cardholders");
                throw;
            }
        }
    }
}
