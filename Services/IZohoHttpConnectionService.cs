using synctesting1050.Models;

namespace synctesting1050.Services;

public interface IZohoHttpConnectionService
{
    Task<ApiResult> CreateUserAsync(CreateUserRequest request, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetZohoUserAsync(string zohoId, string? correlationId, string? ifModifiedSince, CancellationToken cancellationToken = default);
    Task<ApiResult> ListZohoUsersAsync(ZohoUsersListQuery query, string? correlationId, string? ifModifiedSince, CancellationToken cancellationToken = default);
    Task<ApiResult> SyncUsersAsync(SyncUsersRequest request, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetLocalUsersAsync(LocalUsersQuery query, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetLocalUserByPkAsync(long userPk, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetLocalUserByZohoUidAsync(string zohoUid, string? correlationId, CancellationToken cancellationToken = default);
}