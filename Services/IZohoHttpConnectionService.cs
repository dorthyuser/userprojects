using zohotesting.Models;

namespace zohotesting.Services;

public interface IZohoHttpConnectionService
{
    Task<ServiceResult> CreateUserAsync(CreateZohoUserRequest request, string? correlationId, CancellationToken cancellationToken);
    Task<ServiceResult> GetZohoUsersAsync(string? zohoId, string? type, int? page, int? perPage, string? ifModifiedSince, string? correlationId, CancellationToken cancellationToken);
    Task<ServiceResult> SyncUsersAsync(SyncZohoUsersRequest? request, string? correlationId, CancellationToken cancellationToken);
    Task<ServiceResult> GetLocalUsersAsync(LocalUsersQuery query, string? correlationId, CancellationToken cancellationToken);
    Task<ServiceResult> GetLocalUserByPkAsync(int userPk, string? correlationId, CancellationToken cancellationToken);
    Task<ServiceResult> GetLocalUserByZohoUidAsync(string zohoUid, string? correlationId, CancellationToken cancellationToken);
}