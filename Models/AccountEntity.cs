using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalesforceAccountFunctions.Models
{
    [Table("account")]
    public class AccountEntity
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("website")]
        public string? Website { get; set; }

        [Column("billingcity")]
        public string? BillingCity { get; set; }

        [Column("createddate")]
        public DateTime CreatedDate { get; set; }
    }
}
