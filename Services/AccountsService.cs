using System.Text;
using System.Text.Json;

namespace BusTravelAccountsSaMainLambda;

public sealed class AccountsService
{
    private static readonly PostgresClient PostgresClient = new();

    public async Task<ApiResult> GetAccountsAsync(APIGatewayProxyRequest request, ILambdaContext context, CancellationToken cancellationToken)
    {
        var details = RequestContextFactory.Create(request);
        context.Logger.LogLine("INFO START - Request received details=" + LogMaskHelper.SerializeBasicDetails(details) + " payload=" + LogMaskHelper.MaskPayload(request.Body));

        var email = QueryHelper.GetQueryValue(request.QueryStringParameters, "email") ?? string.Empty;
        var enrichedEmail = email.Replace("'", "\\'");

        context.Logger.LogLine("DEBUG Before Request - ListAccountsByEmail personEmail=***MASKED***");
        var rows = await PostgresClient.GetAccountsByEmailAsync(enrichedEmail, cancellationToken);
        context.Logger.LogLine("DEBUG After Request - ListAccountsByEmail");

        if (rows.Count == 0)
        {
            context.Logger.LogLine("INFO END - Request processing completed payload=[] details=" + LogMaskHelper.SerializeBasicDetails(details));
            return new ApiResult(204, "[]");
        }

        var response = rows.Select(MapAccountResponse).ToList();
        var body = JsonSerializer.Serialize(response, JsonOptionsHelper.Options);
        context.Logger.LogLine("INFO END - Request processing completed payload=[...] details=" + LogMaskHelper.SerializeBasicDetails(details));
        return new ApiResult(200, body);
    }

    public async Task<ApiResult> GetAccountByIdAsync(string id, APIGatewayProxyRequest request, ILambdaContext context, CancellationToken cancellationToken)
    {
        var details = RequestContextFactory.Create(request);
        context.Logger.LogLine("INFO START - Request received details=" + LogMaskHelper.SerializeBasicDetails(details) + " payload=" + LogMaskHelper.MaskPayload(request.Body));
        context.Logger.LogLine("DEBUG Before Request - GetAccountDetails AccountId=" + LogMaskHelper.MaskId(id));

        if (!Guid.TryParse(id, out var accountId))
        {
            context.Logger.LogLine("DEBUG After Request - GetAccountDetails");
            context.Logger.LogLine("INFO END - Request processing completed payload=[] details=" + LogMaskHelper.SerializeBasicDetails(details));
            return new ApiResult(204, "[]");
        }

        var row = await PostgresClient.GetAccountByIdAsync(accountId, cancellationToken);
        context.Logger.LogLine("DEBUG After Request - GetAccountDetails");

        if (row is null)
        {
            context.Logger.LogLine("INFO END - Request processing completed payload=[] details=" + LogMaskHelper.SerializeBasicDetails(details));
            return new ApiResult(204, "[]");
        }

        var response = MapAccountResponse(row);
        var body = JsonSerializer.Serialize(response, JsonOptionsHelper.Options);
        context.Logger.LogLine("INFO END - Request processing completed payload={account} details=" + LogMaskHelper.SerializeBasicDetails(details));
        return new ApiResult(200, body);
    }

    public async Task<ApiResult> CreateAccountAsync(APIGatewayProxyRequest request, ILambdaContext context, CancellationToken cancellationToken)
    {
        var details = RequestContextFactory.Create(request);
        context.Logger.LogLine("INFO START - Request received details=" + LogMaskHelper.SerializeBasicDetails(details) + " payload=" + LogMaskHelper.MaskPayload(request.Body));

        var bodyText = BodyHelper.GetBodyText(request);
        var createRequest = JsonSerializer.Deserialize<CreateAccountRequest>(bodyText, JsonOptionsHelper.Options) ?? new CreateAccountRequest();
        var dbRequest = MapCreateRequest(createRequest);

        context.Logger.LogLine("DEBUG Before Request - Create single account payload=" + LogMaskHelper.SerializeCreateOrUpdateRequest(createRequest, null));

        try
        {
            var createdId = Guid.NewGuid();
            await PostgresClient.CreateAccountAsync(createdId, dbRequest, cancellationToken);
            var response = new IdResponse { Id = createdId.ToString() };
            var responseBody = JsonSerializer.Serialize(response, JsonOptionsHelper.Options);
            context.Logger.LogLine("DEBUG After Request - Create single account payload={id}");
            context.Logger.LogLine("INFO END - Request processing completed payload={id} details=" + LogMaskHelper.SerializeBasicDetails(details));
            return new ApiResult(200, responseBody);
        }
        catch (PostgresOperationException ex)
        {
            context.Logger.LogLine("ERROR PostgresOperationException: " + ex.Message);
            var errorBody = JsonSerializer.Serialize(ErrorFactory.Create(400, ex.SqlState + " ERROR", ex.SafeDescription), JsonOptionsHelper.Options);
            context.Logger.LogLine("INFO END - Request processing completed payload={error} details=" + LogMaskHelper.SerializeBasicDetails(details));
            return new ApiResult(400, errorBody);
        }
    }

    public async Task<ApiResult> UpdateAccountAsync(string id, APIGatewayProxyRequest request, ILambdaContext context, CancellationToken cancellationToken)
    {
        var details = RequestContextFactory.Create(request);
        context.Logger.LogLine("INFO START - Request received details=" + LogMaskHelper.SerializeBasicDetails(details) + " payload=" + LogMaskHelper.MaskPayload(request.Body));

        var bodyText = BodyHelper.GetBodyText(request);
        var updateRequest = JsonSerializer.Deserialize<UpdateAccountRequest>(bodyText, JsonOptionsHelper.Options) ?? new UpdateAccountRequest();
        var dbRequest = MapUpdateRequest(updateRequest);

        context.Logger.LogLine("DEBUG Before Request - Update single account AccountId=" + LogMaskHelper.MaskId(id) + " payload=" + LogMaskHelper.SerializeCreateOrUpdateRequest(updateRequest, id));

        if (!Guid.TryParse(id, out var accountId))
        {
            var invalidIdError = JsonSerializer.Serialize(ErrorFactory.Create(400, "400 ERROR", "Account not found"), JsonOptionsHelper.Options);
            context.Logger.LogLine("ERROR Account not found");
            context.Logger.LogLine("INFO END - Request processing completed payload={error} details=" + LogMaskHelper.SerializeBasicDetails(details));
            return new ApiResult(400, invalidIdError);
        }

        try
        {
            var updated = await PostgresClient.UpdateAccountAsync(accountId, dbRequest, cancellationToken);
            context.Logger.LogLine("DEBUG After Request - Update single account payload={result}");

            if (!updated)
            {
                var notFoundError = JsonSerializer.Serialize(ErrorFactory.Create(400, "400 ERROR", "Account not found"), JsonOptionsHelper.Options);
                context.Logger.LogLine("ERROR Account not found");
                context.Logger.LogLine("INFO END - Request processing completed payload={error} details=" + LogMaskHelper.SerializeBasicDetails(details));
                return new ApiResult(400, notFoundError);
            }

            var response = new IdResponse { Id = accountId.ToString() };
            var responseBody = JsonSerializer.Serialize(response, JsonOptionsHelper.Options);
            context.Logger.LogLine("INFO END - Request processing completed payload={id} details=" + LogMaskHelper.SerializeBasicDetails(details));
            return new ApiResult(200, responseBody);
        }
        catch (PostgresOperationException ex)
        {
            context.Logger.LogLine("ERROR PostgresOperationException: " + ex.Message);
            var errorBody = JsonSerializer.Serialize(ErrorFactory.Create(400, ex.SqlState + " ERROR", ex.SafeDescription), JsonOptionsHelper.Options);
            context.Logger.LogLine("INFO END - Request processing completed payload={error} details=" + LogMaskHelper.SerializeBasicDetails(details));
            return new ApiResult(400, errorBody);
        }
    }

    private static AccountResponse MapAccountResponse(AccountRecord row)
    {
        return new AccountResponse
        {
            Id = row.Id.ToString(),
            Salutation = StringHelper.TrimOrNull(row.Salutation),
            FirstName = StringHelper.TrimOrNull(row.FirstName),
            LastName = StringHelper.TrimOrNull(row.LastName),
            PersonBirthdate = row.PersonBirthdate,
            Phone = StringHelper.TrimOrNull(row.Phone),
            MobilePhone = StringHelper.TrimOrNull(row.MobilePhone),
            PersonEmail = StringHelper.TrimOrNull(row.PersonEmail),
            MailingStreet = StringHelper.TrimOrNull(row.MailingStreet),
            MailingPostalCode = StringHelper.TrimOrNull(row.MailingPostalCode),
            MailingCity = StringHelper.TrimOrNull(row.MailingCity),
            MailingCountry = StringHelper.TrimOrNull(row.MailingCountry),
            AccountStatus = StringHelper.TrimOrNull(row.AccountStatus),
            AccountSource = StringHelper.TrimOrNull(row.AccountSource) ?? string.Empty,
            Hotlisted = row.Hotlisted
        };
    }

    private static AccountMutationRequest MapCreateRequest(CreateAccountRequest request)
    {
        return new AccountMutationRequest
        {
            Salutation = StringHelper.TrimOrNull(request.Salutation),
            FirstName = StringHelper.TrimOrNull(request.FirstName),
            LastName = StringHelper.TrimOrNull(request.LastName),
            PersonBirthdate = request.PersonBirthdate,
            Phone = request.Phone is null ? null : StringHelper.TrimOrNull(request.Phone.ToString()),
            MobilePhone = request.MobilePhone is null ? null : StringHelper.TrimOrNull(request.MobilePhone.ToString()),
            PersonEmail = StringHelper.TrimOrNull(request.PersonEmail),
            MailingStreet = StringHelper.TrimOrNull(request.MailingStreet),
            MailingCity = StringHelper.TrimOrNull(request.MailingCity),
            MailingPostalCode = StringHelper.TrimOrNull(request.MailingPostalCode),
            MailingCountry = StringHelper.TrimOrNull(request.MailingCountry),
            AccountStatus = StringHelper.TrimOrNull(request.AccountStatus),
            AccountSource = StringHelper.TrimOrNull(request.AccountSource)
        };
    }

    private static AccountMutationRequest MapUpdateRequest(UpdateAccountRequest request)
    {
        return new AccountMutationRequest
        {
            Salutation = request.Salutation,
            FirstName = StringHelper.TrimOrNull(request.FirstName),
            LastName = StringHelper.TrimOrNull(request.LastName),
            PersonBirthdate = request.PersonBirthdate,
            Phone = request.Phone is null ? null : StringHelper.TrimOrNull(request.Phone.ToString()),
            MobilePhone = request.MobilePhone is null ? null : StringHelper.TrimOrNull(request.MobilePhone.ToString()),
            PersonEmail = StringHelper.TrimOrNull(request.PersonEmail),
            MailingStreet = StringHelper.TrimOrNull(request.MailingStreet),
            MailingCity = StringHelper.TrimOrNull(request.MailingCity),
            MailingPostalCode = StringHelper.TrimOrNull(request.MailingPostalCode),
            MailingCountry = StringHelper.TrimOrNull(request.MailingCountry),
            AccountStatus = StringHelper.TrimOrNull(request.AccountStatus),
            AccountSource = StringHelper.TrimOrNull(request.AccountSource)
        };
    }
}