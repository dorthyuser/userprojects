using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ddctravelcard2026Lambda.Models;
using Npgsql;
using NpgsqlTypes;
using Amazon.Lambda.APIGatewayEvents;
using System.Net;

namespace Ddctravelcard2026Lambda.Services
{
    public class Service
    {
        private readonly string _connectionString;
        private readonly NpgsqlDataSource _dataSource;

        public Service()
        {
            // Read from environment or appsettings; fallback placeholder
            _connectionString = Environment.GetEnvironmentVariable("PostgreSql__ConnectionString") ?? "Host=your-host;Port=5432;Database=your-db;Username=your-user;Password=your-password;Pooling=true;Maximum Pool Size=20;";

            // Attempt to resolve DNS for the host portion proactively to avoid runtime DNS failures in some Lambda/VPC configurations
            try
            {
                var connStringBuilder = new NpgsqlConnectionStringBuilder(_connectionString);
                var host = connStringBuilder.Host;
                if (!string.IsNullOrEmpty(host) && !IPAddress.TryParse(host, out _))
                {
                    try
                    {
                        var addresses = Dns.GetHostAddressesAsync(host).GetAwaiter().GetResult();
                        if (addresses != null && addresses.Length > 0)
                        {
                            string chosen = null;
                            foreach (var a in addresses)
                            {
                                if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                {
                                    chosen = a.ToString();
                                    break;
                                }
                            }
                            if (chosen == null) chosen = addresses[0].ToString();
                            connStringBuilder.Host = chosen;
                            _connectionString = connStringBuilder.ToString();
                            Console.WriteLine($"Resolved host '{host}' to '{chosen}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Host resolution failed for '{host}': {ex.Message}");
                        // Leave the connection string as-is; Npgsql will attempt its own resolution later
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection string processing failed: {ex.Message}");
            }

            var dsBuilder = new NpgsqlDataSourceBuilder(_connectionString);

            // Map enums to pg enum type name exactly
            // Use overload that does not require a custom name translator to avoid API differences
            try
            {
                dsBuilder.MapEnum<TravelcardType>("travelcard_type_enum");
                dsBuilder.MapEnum<CardholderType>("cardholder_type_enum");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Enum mapping: {ex.Message}");
            }

            _dataSource = dsBuilder.Build();
        }

        public (bool IsValid, string ErrorMessage) ValidateRequest(Request request)
        {
            // Basic presence
            if (request == null) return (false, "Request is null");

            // travelcardType required
            // Dates
            var now = DateTime.UtcNow;

            if (request.TravelcardRequestedDate.ToUniversalTime() > now)
                return (false, "Requested date must be in the past");

            // valid_from should be earlier than or equal to valid_to
            if (request.TravelcardValidFrom.ToUniversalTime() > request.TravelcardValidTo.ToUniversalTime())
                return (false, "travelcardValidFrom must be earlier than or equal to travelcardValidTo");

            if (request.TravelcardValidTo.ToUniversalTime() <= now)
                return (false, "travelcardValidTo must be in the future");

            // If SixteenToSeventeen, usableTo is required and must be in future
            if (request.TravelcardType == TravelcardType.SixteenToSeventeen)
            {
                if (!request.TravelcardUsableTo.HasValue)
                    return (false, "travelcardUsableTo is required for SixteenToSeventeen travelcard type");

                if (request.TravelcardUsableTo.Value.ToUniversalTime() <= now)
                    return (false, "travelcardUsableTo must be in the future");
            }
            else
            {
                if (request.TravelcardUsableTo.HasValue)
                    return (false, "travelcardUsableTo must not be provided unless travelcard type is SixteenToSeventeen");
            }

            // travelcardName optional pattern
            if (!string.IsNullOrEmpty(request.TravelcardName))
            {
                if (request.TravelcardName.Length > 255) return (false, "travelcardName too long");
                if (!Regex.IsMatch(request.TravelcardName, "^[A-Za-z0-9 ]*$")) return (false, "travelcardName contains invalid characters");
            }

            // travelcardNumber length and pattern
            if (string.IsNullOrWhiteSpace(request.TravelcardNumber)) return (false, "travelcardNumber is required");
            if (request.TravelcardNumber.Length < 11 || request.TravelcardNumber.Length > 22) return (false, "travelcardNumber length must be between 11 and 22");
            if (!Regex.IsMatch(request.TravelcardNumber, "^[A-Za-z0-9]+$")) return (false, "travelcardNumber contains invalid characters");

            // transaction reference 15 chars specific format
            if (string.IsNullOrWhiteSpace(request.TravelcardTransactionReference) || request.TravelcardTransactionReference.Length != 15)
                return (false, "travelcardTransactionReference must be 15 characters");

            if (!Regex.IsMatch(request.TravelcardTransactionReference, "^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$"))
                return (false, "travelcardTransactionReference format invalid");

            // cardholders: exactly 1 or 2, must include Primary and optional Secondary
            if (request.Cardholders == null || request.Cardholders.Count < 1 || request.Cardholders.Count > 2)
                return (false, "cardholders must contain 1 or 2 items");

            bool hasPrimary = false;
            int secondaryCount = 0;
            foreach (var ch in request.Cardholders)
            {
                if (string.IsNullOrWhiteSpace(ch.CardholderTitle) || ch.CardholderTitle.Length > 15)
                    return (false, "cardholderTitle invalid");

                if (string.IsNullOrWhiteSpace(ch.CardholderForename) || ch.CardholderForename.Length > 100)
                    return (false, "cardholderForename invalid");

                if (string.IsNullOrWhiteSpace(ch.CardholderSurname) || ch.CardholderSurname.Length > 100)
                    return (false, "cardholderSurname invalid");

                if (string.IsNullOrWhiteSpace(ch.CardholderPhotoName) || ch.CardholderPhotoName.Length > 100)
                    return (false, "cardholderPhotoName invalid");

                if (ch.CardholderType == CardholderType.Primary) hasPrimary = true;
                if (ch.CardholderType == CardholderType.Secondary) secondaryCount++;

                // Photos: one of three must be present
                bool hasRrs = !string.IsNullOrEmpty(ch.CardholderPhotoRRSKey);
                bool hasUrl = !string.IsNullOrEmpty(ch.CardholderPhotoURL);
                bool hasKey = !string.IsNullOrEmpty(ch.CardholderPhotoKey);
                int present = (hasRrs ? 1 : 0) + (hasUrl ? 1 : 0) + (hasKey ? 1 : 0);
                if (present == 0) return (false, "Each cardholder must provide one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey");
                if (present > 1) return (false, "Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey");

                if (hasRrs)
                {
                    if (ch.CardholderPhotoRRSKey!.Length < 39 || ch.CardholderPhotoRRSKey.Length > 42) return (false, "cardholderPhotoRRSKey length invalid");
                    if (!Regex.IsMatch(ch.CardholderPhotoRRSKey, "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$")) return (false, "cardholderPhotoRRSKey format invalid");
                }
                if (hasKey)
                {
                    if (ch.CardholderPhotoKey!.Length < 39 || ch.CardholderPhotoKey.Length > 42) return (false, "cardholderPhotoKey length invalid");
                    if (!Regex.IsMatch(ch.CardholderPhotoKey, "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$")) return (false, "cardholderPhotoKey format invalid");
                }
                if (hasUrl)
                {
                    if (ch.CardholderPhotoURL!.Length < 20 || ch.CardholderPhotoURL.Length > 2048) return (false, "cardholderPhotoURL length invalid");
                    if (!Uri.IsWellFormedUriString(ch.CardholderPhotoURL, UriKind.Absolute)) return (false, "cardholderPhotoURL is not a valid URI");
                }
            }

            if (!hasPrimary) return (false, "At least one Primary cardholder is required");

            // Business rule: secondary allowed only for Family or TwoTogether (example rule)
            if (secondaryCount > 0)
            {
                if (!(request.TravelcardType == TravelcardType.Family || request.TravelcardType == TravelcardType.TwoTogether))
                    return (false, "Secondary cardholder is allowed only for Family or TwoTogether travelcard types");
            }

            return (true, string.Empty);
        }

        public APIGatewayProxyResponse CreateErrorResponse(int statusCode, string message)
        {
            var body = JsonSerializer.Serialize(new { error = message });
            return new APIGatewayProxyResponse
            {
                StatusCode = statusCode,
                Body = body,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        public async Task<(string TravelcardId, string Token)> CreateTravelcardAsync(Request request)
        {
            // Generate the external travelcardId as GUID to match expected response.
            var externalId = Guid.NewGuid().ToString();
            var token = GenerateToken(6);

            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Insert travelcard
                const string travelcardSql = @"
INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to)
VALUES (@type, @valid_from, @valid_to, @name, @number, @requested_date, @transaction_reference, @usable_to)
RETURNING id;";

                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = travelcardSql;

                // Use Parameters.Add (not AddWithValue)
                var pType = cmd.Parameters.Add("@type", NpgsqlDbType.Varchar);
                pType.Value = request.TravelcardType.ToString();

                var pFrom = cmd.Parameters.Add("@valid_from", NpgsqlDbType.TimestampTz);
                pFrom.Value = request.TravelcardValidFrom.ToUniversalTime();

                var pTo = cmd.Parameters.Add("@valid_to", NpgsqlDbType.TimestampTz);
                pTo.Value = request.TravelcardValidTo.ToUniversalTime();

                var pName = cmd.Parameters.Add("@name", NpgsqlDbType.Varchar);
                pName.Value = (object?)request.TravelcardName ?? DBNull.Value;

                var pNumber = cmd.Parameters.Add("@number", NpgsqlDbType.Varchar);
                pNumber.Value = request.TravelcardNumber;

                var pRequested = cmd.Parameters.Add("@requested_date", NpgsqlDbType.TimestampTz);
                pRequested.Value = request.TravelcardRequestedDate.ToUniversalTime();

                var pTrans = cmd.Parameters.Add("@transaction_reference", NpgsqlDbType.Unknown);
                pTrans.Value = request.TravelcardTransactionReference;

                var pUsable = cmd.Parameters.Add("@usable_to", NpgsqlDbType.TimestampTz);
                pUsable.Value = (object?)request.TravelcardUsableTo?.ToUniversalTime() ?? DBNull.Value;

                var idObj = await cmd.ExecuteScalarAsync();
                int travelcardId = Convert.ToInt32(idObj);

                // Insert cardholders
                const string cardholderSql = @"
INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key)
VALUES (@travelcard_id, @title, @forename, @surname, @type, @photo_name, @rrs_key, @url, @photo_key);
";

                foreach (var ch in request.Cardholders)
                {
                    await using var chCmd = conn.CreateCommand();
                    chCmd.Transaction = tx;
                    chCmd.CommandText = cardholderSql;

                    chCmd.Parameters.Add("@travelcard_id", NpgsqlDbType.Integer).Value = travelcardId;
                    chCmd.Parameters.Add("@title", NpgsqlDbType.Varchar).Value = ch.CardholderTitle;
                    chCmd.Parameters.Add("@forename", NpgsqlDbType.Varchar).Value = ch.CardholderForename;
                    chCmd.Parameters.Add("@surname", NpgsqlDbType.Varchar).Value = ch.CardholderSurname;
                    chCmd.Parameters.Add("@type", NpgsqlDbType.Varchar).Value = ch.CardholderType.ToString();
                    chCmd.Parameters.Add("@photo_name", NpgsqlDbType.Varchar).Value = ch.CardholderPhotoName;
                    chCmd.Parameters.Add("@rrs_key", NpgsqlDbType.Varchar).Value = (object?)ch.CardholderPhotoRRSKey ?? DBNull.Value;
                    chCmd.Parameters.Add("@url", NpgsqlDbType.Varchar).Value = (object?)ch.CardholderPhotoURL ?? DBNull.Value;
                    chCmd.Parameters.Add("@photo_key", NpgsqlDbType.Varchar).Value = (object?)ch.CardholderPhotoKey ?? DBNull.Value;

                    await chCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                Console.WriteLine($"Inserted travelcard id {travelcardId}");

                return (externalId, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB error: {ex}");
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        private static string GenerateToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rand = new Random();
            var arr = new char[length];
            for (int i = 0; i < length; i++) arr[i] = chars[rand.Next(chars.Length)];
            return new string(arr);
        }
    }
}
