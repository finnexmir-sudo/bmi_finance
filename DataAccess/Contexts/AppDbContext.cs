using FinNex.Domain;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
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