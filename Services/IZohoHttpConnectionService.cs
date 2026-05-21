using synctesting1109.Models;

namespace synctesting1109.Services;

public interface IZohoHttpConnectionService
{
    Task<ApiResult> CreateUserAsync(HttpRequest request, ZohoCreateUserRequest model, CancellationToken cancellationToken = default);
    Task<ApiResult> GetZohoUsersAsync(HttpRequest request, ZohoGetUsersQuery query, CancellationToken cancellationToken = default);
    Task<ApiResult> GetZohoUserByIdAsync(HttpRequest request, string zohoId, ZohoGetUsersQuery query, CancellationToken cancellationToken = default);
    Task<ApiResult> SyncUsersAsync(HttpRequest request, ZohoSyncUsersRequest model, CancellationToken cancellationToken = default);
    Task<ApiResult> GetLocalUsersAsync(HttpRequest request, LocalUsersQuery query, CancellationToken cancellationToken = default);
    Task<ApiResult> GetLocalUserByPkAsync(HttpRequest request, string userPk, CancellationToken cancellationToken = default);
    Task<ApiResult> GetLocalUserByZohoUidAsync(HttpRequest request, string zohoUid, CancellationToken cancellationToken = default);
}