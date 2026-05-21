using synctesting1109.Models;

namespace synctesting1109.Services;

public interface IZohoHttpConnectionService
{
    Task<ApiResult> CreateUserAsync(ZohoCreateUserRequest model, string? authorization, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetZohoUsersAsync(ZohoGetUsersQuery query, string? authorization, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetZohoUserByIdAsync(string zohoId, ZohoGetUsersQuery query, string? authorization, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> SyncUsersAsync(ZohoSyncUsersRequest model, string? authorization, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetLocalUsersAsync(LocalUsersQuery query, string? authorization, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetLocalUserByPkAsync(long userPk, string? authorization, string? correlationId, CancellationToken cancellationToken = default);
    Task<ApiResult> GetLocalUserByZohoUidAsync(string zohoUid, string? authorization, string? correlationId, CancellationToken cancellationToken = default);
}
