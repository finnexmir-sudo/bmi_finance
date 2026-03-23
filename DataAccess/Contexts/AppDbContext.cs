using System.Reflection.Emit;
using FinNex.Domain;
using FinNex.Domain.Entities;
using FinNex.Domain.Entities.HR;
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
    public DbSet<Departament> Departments { get; set; }
    public DbSet<OdenisTapsirigiNomresi> OdenisTapsirigiNomreleri { get; set; }
    public DbSet<Valyuta> Valyutalar { get; set; } = null!;

    public DbSet<Sened> Senedler { get; set; }
    public DbSet<SenedFayl> SenedFayllar { get; set; }
    public DbSet<SenedNovu> SenedNovleri { get; set; }
    public DbSet<Departament> Departamentler { get; set; }
    public DbSet<Tag> Tagler { get; set; }
    public DbSet<SenedTagMap> SenedTagMaps { get; set; }
    public DbSet<SenedAccess> SenedAccessler { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    public DbSet<UserDepartment> UserDepartments { get; set; }

    // =====================
    // HR Module
    // =====================

    public DbSet<Isci> Isciler { get; set; }
    public DbSet<Vezife> Vezifeler { get; set; }
    public DbSet<Maas> Maaslar { get; set; }
    public DbSet<Davamiyyet> Davamiyyetler { get; set; }
    // Məzuniyyət modulu üçün yeni DbSet-lər
    public DbSet<Mezuniyyet> Mezuniyyetler { get; set; }
    public DbSet<MezuniyyetBalans> MezuniyyetBalanslari { get; set; }
    public DbSet<BayramGunu> BayramGunleri { get; set; }
    public DbSet<Icaze> Icazeler { get; set; }

    public DbSet<IsciTeyinat> IsciTeyinatlari { get; set; }
    public DbSet<IsciStrukturRolu> IsciStrukturRollari { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

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

        // -------------------------
        // SenedTagMap (Many-to-Many)
        // -------------------------
        builder.Entity<SenedTagMap>()
            .HasKey(x => new { x.SenedId, x.TagId });

        builder.Entity<SenedTagMap>()
            .HasOne(x => x.Sened)
            .WithMany(x => x.SenedTagMaps)
            .HasForeignKey(x => x.SenedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SenedTagMap>()
            .HasOne(x => x.Tag)
            .WithMany(x => x.SenedTagMaps)
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        
        // -------------------------
        // SenedFayl Unique Version
        // -------------------------
        builder.Entity<SenedFayl>()
            .HasIndex(x => new { x.SenedId, x.VersiyaNo })
            .IsUnique();

        // -------------------------
        // Sened Relationships
        // -------------------------
        builder.Entity<Sened>()
            .HasOne(x => x.Department)
            .WithMany(x => x.Senedler)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Sened>()
            .HasOne(x => x.SenedNovu)
            .WithMany()
            .HasForeignKey(x => x.SenedNovuId)
            .OnDelete(DeleteBehavior.Restrict);

        // -------------------------
        // SenedAccess
        // -------------------------
        builder.Entity<SenedAccess>()
            .HasOne(x => x.Sened)
            .WithMany()
            .HasForeignKey(x => x.SenedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Vezife>()
    .HasIndex(x => x.Ad)
    .IsUnique();
        
        builder.Entity<Isci>()
            .HasOne(x => x.AppUser)
            .WithOne()
            .HasForeignKey<Isci>(x => x.AppUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Isci>()
            .HasIndex(x => x.FIN)
            .IsUnique();
        builder.Entity<Maas>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Maas>()
            .HasIndex(x => new { x.IsciId, x.Il, x.Ay })
            .IsUnique();
        builder.Entity<Davamiyyet>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Davamiyyet>()
            .HasIndex(x => new { x.IsciId, x.Tarix })
            .IsUnique();
        builder.Entity<Mezuniyyet>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Cascade);

        // İcazə cədvəlində Isci və digər işçi əlaqələri
        builder.Entity<Icaze>()
    .HasOne(x => x.Isci)
    .WithMany()
    .HasForeignKey(x => x.IsciId)
    .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Icaze>()
            .HasOne(x => x.EvezEdenIsci)
            .WithMany()
            .HasForeignKey(x => x.EvezEdenIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Icaze>()
            .HasOne(x => x.SobeReisi)
            .WithMany()
            .HasForeignKey(x => x.SobeReisiId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Icaze>()
            .HasOne(x => x.Rehber)
            .WithMany()
            .HasForeignKey(x => x.RehberId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Icaze>()
            .HasOne(x => x.HrTesdiqleyen)
            .WithMany()
            .HasForeignKey(x => x.HrId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<AppUser>()
    .HasOne(x => x.Isci)
    .WithOne(x => x.AppUser)
    .HasForeignKey<Isci>(x => x.AppUserId)
    .OnDelete(DeleteBehavior.SetNull);

        // Məzuniyyət balansı üçün unikal indeks (Bir işçinin bir ildə yalnız bir balansı ola bilər)
        builder.Entity<MezuniyyetBalans>()
            .HasIndex(x => new { x.IsciId, x.Il })
            .IsUnique();

        // Məzuniyyət cədvəlində Isci və EvezEdenIsci əlaqələri
        builder.Entity<Mezuniyyet>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Mezuniyyet>()
            .HasOne(x => x.EvezEdenIsci)
            .WithMany()
            .HasForeignKey(x => x.EvezEdenIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<IsciTeyinat>()
    .HasOne(x => x.Isci)
    .WithMany(x => x.IsciTeyinatlari)
    .HasForeignKey(x => x.IsciId)
    .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<IsciTeyinat>()
            .HasOne(x => x.Departament)
            .WithMany()
            .HasForeignKey(x => x.DepartamentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<IsciTeyinat>()
            .HasOne(x => x.Vezife)
            .WithMany()
            .HasForeignKey(x => x.VezifeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<IsciStrukturRolu>()
    .HasOne(x => x.Isci)
    .WithMany()
    .HasForeignKey(x => x.IsciId)
    .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<IsciStrukturRolu>()
            .HasOne(x => x.Departament)
            .WithMany()
            .HasForeignKey(x => x.DepartamentId)
            .OnDelete(DeleteBehavior.NoAction);


        builder.Entity<UserDepartment>(entity =>
        {
            entity.ToTable("UserDepartments");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.User)
                  .WithMany(u => u.UserDepartments)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Department)
                  .WithMany(d => d.UserDepartments)
                  .HasForeignKey(x => x.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.Esasdir)
                  .HasDefaultValue(false);
        });
        builder.Entity<MusteriHesabi>()
     .HasIndex(x => new { x.MusteriId, x.Iban })
     .IsUnique();


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