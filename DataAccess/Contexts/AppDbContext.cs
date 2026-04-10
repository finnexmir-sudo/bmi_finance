using FinNex.Domain;
using FinNex.Domain.Entities;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Entities.Structure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Reflection.Emit;
using System.Security.Claims;

namespace FinNex.DataAccess.Contexts;

// IdentityDbContext-dən miras alırıq ki, AppUser və AppRole (int ID ilə) işləsin
public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
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
    public DbSet<SenedDovriyyesiIstifadeciIcazesi> senedDovriyyesiIstifadeciIcazeleri { get; set; }
    public DbSet<Departament> Departamentler { get; set; }
    public DbSet<Tag> Tagler { get; set; }
    public DbSet<SenedTagMap> SenedTagMaps { get; set; }
    public DbSet<SenedAccess> SenedAccessler { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SenedSablon> SenedSablonlar { get; set; }

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

    public DbSet<IsciMaliye> IsciMaliyeleri { get; set; }
    public DbSet<MaasDetay> MaasDetaylari { get; set; }
    public DbSet<MaasNovu> MaasNovleri { get; set; }
    public DbSet<IsciMaasTarixcesi> IsciMaasTarixceleri { get; set; }
    public DbSet<MaasParametri> MaasParametrleri { get; set; }

    public DbSet<Mesaj> Mesajlar { get; set; }
    public DbSet<Bildiris> Bildirisler { get; set; }
    public DbSet<EvezediciTesdiq> EvezediciTesdiqler { get; set; }
    public DbSet<Tapshiriq> Tapshiriqlar { get; set; }
    public DbSet<TapshiriqSherh> TapshiriqSherhler { get; set; }
    public DbSet<Gorush> Gorushler { get; set; }
    public DbSet<GorushIshtirakci> GorushIshtirakcilar { get; set; }
    public DbSet<Xatirlatma> Xatirlatmalar { get; set; }
    public DbSet<LoginLog> LoginLogs { get; set; }

    // Hesabat İzləmə
    public DbSet<HesabatKateqoriyasi> HesabatKateqoriyalari { get; set; }
    public DbSet<HesabatSablonu> HesabatSablonlari { get; set; }
    public DbSet<HesabatTapshiriq> HesabatTapshiriqlari { get; set; }

    // Performans
    public DbSet<PerformansQiymetlendirme> PerformansQiymetlendirmeler { get; set; }
    public DbSet<PerformansKriteriya> PerformansKriteriyalar { get; set; }

    // Təlim
    public DbSet<Telim> Telimler { get; set; }
    public DbSet<TelimIshtiraki> TelimIshtiraklar { get; set; }
    public DbSet<Sertifikat> Sertifikatlar { get; set; }

    // Xərc
    public DbSet<XercKateqoriyasi> XercKateqoriyalari { get; set; }
    public DbSet<Xerc> Xercler { get; set; }

    // Büdcə
    public DbSet<Budce> Budceler { get; set; }

    // Elan
    public DbSet<Elan> Elanlar { get; set; }

    // Chat
    public DbSet<ChatMesaj> ChatMesajlar { get; set; }

    // Kredit
    public DbSet<FinNex.Domain.Entities.Kredit.KreditMuraciet> KreditMuracietler { get; set; }

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

        // Mesaj
        builder.Entity<Mesaj>()
            .HasOne(m => m.GonderenIsci)
            .WithMany()
            .HasForeignKey(m => m.GonderenIsciId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Mesaj>()
            .HasOne(m => m.AlanIsci)
            .WithMany()
            .HasForeignKey(m => m.AlanIsciId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Mesaj>()
            .HasOne(m => m.CavabVerdigiMesaj)
            .WithMany(m => m.Cavablar)
            .HasForeignKey(m => m.CavabVerdigiMesajId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bildiris
        builder.Entity<Bildiris>()
            .HasOne(b => b.Isci)
            .WithMany()
            .HasForeignKey(b => b.IsciId)
            .OnDelete(DeleteBehavior.Restrict);

        // EvezediciTesdiq
        builder.Entity<EvezediciTesdiq>()
            .HasOne(e => e.EvezediciIsci)
            .WithMany()
            .HasForeignKey(e => e.EvezediciIsciId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<EvezediciTesdiq>()
            .HasOne(e => e.Mezuniyyet)
            .WithMany()
            .HasForeignKey(e => e.MezuniyyetId)
            .OnDelete(DeleteBehavior.Restrict);

        // OnModelCreating-ə:
        builder.Entity<Tapshiriq>()
            .HasOne(t => t.YaradanIsci)
            .WithMany()
            .HasForeignKey(t => t.YaradanIsciId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Tapshiriq>()
            .HasOne(t => t.TeyinOlunanIsci)
            .WithMany()
            .HasForeignKey(t => t.TeyinOlunanIsciId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TapshiriqSherh>()
            .HasOne(s => s.MuellifIsci)
            .WithMany()
            .HasForeignKey(s => s.MuellifIsciId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TapshiriqSherh>()
            .HasOne(s => s.Tapshiriq)
            .WithMany(t => t.Sherhler)
            .HasForeignKey(s => s.TapshiriqId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Gorush>()
            .HasOne(g => g.TeshkilatciIsci)
            .WithMany()
            .HasForeignKey(g => g.TeshkilatciIsciId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<GorushIshtirakci>()
            .HasOne(gi => gi.Isci)
            .WithMany()
            .HasForeignKey(gi => gi.IsciId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<GorushIshtirakci>()
            .HasOne(gi => gi.Gorush)
            .WithMany(g => g.Ishtirakcılar)
            .HasForeignKey(gi => gi.GorushId)
            .OnDelete(DeleteBehavior.Cascade);

        // -------------------------
        // SenedAccess
        // -------------------------
        builder.Entity<SenedAccess>()
            .HasOne(x => x.Sened)
            .WithMany()
            .HasForeignKey(x => x.SenedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Vezife>()
            .HasIndex(x => new { x.Ad, x.DepartamentId })
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


        builder.Entity<IsciMaliye>()
    .HasOne(x => x.Isci)
    .WithOne()
    .HasForeignKey<IsciMaliye>(x => x.IsciId)
    .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IsciMaliye>()
            .HasIndex(x => x.IsciId)
            .IsUnique();

        builder.Entity<IsciMaliye>()
            .Property(x => x.CariMaas)
            .HasPrecision(18, 2);

        builder.Entity<IsciMaliye>()
            .Property(x => x.BankHesabNo)
            .HasMaxLength(34);

        builder.Entity<IsciMaliye>()
            .Property(x => x.SosialSigortaNo)
            .HasMaxLength(50);


        // ==========================
        // MaasNovu
        // ==========================
        builder.Entity<MaasNovu>()
            .HasIndex(x => x.Ad)
            .IsUnique();

        builder.Entity<MaasNovu>()
            .Property(x => x.Ad)
            .HasMaxLength(150);


        // ==========================
        // MaasDetay
        // ==========================
        builder.Entity<MaasDetay>()
            .HasOne(x => x.Maas)
            .WithMany(x => x.Detallar)
            .HasForeignKey(x => x.MaasId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MaasDetay>()
            .HasOne(x => x.MaasNovu)
            .WithMany(x => x.MaasDetallari)
            .HasForeignKey(x => x.MaasNovuId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MaasDetay>()
            .Property(x => x.Mebleg)
            .HasPrecision(18, 2);

        builder.Entity<MaasDetay>()
            .Property(x => x.Aciqlama)
            .HasMaxLength(500);


        // ==========================
        // IsciMaasTarixcesi
        // ==========================
        builder.Entity<IsciMaasTarixcesi>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IsciMaasTarixcesi>()
            .Property(x => x.KohneMaas)
            .HasPrecision(18, 2);

        builder.Entity<IsciMaasTarixcesi>()
            .Property(x => x.YeniMaas)
            .HasPrecision(18, 2);

        builder.Entity<IsciMaasTarixcesi>()
            .Property(x => x.EmrinNomresi)
            .HasMaxLength(100);

        builder.Entity<IsciMaasTarixcesi>()
            .Property(x => x.Sebeb)
            .HasMaxLength(300);


        // ==========================
        // MaasParametri
        // ==========================
        builder.Entity<MaasParametri>()
            .Property(x => x.Deyer)
            .HasPrecision(18, 4);

        builder.Entity<MaasParametri>()
            .Property(x => x.Aciqlama)
            .HasMaxLength(300);

        // Eyni parametr növü üçün eyni başlanma tarixində təkrar olmasın
        builder.Entity<MaasParametri>()
            .HasIndex(x => new { x.Nov, x.BaslamaTarixi })
            .IsUnique();

        builder.Entity<Davamiyyet>()
            .HasIndex(x => new { x.IsciId, x.Tarix })
            .IsUnique();
        builder.Entity<Mezuniyyet>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Cascade);

        // AppDbContext.cs-də OnModelCreating-ə əlavə:
        builder.Entity<Mezuniyyet>()
            .HasOne(m => m.SobeReisiIsci)
            .WithMany()
            .HasForeignKey(m => m.SobeReisiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Mezuniyyet>()
            .HasOne(m => m.RehberIsci)
            .WithMany()
            .HasForeignKey(m => m.RehberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Mezuniyyet>()
            .HasOne(m => m.HrIsci)
            .WithMany()
            .HasForeignKey(m => m.HrId)
            .OnDelete(DeleteBehavior.Restrict);

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

        // Məzuniyyət balansı üçün unikal indeks (Bir işçinin bir ildə hər növ üçün bir balansı ola bilər)
        builder.Entity<MezuniyyetBalans>()
            .HasIndex(x => new { x.IsciId, x.Il, x.Nov })
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
     .WithMany(d => d.IsciTeyinatlar)  // ← əlavə et
     .HasForeignKey(x => x.DepartamentId)
     .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<IsciTeyinat>()
            .HasOne(x => x.Vezife)
            .WithMany(v => v.IsciTeyinatlar)  // ← əlavə et
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
        // ── LoginLog ──────────────────────────────────────────
        builder.Entity<LoginLog>(entity =>
        {
            entity.ToTable("LoginLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserName).HasMaxLength(256);
            entity.Property(x => x.FullName).HasMaxLength(200);
            entity.Property(x => x.IpAddress).HasMaxLength(50);
            entity.Property(x => x.UserAgent).HasMaxLength(500);
            entity.Property(x => x.FailReason).HasMaxLength(300);
            entity.HasIndex(x => x.LoginTime);
        });

        builder.Entity<MusteriHesabi>()
     .HasIndex(x => new { x.MusteriId, x.Iban })
     .IsUnique();

        // ── Hesabat İzləmə ──────────────────────────────────
        builder.Entity<HesabatSablonu>()
            .HasOne(x => x.Kateqoriya)
            .WithMany(k => k.Sablonlar)
            .HasForeignKey(x => x.KateqoriyaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HesabatSablonu>()
            .HasOne(x => x.MesulIsci)
            .WithMany()
            .HasForeignKey(x => x.MesulIsciId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HesabatSablonu>()
            .HasOne(x => x.Departament)
            .WithMany()
            .HasForeignKey(x => x.DepartamentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HesabatTapshiriq>()
            .HasOne(x => x.Sablon)
            .WithMany(s => s.Tapshiriqlar)
            .HasForeignKey(x => x.SablonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<HesabatTapshiriq>()
            .HasOne(x => x.IcraEdenIsci)
            .WithMany()
            .HasForeignKey(x => x.IcraEdenIsciId)
            .OnDelete(DeleteBehavior.Restrict);

        // -------------------------
        // SenedSablon
        // -------------------------
        builder.Entity<SenedSablon>()
            .HasOne(x => x.SenedNovu)
            .WithMany()
            .HasForeignKey(x => x.SenedNovuId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Performans ─────────────────────────────────────────
        builder.Entity<PerformansQiymetlendirme>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<PerformansQiymetlendirme>()
            .HasOne(x => x.QiymetlendirenIsci)
            .WithMany()
            .HasForeignKey(x => x.QiymetlendirenIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<PerformansQiymetlendirme>()
            .Property(x => x.IsciOrtalamaQiymet).HasPrecision(5, 2);
        builder.Entity<PerformansQiymetlendirme>()
            .Property(x => x.MudirOrtalamaQiymet).HasPrecision(5, 2);
        builder.Entity<PerformansQiymetlendirme>()
            .Property(x => x.YekunQiymet).HasPrecision(5, 2);

        builder.Entity<PerformansKriteriya>()
            .HasOne(x => x.Performans)
            .WithMany(p => p.Kriteriyalar)
            .HasForeignKey(x => x.PerformansId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PerformansKriteriya>()
            .Property(x => x.Ceki).HasPrecision(5, 2);
        builder.Entity<PerformansKriteriya>()
            .Property(x => x.IsciQiymeti).HasPrecision(5, 2);
        builder.Entity<PerformansKriteriya>()
            .Property(x => x.MudirQiymeti).HasPrecision(5, 2);

        // ── Təlim ─────────────────────────────────────────────
        builder.Entity<TelimIshtiraki>()
            .HasOne(x => x.Telim)
            .WithMany(t => t.Ishtirakcilar)
            .HasForeignKey(x => x.TelimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TelimIshtiraki>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TelimIshtiraki>()
            .Property(x => x.Qiymet).HasPrecision(5, 2);

        builder.Entity<Telim>()
            .Property(x => x.Xerc).HasPrecision(18, 2);

        builder.Entity<Sertifikat>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Xərc ──────────────────────────────────────────────
        builder.Entity<Xerc>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Xerc>()
            .HasOne(x => x.Kateqoriya)
            .WithMany(k => k.Xercler)
            .HasForeignKey(x => x.KateqoriyaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Xerc>()
            .HasOne(x => x.TesdiqleyenIsci)
            .WithMany()
            .HasForeignKey(x => x.TesdiqleyenIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Xerc>()
            .Property(x => x.Mebleg).HasPrecision(18, 2);

        builder.Entity<XercKateqoriyasi>()
            .HasIndex(x => x.Ad).IsUnique();

        // ── Büdcə ─────────────────────────────────────────────
        builder.Entity<Budce>()
            .HasOne(x => x.Departament)
            .WithMany()
            .HasForeignKey(x => x.DepartamentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Budce>()
            .HasIndex(x => new { x.DepartamentId, x.Il, x.Ay })
            .IsUnique();

        builder.Entity<Budce>()
            .Property(x => x.PlanMebleg).HasPrecision(18, 2);
        builder.Entity<Budce>()
            .Property(x => x.FaktikiMebleg).HasPrecision(18, 2);

        // ── Elan ──────────────────────────────────────────────
        builder.Entity<Elan>()
            .HasOne(x => x.GonderenIsci)
            .WithMany()
            .HasForeignKey(x => x.GonderenIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Chat ──────────────────────────────────────────────
        builder.Entity<ChatMesaj>()
            .HasOne(x => x.GonderenIsci)
            .WithMany()
            .HasForeignKey(x => x.GonderenIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ChatMesaj>()
            .HasOne(x => x.AlanIsci)
            .WithMany()
            .HasForeignKey(x => x.AlanIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── XercKateqoriyasi Seed Data ────────────────────────
        builder.Entity<XercKateqoriyasi>().HasData(
            new XercKateqoriyasi { Id = 1, Ad = "Taksi", Ikon = "bi-taxi-front", Aktivdir = true },
            new XercKateqoriyasi { Id = 2, Ad = "Yemək", Ikon = "bi-cup-hot", Aktivdir = true },
            new XercKateqoriyasi { Id = 3, Ad = "Ofis ləvazimatı", Ikon = "bi-printer", Aktivdir = true },
            new XercKateqoriyasi { Id = 4, Ad = "Səfər xərcləri", Ikon = "bi-airplane", Aktivdir = true },
            new XercKateqoriyasi { Id = 5, Ad = "Digər", Ikon = "bi-three-dots", Aktivdir = true }
        );

        // ── MaasNovu Seed Data ────────────────────────────────────
        builder.Entity<MaasNovu>().HasData(
            new MaasNovu { Id = 1, Ad = "Əsas Əməkhaqqı", Tip = MaasDetayTipi.Gelir, Aktivdir = true },
            new MaasNovu { Id = 2, Ad = "Bonus/Mükafat", Tip = MaasDetayTipi.Gelir, Aktivdir = true },
            new MaasNovu { Id = 3, Ad = "Məzuniyyət Ödənişi", Tip = MaasDetayTipi.Gelir, Aktivdir = true },
            new MaasNovu { Id = 4, Ad = "Davamiyyət Kəsintisi", Tip = MaasDetayTipi.Tutulma, Aktivdir = true },
            new MaasNovu { Id = 5, Ad = "Gecikdirmə Cəriməsi", Tip = MaasDetayTipi.Tutulma, Aktivdir = true },
            new MaasNovu { Id = 6, Ad = "Gəlir Vergisi", Tip = MaasDetayTipi.Tutulma, Aktivdir = true },
            new MaasNovu { Id = 7, Ad = "DSMF (İşçi)", Tip = MaasDetayTipi.Tutulma, Aktivdir = true },
            new MaasNovu { Id = 8, Ad = "İşsizlik Sığortası (İşçi)", Tip = MaasDetayTipi.Tutulma, Aktivdir = true },
            new MaasNovu { Id = 9, Ad = "İTSS", Tip = MaasDetayTipi.Tutulma, Aktivdir = true },
            new MaasNovu { Id = 10, Ad = "DSMF (İşəgötürən)", Tip = MaasDetayTipi.IsegoturenXerci, Aktivdir = true },
            new MaasNovu { Id = 11, Ad = "İşsizlik Sığortası (İşəgötürən)", Tip = MaasDetayTipi.IsegoturenXerci, Aktivdir = true }
        );

        // ── MaasParametri Seed Data ───────────────────────────────
        builder.Entity<MaasParametri>().HasData(
            new MaasParametri { Id = 1, Nov = MaasParametrNovu.GelirVergisiFaizi, Tip = MaasParametrTipi.Faiz, Deyer = 14m, Aciqlama = "2026", BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new MaasParametri { Id = 2, Nov = MaasParametrNovu.DsmfFaizi, Tip = MaasParametrTipi.Faiz, Deyer = 3m, Aciqlama = "2026", BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new MaasParametri { Id = 3, Nov = MaasParametrNovu.IssizlikSigortasiFaizi, Tip = MaasParametrTipi.Faiz, Deyer = 0.5m, Aciqlama = "2026", BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new MaasParametri { Id = 4, Nov = MaasParametrNovu.IcbariTibbiSigortaFaizi, Tip = MaasParametrTipi.Faiz, Deyer = 2m, Aciqlama = "2026", BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new MaasParametri { Id = 5, Nov = MaasParametrNovu.MinimumEmekHaqqi, Tip = MaasParametrTipi.Mebleg, Deyer = 345m, Aciqlama = "2026", BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new MaasParametri { Id = 6, Nov = MaasParametrNovu.VergiGuzestiMeblegi, Tip = MaasParametrTipi.Mebleg, Deyer = 200m, Aciqlama = "2026", BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true }
        );

    }

    // ƏN VACİB HİSSƏ: SaveChanges zamanı avtomatik Audit məlumatlarının doldurulması
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var userId = GetCurrentUserId();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.YaradilmaTarixi = DateTime.Now;
                    entry.Entity.Silinib = false;
                    entry.Entity.YaradanIcraciId = userId;
                    break;

                case EntityState.Modified:
                    if (entry.Entity.Silinib && entry.Entity.SilinmeTarixi == null)
                    {
                        // Soft delete
                        entry.Entity.SilinmeTarixi = DateTime.Now;
                        entry.Entity.SilenIcraciId = userId;
                    }
                    else if (!entry.Entity.Silinib)
                    {
                        entry.Entity.YenilenmeTarixi = DateTime.Now;
                        entry.Entity.YenileyenIcraciId = userId;
                    }
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private int? GetCurrentUserId()
    {
        var userIdStr = _httpContextAccessor?.HttpContext?.User
            ?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdStr, out int id) ? id : null;
    }
}