using Microsoft.EntityFrameworkCore;
using SalesforceAccountFunctions.Models;

namespace SalesforceAccountFunctions.Data
{
    public class SalesforceDbContext : DbContext
    {
        public SalesforceDbContext(DbContextOptions<SalesforceDbContext> options) : base(options)
        {
        }

        public DbSet<AccountEntity> Accounts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AccountEntity>(entity =>
            {
                entity.ToTable("account");

                // Column mappings to handle lowercase database columns
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.Website).HasColumnName("website");
                entity.Property(e => e.BillingCity).HasColumnName("billingcity");
                entity.Property(e => e.CreatedDate).HasColumnName("createddate");
            });
        }
    }
}
