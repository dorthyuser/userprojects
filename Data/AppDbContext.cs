using azuresharpapi153.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace azuresharpapi153.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TravelcardEntity> Travelcards => Set<TravelcardEntity>();
    public DbSet<CardholderEntity> Cardholders => Set<CardholderEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<TravelcardTypeEnum>("public", "travelcard_type_enum");
        modelBuilder.HasPostgresEnum<CardholderTypeEnum>("public", "cardholder_type_enum");

        modelBuilder.Entity<TravelcardEntity>(entity =>
        {
            entity.ToTable("travelcards", "public");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TravelcardType).HasColumnName("travelcard_type").HasColumnType("travelcard_type_enum");
            entity.Property(x => x.TravelcardValidFrom).HasColumnName("travelcard_valid_from");
            entity.Property(x => x.TravelcardValidTo).HasColumnName("travelcard_valid_to");
            entity.Property(x => x.TravelcardName).HasColumnName("travelcard_name").HasMaxLength(255);
            entity.Property(x => x.TravelcardNumber).HasColumnName("travelcard_number").HasMaxLength(22);
            entity.Property(x => x.TravelcardRequestedDate).HasColumnName("travelcard_requested_date");
            entity.Property(x => x.TravelcardTransactionReference).HasColumnName("travelcard_transaction_reference").HasMaxLength(15).IsFixedLength();
            entity.Property(x => x.TravelcardUsableTo).HasColumnName("travelcard_usable_to");
        });

        modelBuilder.Entity<CardholderEntity>(entity =>
        {
            entity.ToTable("cardholders", "public");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TravelcardId).HasColumnName("travelcard_id");
            entity.Property(x => x.CardholderTitle).HasColumnName("cardholder_title").HasMaxLength(15);
            entity.Property(x => x.CardholderForename).HasColumnName("cardholder_forename").HasMaxLength(100);
            entity.Property(x => x.CardholderSurname).HasColumnName("cardholder_surname").HasMaxLength(100);
            entity.Property(x => x.CardholderType).HasColumnName("cardholder_type").HasColumnType("cardholder_type_enum");
            entity.Property(x => x.CardholderPhotoName).HasColumnName("cardholder_photo_name").HasMaxLength(100);
            entity.Property(x => x.CardholderPhotoRRSKey).HasColumnName("cardholder_photo_rrs_key").HasMaxLength(42);
            entity.Property(x => x.CardholderPhotoURL).HasColumnName("cardholder_photo_url").HasMaxLength(2048);
            entity.Property(x => x.CardholderPhotoKey).HasColumnName("cardholder_photo_key").HasMaxLength(42);

            entity.HasOne(x => x.Travelcard)
                .WithMany(x => x.Cardholders)
                .HasForeignKey(x => x.TravelcardId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
