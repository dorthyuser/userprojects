using Microsoft.EntityFrameworkCore;
using AgeApi.Entities;

namespace AgeApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UserEntity> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Important: map entity properties to lowercase DB column names to avoid mapping errors
            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.DateOfBirth).HasColumnName("dateofbirth");
                entity.Property(e => e.RequestedAt).HasColumnName("requestedat");
            });
        }
    }
}
