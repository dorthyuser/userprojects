using System.ComponentModel.DataAnnotations;
using System.Net;
using Buyandsellgold1013Lambda.Models;
using Npgsql;

namespace Buyandsellgold1013Lambda.Services;

public sealed class Service
{
    private readonly NpgsqlDataSource _dataSource;

    public Service(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<ProductListResponse> GetProductsAsync(IDictionary<string, string>? queryParams, string requestId, CancellationToken cancellationToken)
    {
        var page = 1;
        var pageSize = 20;
        if (queryParams != null)
        {
            if (queryParams.TryGetValue("page", out var pageValue) && int.TryParse(pageValue, out var parsedPage)) page = parsedPage;
            if (queryParams.TryGetValue("pageSize", out var pageSizeValue) && int.TryParse(pageSizeValue, out var parsedPageSize)) pageSize = parsedPageSize;
        }
        ValidatePaging(page, pageSize);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT product_id, name, gold_purity, weight_grams, price, currency, status, COUNT(*) OVER() AS total_records FROM products ORDER BY name LIMIT @limit OFFSET @offset", conn);
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var items = new List<ProductResponse>();
        var totalRecords = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            totalRecords = reader.GetInt32(reader.GetOrdinal("total_records"));
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
        return new ProductListResponse { Data = items, Pagination = new PaginationResponse { Page = page, PageSize = pageSize, TotalRecords = totalRecords, TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize) } };
    }

    public async Task<ProductResponse> GetProductAsync(string productId, string requestId, CancellationToken cancellationToken)
    {
        ValidateIdentifier(productId, nameof(productId));
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT product_id, name, gold_purity, weight_grams, price, currency, status FROM products WHERE product_id = @product_id", conn);
        cmd.Parameters.AddWithValue("product_id", productId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new KeyNotFoundException("Product not found.");
        return new ProductResponse
        {
            ProductId = reader.GetString(0),
            Name = reader.GetString(1),
            GoldPurity = reader.GetString(2),
            WeightGrams = reader.GetDecimal(3),
            Price = reader.GetDecimal(4),
            Currency = reader.GetString(5),
            Status = reader.GetString(6)
        };
    }

    public async Task<CreateBuyOrderResponse> CreateBuyOrderAsync(CreateBuyOrderRequest request, string requestId, CancellationToken cancellationToken)
    {
        ValidateBuyOrder(request);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            var orderId = $"ord_{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            decimal total = 0m;
            var items = new List<(string ProductId, int Quantity, decimal UnitPrice)>();
            foreach (var item in request.Items)
            {
                await using var productCmd = new NpgsqlCommand("SELECT price, status FROM products WHERE product_id = @product_id", conn, tx);
                productCmd.Parameters.AddWithValue("product_id", item.ProductId);
                await using var reader = await productCmd.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken)) throw new KeyNotFoundException("Product not found.");
                var status = reader.GetString(1);
                if (!status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Product must be active.");
                var unitPrice = reader.GetDecimal(0);
                total += unitPrice * item.Quantity;
                items.Add((item.ProductId, item.Quantity, unitPrice));
            }
            await using (var insertOrder = new NpgsqlCommand("INSERT INTO buy_orders (order_id, customer_id, status, total_amount, currency, delivery_address) VALUES (@order_id, @customer_id, 'PENDING', @total_amount, 'USD', @delivery_address)", conn, tx))
            {
                insertOrder.Parameters.AddWithValue("order_id", orderId);
                insertOrder.Parameters.AddWithValue("customer_id", request.CustomerId);
                insertOrder.Parameters.AddWithValue("total_amount", total);
                insertOrder.Parameters.AddWithValue("delivery_address", request.DeliveryAddress);
                await insertOrder.ExecuteNonQueryAsync(cancellationToken);
            }
            foreach (var item in items)
            {
                await using var insertItem = new NpgsqlCommand("INSERT INTO buy_order_items (order_id, product_id, quantity, unit_price) VALUES (@order_id, @product_id, @quantity, @unit_price)", conn, tx);
                insertItem.Parameters.AddWithValue("order_id", orderId);
                insertItem.Parameters.AddWithValue("product_id", item.ProductId);
                insertItem.Parameters.AddWithValue("quantity", item.Quantity);
                insertItem.Parameters.AddWithValue("unit_price", item.UnitPrice);
                await insertItem.ExecuteNonQueryAsync(cancellationToken);
            }
            await tx.CommitAsync(cancellationToken);
            return new CreateBuyOrderResponse { OrderId = orderId, CustomerId = request.CustomerId, Status = "PENDING", TotalAmount = total, Currency = "USD", CreatedAt = DateTime.UtcNow };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CreateSellRequestResponse> CreateSellRequestAsync(CreateSellRequest request, string requestId, CancellationToken cancellationToken)
    {
        ValidateSellRequest(request);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        try
        {
            var sellRequestId = $"sel_{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            await using var cmd = new NpgsqlCommand("INSERT INTO sell_requests (sell_request_id, customer_id, product_name, gold_purity, weight_grams, expected_price, notes, status) VALUES (@sell_request_id, @customer_id, @product_name, @gold_purity, @weight_grams, @expected_price, @notes, 'SUBMITTED')", conn, tx);
            cmd.Parameters.AddWithValue("sell_request_id", sellRequestId);
            cmd.Parameters.AddWithValue("customer_id", request.CustomerId);
            cmd.Parameters.AddWithValue("product_name", request.ProductName);
            cmd.Parameters.AddWithValue("gold_purity", request.GoldPurity);
            cmd.Parameters.AddWithValue("weight_grams", request.WeightGrams);
            cmd.Parameters.AddWithValue("expected_price", (object?)request.ExpectedPrice ?? DBNull.Value);
            cmd.Parameters.AddWithValue("notes", (object?)request.Notes ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return new CreateSellRequestResponse { SellRequestId = sellRequestId, CustomerId = request.CustomerId, Status = "SUBMITTED", CreatedAt = DateTime.UtcNow };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BuyOrderResponse> GetBuyOrderAsync(string orderId, string requestId, CancellationToken cancellationToken)
    {
        ValidateIdentifier(orderId, nameof(orderId));
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT order_id, customer_id, status, total_amount, currency, delivery_address, created_at FROM buy_orders WHERE order_id = @order_id", conn);
        cmd.Parameters.AddWithValue("order_id", orderId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new KeyNotFoundException("Order not found.");
        var response = new BuyOrderResponse
        {
            OrderId = reader.GetString(0),
            CustomerId = reader.GetString(1),
            Status = reader.GetString(2),
            TotalAmount = reader.GetDecimal(3),
            Currency = reader.GetString(4),
            DeliveryAddress = reader.GetString(5),
            CreatedAt = reader.GetDateTime(6)
        };
        await reader.CloseAsync();
        await using var itemsCmd = new NpgsqlCommand("SELECT product_id, quantity, unit_price FROM buy_order_items WHERE order_id = @order_id ORDER BY buy_order_item_id", conn);
        itemsCmd.Parameters.AddWithValue("order_id", orderId);
        await using var itemsReader = await itemsCmd.ExecuteReaderAsync(cancellationToken);
        while (await itemsReader.ReadAsync(cancellationToken)) response.Items.Add(new BuyOrderItemResponse { ProductId = itemsReader.GetString(0), Quantity = itemsReader.GetInt32(1), UnitPrice = itemsReader.GetDecimal(2) });
        return response;
    }

    public async Task<SellRequestResponse> GetSellRequestAsync(string sellRequestId, string requestId, CancellationToken cancellationToken)
    {
        ValidateIdentifier(sellRequestId, nameof(sellRequestId));
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT sell_request_id, customer_id, status, product_name, gold_purity, weight_grams, expected_price, notes, created_at FROM sell_requests WHERE sell_request_id = @sell_request_id", conn);
        cmd.Parameters.AddWithValue("sell_request_id", sellRequestId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new KeyNotFoundException("Sell request not found.");
        return new SellRequestResponse
        {
            SellRequestId = reader.GetString(0),
            CustomerId = reader.GetString(1),
            Status = reader.GetString(2),
            ProductName = reader.GetString(3),
            GoldPurity = reader.GetString(4),
            WeightGrams = reader.GetDecimal(5),
            ExpectedPrice = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
            Notes = reader.IsDBNull(7) ? null : reader.GetString(7),
            CreatedAt = reader.GetDateTime(8)
        };
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page <= 0) throw new ValidationException("page must be positive.");
        if (pageSize <= 0) throw new ValidationException("pageSize must be positive.");
    }

    private static void ValidateIdentifier(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64) throw new ValidationException($"{fieldName} is invalid.");
    }

    private static void ValidateBuyOrder(CreateBuyOrderRequest request)
    {
        Validator.ValidateObject(request, new ValidationContext(request), true);
        if (request.Items.Count < 1) throw new ValidationException("items must contain at least one item.");
        foreach (var item in request.Items)
        {
            Validator.ValidateObject(item, new ValidationContext(item), true);
            if (item.Quantity <= 0) throw new ValidationException("quantity must be greater than zero.");
        }
    }

    private static void ValidateSellRequest(CreateSellRequest request)
    {
        Validator.ValidateObject(request, new ValidationContext(request), true);
        if (request.WeightGrams <= 0) throw new ValidationException("weightGrams must be greater than zero.");
        if (request.ExpectedPrice.HasValue && request.ExpectedPrice.Value < 0) throw new ValidationException("expectedPrice must be non-negative.");
    }
}