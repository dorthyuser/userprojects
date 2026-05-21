using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace synctesting1050.Models;

public sealed class ZohoUsersListQuery
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TypeEnum
    {
        AllUsers,
        ActiveUsers,
        DeactiveUsers,
        ConfirmedUsers,
        AdminUsers
    }

    public TypeEnum? Type { get; set; }

    [Range(1, int.MaxValue)]
    public int? Page { get; set; }

    [Range(1, 200)]
    public int? PerPage { get; set; }
}
