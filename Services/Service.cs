using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Npgsql;
using Buyandsellgold1013Lambda.Models;

namespace Buyandsellgold1013Lambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<Service> _logger;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<Service>();
    }

    public async Task<ProductListResponse> GetProductsAsync(IDictionary<string, string>? queryParams, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        var page = ParsePositiveInt(queryParams, "page", 1);
        var pageSize = ParsePositiveInt(queryParams, "pageSize", 20);
        _logger.LogInformation("Validation passed.");

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        const string sql = "SELECT product_id, name, gold_purity, weight_grams, price, currency, status, COUNT(*) OVER() AS total_count FROM products ORDER BY name LIMIT @pageSize OFFSET @offset";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        _logger.LogInformation("products SELECT");

        var items = new List<ProductResponse>();
        var totalRecords = 0;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            totalRecords = reader.GetInt32(reader.GetOrdinal("total_count"));
            items.Add(new ProductResponse
            {
                ProductId = reader.GetString(reader.GetOrdinal("product_id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                GoldPurity = reader.GetString(reader.GetOrdinal("gold_purity")),
                WeightGrams = reader.GetDecimal(reader.GetOrdinal("weight_grams")),
                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                Currency = reader.GetString(reader.GetOrdinal("currency")),
                Status = reader.GetString(reader.GetOrdinal("status"))
            });
        }

        return new ProductListResponse
        {
            Data = items,
            Pagination = new PaginationResponse { Page = page, PageSize = pageSize, TotalRecords = totalRecords, TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize) }
        };
    }

    public async Task<ProductResponse> GetProductAsync(string productId, CancellationToken cancellationToken)
    {
        ValidateIdentifier(productId, nameof(productId));
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        const string sql = "SELECT product_id, name, gold_purity, weight_grams, price, currency, status FROM products WHERE product_id = @productId";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("productId", productId);
        _logger.LogInformation("products SELECT");
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new KeyNotFoundException();
        return new ProductResponse
        {
            ProductId = reader.GetString(0), Name = reader.GetString(1), GoldPurity = reader.GetString(2), WeightGrams = reader.GetDecimal(3), Price = reader.GetDecimal(4), Currency = reader.GetString(5), Status = reader.GetString(6)
        };
    }

    public async Task<CreateBuyOrderResponse> CreateBuyOrderAsync(CreateBuyOrderRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            var total = 0m;
            foreach (var item in request.Items)
            {
                await using var priceCmd = new NpgsqlCommand("SELECT price, status FROM products WHERE product_id = @productId", conn, tx);
                priceCmd.Parameters.AddWithValue("productId", item.ProductId);
                _logger.LogInformation("products SELECT");
                await using var reader = await priceCmd.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken)) throw new KeyNotFoundException();
                var status = reader.GetString(1);
                if (!status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
                var unitPrice = reader.GetDecimal(0);
                total += unitPrice * item.Quantity;
                await reader.DisposeAsync();
            }

            var orderId = $"ord_{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            await using var insert = new NpgsqlCommand("INSERT INTO buy_orders (order_id, customer_id, status, total_amount, currency, delivery_address) VALUES (@orderId, @customerId, 'PENDING', @totalAmount, 'USD', @deliveryAddress) RETURNING created_at", conn, tx);
            insert.Parameters.AddWithValue("orderId", orderId);
            insert.Parameters.AddWithValue("customerId", request.CustomerId);
            insert.Parameters.AddWithValue("totalAmount", total);
            insert.Parameters.AddWithValue("deliveryAddress", request.DeliveryAddress);
            _logger.LogInformation("buy_orders INSERT");
            var createdAt = (DateTimeOffset)(await insert.ExecuteScalarAsync(cancellationToken) ?? DateTimeOffset.UtcNow);
            foreach (var item in request.Items)
            {
                await using var itemCmd = new NpgsqlCommand("INSERT INTO buy_order_items (order_id, product_id, quantity, unit_price) VALUES (@orderId, @productId, @quantity, @unitPrice)", conn, tx);
                itemCmd.Parameters.AddWithValue("orderId", orderId);
                itemCmd.Parameters.AddWithValue("productId", item.ProductId);
                itemCmd.Parameters.AddWithValue("quantity", item.Quantity);
                itemCmd.Parameters.AddWithValue("unitPrice", 0m);
                _logger.LogInformation("buy_order_items INSERT");
                await itemCmd.ExecuteNonQueryAsync(cancellationToken);
            }
            await tx.CommitAsync(cancellationToken);
            return new CreateBuyOrderResponse { OrderId = orderId, CustomerId = request.CustomerId, Status = "PENDING", TotalAmount = total, Currency = "USD", CreatedAt = createdAt };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CreateSellRequestResponse> CreateSellRequestAsync(CreateSellRequestRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            var sellRequestId = $"sel_{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            await using var cmd = new NpgsqlCommand("INSERT INTO sell_requests (sell_request_id, customer_id, product_name, gold_purity, weight_grams, expected_price, notes, status) VALUES (@sellRequestId, @customerId, @productName, @goldPurity, @weightGrams, @expectedPrice, @notes, 'SUBMITTED') RETURNING created_at", conn, tx);
            cmd.Parameters.AddWithValue("sellRequestId", sellRequestId);
            cmd.Parameters.AddWithValue("customerId", request.CustomerId);
            cmd.Parameters.AddWithValue("productName", request.ProductName);
            cmd.Parameters.AddWithValue("goldPurity", request.GoldPurity);
            cmd.Parameters.AddWithValue("weightGrams", request.WeightGrams);
            cmd.Parameters.AddWithValue("expectedPrice", (object?)request.ExpectedPrice ?? DBNull.Value);
            cmd.Parameters.AddWithValue("notes", (object?)request.Notes ?? DBNull.Value);
            _logger.LogInformation("sell_requests INSERT");
            var createdAt = (DateTimeOffset)(await cmd.ExecuteScalarAsync(cancellationToken) ?? DateTimeOffset.UtcNow);
            await tx.CommitAsync(cancellationToken);
            return new CreateSellRequestResponse { SellRequestId = sellRequestId, CustomerId = request.CustomerId, Status = "SUBMITTED", CreatedAt = createdAt };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BuyOrderResponse> GetBuyOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        ValidateIdentifier(orderId, nameof(orderId));
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT order_id, customer_id, status, total_amount, currency, delivery_address, created_at FROM buy_orders WHERE order_id = @orderId", conn);
        cmd.Parameters.AddWithValue("orderId", orderId);
        _logger.LogInformation("buy_orders SELECT");
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new KeyNotFoundException();
        return new BuyOrderResponse { OrderId = reader.GetString(0), CustomerId = reader.GetString(1), Status = reader.GetString(2), TotalAmount = reader.GetDecimal(3), Currency = reader.GetString(4), DeliveryAddress = reader.GetString(5), CreatedAt = reader.GetFieldValue<DateTimeOffset>(6), Items = new List<BuyOrderItemResponse>() };
    }

    public async Task<SellRequestResponse> GetSellRequestAsync(string sellRequestId, CancellationToken cancellationToken)
    {
        ValidateIdentifier(sellRequestId, nameof(sellRequestId));
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT sell_request_id, customer_id, status, product_name, gold_purity, weight_grams, expected_price, notes, created_at FROM sell_requests WHERE sell_request_id = @sellRequestId", conn);
        cmd.Parameters.AddWithValue("sellRequestId", sellRequestId);
        _logger.LogInformation("sell_requests SELECT");
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new KeyNotFoundException();
        return new SellRequestResponse { SellRequestId = reader.GetString(0), CustomerId = reader.GetString(1), Status = reader.GetString(2), ProductName = reader.GetString(3), GoldPurity = reader.GetString(4), WeightGrams = reader.GetDecimal(5), ExpectedPrice = reader.IsDBNull(6) ? null : reader.GetDecimal(6), Notes = reader.IsDBNull(7) ? null : reader.GetString(7), CreatedAt = reader.GetFieldValue<DateTimeOffset>(8) };
    }

    private static void ValidateRequest(CreateBuyOrderRequest request)
    {
        _ = new List<ValidationResult>();
        if (string.IsNullOrWhiteSpace(request.CustomerId)) throw new ArgumentException(nameof(request.CustomerId));
        if (string.IsNullOrWhiteSpace(request.DeliveryAddress)) throw new ArgumentException(nameof(request.DeliveryAddress));
        if (request.Items.Count < 1) throw new ArgumentException(nameof(request.Items));
    }

    private static void ValidateRequest(CreateSellRequestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId)) throw new ArgumentException(nameof(request.CustomerId));
        if (string.IsNullOrWhiteSpace(request.ProductName)) throw new ArgumentException(nameof(request.ProductName));
        if (string.IsNullOrWhiteSpace(request.GoldPurity)) throw new ArgumentException(nameof(request.GoldPurity));
        if (request.WeightGrams <= 0) throw new ArgumentException(nameof(request.WeightGrams));
        if (request.ExpectedPrice.HasValue && request.ExpectedPrice.Value < 0) throw new ArgumentException(nameof(request.ExpectedPrice));
    }

    private static void ValidateIdentifier(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(fieldName);
    }

    private static int ParsePositiveInt(IDictionary<string, string>? queryParams, string key, int defaultValue)
    {
        if (queryParams == null || !queryParams.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return defaultValue;
        if (!int.TryParse(raw, out var value) || value <= 0) throw new ArgumentException(key);
        return value;
    }
}