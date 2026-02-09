using System;
using Microsoft.EntityFrameworkCore;
using TravelCardFunctionApp.Models;

namespace TravelCardFunctionApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<TravelCard> TravelCards { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TravelCard>(entity =>
            {
                entity.ToTable("travelcard");
                entity.HasKey(e => e.Id);

                // Map properties to lowercase DB columns
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CardNumber).HasColumnName("card_number").HasMaxLength(100).IsRequired();
                entity.Property(e => e.HolderName).HasColumnName("holder_name").HasMaxLength(200).IsRequired();
                entity.Property(e => e.Balance).HasColumnName("balance");
                entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });
        }
    }
}
