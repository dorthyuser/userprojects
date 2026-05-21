using synctesting1109.Models;

namespace synctesting1109.Services;

public interface IZohoHttpConnectionService
{
    Task<ApiResult> CreateUserAsync(ZohoCreateUserRequest model, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetZohoUsersAsync(ZohoGetUsersQuery query, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetZohoUserByIdAsync(string zohoId, ZohoGetUsersQuery query, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> SyncUsersAsync(ZohoSyncUsersRequest model, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetLocalUsersAsync(LocalUsersQuery query, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetLocalUserByPkAsync(long userPk, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetLocalUserByZohoUidAsync(string zohoUid, string? correlationId, CancellationToken cancellationToken = default);
}
