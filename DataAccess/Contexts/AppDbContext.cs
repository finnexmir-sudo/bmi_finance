using System.Reflection.Emit;
using FinNex.Domain;
using FinNex.Domain.Entities;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Entities.Structure;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FinNex.DataAccess.Contexts;

// IdentityDbContext-dən miras alırıq ki, AppUser və AppRole (int ID ilə) işləsin
public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Bank> Banklar { get; set; }
    public DbSet<BankHesabi> BankHesablari { get; set; }
    public DbSet<Musteri> Musteriler { get; set; }
    public DbSet<MusteriHesabi> MusteriHesablari { get; set; }
    public DbSet<OdenisTapsirigi> OdenisTapsiriqlari { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<OdenisTapsirigiNomresi> OdenisTapsirigiNomreleri { get; set; }
    public DbSet<Valyuta> Valyutalar { get; set; } = null!;

    public DbSet<Sobe> Sobeler => Set<Sobe>();
    public DbSet<SenedNovu> SenedNovleri => Set<SenedNovu>();
    public DbSet<Sened> Senedler => Set<Sened>();
    public DbSet<SenedFayl> SenedFayllar => Set<SenedFayl>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Müştərilər üçün artıq yazmışdıq (Yenə də yoxla)
        builder.Entity<OdenisTapsirigi>()
            .HasOne(x => x.OduyenMusteri)
            .WithMany()
            .HasForeignKey(x => x.OduyenMusteriId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<OdenisTapsirigi>()
            .HasOne(x => x.AlanMusteri)
            .WithMany()
            .HasForeignKey(x => x.AlanMusteriId)
            .OnDelete(DeleteBehavior.NoAction);

        // ƏSAS BURADIR: Hesablar üçün də NoAction əlavə edirik
        builder.Entity<OdenisTapsirigi>()
            .HasOne(x => x.OduyenHesab)
            .WithMany()
            .HasForeignKey(x => x.OduyenHesabId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<OdenisTapsirigi>()
            .HasOne(x => x.AlanHesab)
            .WithMany()
            .HasForeignKey(x => x.AlanHesabId)
            .OnDelete(DeleteBehavior.NoAction);

        // Bank FK-lari
        builder.Entity<OdenisTapsirigi>()
            .HasOne(x => x.OduyenBank)
            .WithMany()
            .HasForeignKey(x => x.OduyenBankId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<OdenisTapsirigi>()
            .HasOne(x => x.AlanBank)
            .WithMany()
            .HasForeignKey(x => x.AlanBankId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Sobe>(e =>
        {
            e.ToTable("Sobeler");
            e.Property(x => x.Kod).HasMaxLength(20).IsRequired();
            e.Property(x => x.Ad).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Kod).IsUnique();
            e.HasQueryFilter(x => !x.Silinib);
        });

        builder.Entity<SenedNovu>(e =>
        {
            e.ToTable("SenedNovleri");
            e.Property(x => x.Kod).HasMaxLength(30).IsRequired();
            e.Property(x => x.Ad).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.SobeId, x.Kod }).IsUnique();
            e.HasQueryFilter(x => !x.Silinib);
        });

        builder.Entity<Sened>(e =>
        {
            e.ToTable("Senedler");
            e.Property(x => x.Basliq).HasMaxLength(250).IsRequired();
            e.Property(x => x.AcarSoz).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.AcarSoz);
            e.HasQueryFilter(x => !x.Silinib);

            e.HasOne(x => x.Sobe).WithMany(x => x.Senedler).HasForeignKey(x => x.SobeId);
            e.HasOne(x => x.SenedNovu).WithMany().HasForeignKey(x => x.SenedNovuId);
        });

        builder.Entity<SenedFayl>(e =>
        {
            e.ToTable("SenedFayllar");
            e.Property(x => x.OriginalAd).HasMaxLength(260).IsRequired();
            e.Property(x => x.StoredAd).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
            e.Property(x => x.Yol).HasMaxLength(500).IsRequired();

            e.HasIndex(x => new { x.SenedId, x.VersiyaNo }).IsUnique();
            e.HasQueryFilter(x => !x.Silinib);

            e.HasOne(x => x.Sened).WithMany(x => x.Fayllar).HasForeignKey(x => x.SenedId);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.Property(x => x.UserId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.HasQueryFilter(x => !x.Silinib);
        });
    }

    // ƏN VACİB HİSSƏ: SaveChanges zamanı avtomatik Audit məlumatlarının doldurulması
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // BaseEntity-dən miras alan və dəyişiklik edilən bütün obyektləri tapırıq
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                // Yeni məlumat əlavə olunanda
                case EntityState.Added:
                    entry.Entity.YaradilmaTarixi = DateTime.Now;
                    entry.Entity.Silinib = false;
                    // YaradanIcraciId-ni Program.cs-də UserAccessor qurandan sonra bura bağlayacağıq
                    break;

                // Məlumat yenilənəndə
                case EntityState.Modified:
                    // Əgər obyekt silinməyibsə, yenilənmə tarixini qoy
                    if (!entry.Entity.Silinib)
                    {
                        entry.Entity.YenilenmeTarixi = DateTime.Now;
                    }
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}