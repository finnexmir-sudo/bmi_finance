using FinNex.Domain;
using FinNex.Domain.Entities;
using FinNex.Domain.Entities.AI;
using FinNex.Domain.Entities.Avtopark;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Mektub;
using FinNex.Domain.Entities.Hevale;
using FinNex.Domain.Entities.Emeliyyat;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Entities.Sorgular;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Entities.Yardim;
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
    public DbSet<FealiyyetJurnali> FealiyyetJurnali { get; set; }
    public DbSet<SenedSablon> SenedSablonlar { get; set; }

    public DbSet<UserDepartment> UserDepartments { get; set; }

    // =====================
    // HR Module
    // =====================

    public DbSet<Isci> Isciler { get; set; }
    public DbSet<DaxilMektub> DaxilMektublar { get; set; }
    public DbSet<XaricMektub> XaricMektublar { get; set; }
    public DbSet<GedenHevale> GedenHevaleler { get; set; }
    public DbSet<GelenHevale> GelenHevaleler { get; set; }
    public DbSet<Kocurme> Kocurmeler { get; set; }

    // Səhifə təlimatları — «?» düyməsinin mətni (27.08.2026)
    public DbSet<SehifeYardimi> SehifeYardimlari { get; set; }
    public DbSet<TelebeKocurme> TelebeKocurmeler { get; set; }
    public DbSet<Vezife> Vezifeler { get; set; }
    public DbSet<Maas> Maaslar { get; set; }
    public DbSet<DsmfTarixce> DsmfTarixceler { get; set; }
    public DbSet<Davamiyyet> Davamiyyetler { get; set; }
    public DbSet<CihazOxuma> CihazOxumalar { get; set; }
    public DbSet<IsGunuBitdiElan> IsGunuBitdiElanlar { get; set; }
    public DbSet<ErkenCixisIcaze> ErkenCixisIcazeler { get; set; }
    // Məzuniyyət modulu üçün yeni DbSet-lər
    public DbSet<Mezuniyyet> Mezuniyyetler { get; set; }
    public DbSet<MezuniyyetBalans> MezuniyyetBalanslari { get; set; }
    public DbSet<MezuniyyetKompensasiyasi> MezuniyyetKompensasiyalari { get; set; }
    public DbSet<BayramGunu> BayramGunleri { get; set; }
    public DbSet<Icaze> Icazeler { get; set; }
    public DbSet<IcazeCixisGiris> IcazeCixisGirisler { get; set; }
    public DbSet<EzamiyyetMekan> EzamiyyetMekanlar { get; set; }
    public DbSet<EzamiyyetMuraciet> EzamiyyetMuracietler { get; set; }

    // VM 98.2.1 — işçi kreditləri üzrə hesabi gəlir hesablamasında istifadə olunan
    // bazar faiz dərəcəsi (mühasib əl ilə idarə edir, dəyişəndə yeni sətir)
    public DbSet<KreditFaizDerecesi> KreditFaizDereceleri { get; set; }

    // Avtopark — xidməti maşınlar, açar jurnalı, müddət izləmə
    public DbSet<Masin> Masinlar { get; set; }
    public DbSet<MasinMuraciet> MasinMuracietler { get; set; }
    public DbSet<MasinMuddetNovu> MasinMuddetNovleri { get; set; }
    public DbSet<MasinMuddet> MasinMuddetler { get; set; }
    public DbSet<AvtoparkXeberdarliqAlicisi> AvtoparkXeberdarliqAlicilari { get; set; }

    // Mərkəzi əmr reyestri + nömrə sayğacı (məzuniyyət, maaş dəyişikliyi, ...)
    public DbSet<Emr> Emrler { get; set; }
    public DbSet<EmrSayghaci> EmrSayghaclari { get; set; }

    // Mühasibat (proводka) hesabları — açar→hesab (avans, maaş və s.)
    public DbSet<MuhasibatHesabi> MuhasibatHesablari { get; set; }

    public DbSet<IsciTeyinat> IsciTeyinatlari { get; set; }
    public DbSet<MuqavileYenileme> MuqavileYenilemeleri { get; set; }
    public DbSet<IsciStrukturRolu> IsciStrukturRollari { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

    public DbSet<IsciMaliye> IsciMaliyeleri { get; set; }
    public DbSet<MaasDetay> MaasDetaylari { get; set; }
    public DbSet<MaasNovu> MaasNovleri { get; set; }
    public DbSet<AyliqElaveQeydi> AyliqElaveQeydleri { get; set; }
    public DbSet<IsciDigerTutulma> IsciDigerTutulmalari { get; set; }
    public DbSet<IsciMaasTarixcesi> IsciMaasTarixceleri { get; set; }
    public DbSet<MaasParametri> MaasParametrleri { get; set; }
    public DbSet<VergiPille> VergiPilleleri { get; set; }
    public DbSet<IsciAyliqQazanc> IsciAyliqQazanclar { get; set; }
    public DbSet<Xestelik> Xestelikler { get; set; }
    public DbSet<XestelikOdenis> XestelikOdenisleri { get; set; }
    public DbSet<Guzest> Guzestler { get; set; }
    public DbSet<IsciGuzest> IsciGuzestler { get; set; }
    public DbSet<IsciUsaq> IsciUsaqlari { get; set; }
    public DbSet<IsciHYS> IsciHYSler { get; set; }
    public DbSet<Avans> Avanslar { get; set; }
    public DbSet<Teklif> Teklifler { get; set; }

    // Gələn Mail Modulu
    public DbSet<GelenMail> GelenMailler { get; set; }
    public DbSet<GelenMailQosma> GelenMailQosmalar { get; set; }
    public DbSet<GelenMailIsci> GelenMailIsciler { get; set; }

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
    public DbSet<PerformansKriteriyaSablon> PerformansKriteriyaSablonlar { get; set; }

    // Təlim
    public DbSet<Telim> Telimler { get; set; }
    public DbSet<TelimIshtiraki> TelimIshtiraklar { get; set; }
    public DbSet<Sertifikat> Sertifikatlar { get; set; }

    // Xərc
    public DbSet<XercKateqoriyasi> XercKateqoriyalari { get; set; }
    public DbSet<Xerc> Xercler { get; set; }

    // Büdcə
    public DbSet<Budce> Budceler { get; set; }
    public DbSet<SirketBudcesi> SirketBudceleri { get; set; }
    public DbSet<DovriOdenis> DovriOdenisler { get; set; }
    public DbSet<GozlenilenXerc> GozlenilenXercler { get; set; }
    public DbSet<SistemAyar> SistemAyarlari { get; set; }
    public DbSet<IsParametri> IsParametrler { get; set; }

    // Elan
    public DbSet<Elan> Elanlar { get; set; }

    // =====================
    // Motivasiya / Jeton Modulu
    // =====================
    public DbSet<JetonTeyinati> JetonTeyinatlari { get; set; }
    public DbSet<IsciJetonu> IsciJetonlari { get; set; }
    public DbSet<JetonRedimTelebi> JetonRedimTelebieri { get; set; }

    public DbSet<ReytingKateqoriyasi> ReytingKateqoriyalari { get; set; }
    public DbSet<ReytingParametri> ReytingParametrleri { get; set; }
    public DbSet<IsciReytinqi> IsciReytinqleri { get; set; }
    public DbSet<ManualReytingQeydi> ManualReytingQeydleri { get; set; }
    public DbSet<JetonTeklifi> JetonTeklifleri { get; set; }

    // Chat
    public DbSet<ChatMesaj> ChatMesajlar { get; set; }

    // Kredit
    public DbSet<FinNex.Domain.Entities.Kredit.KreditMuraciet> KreditMuracietler { get; set; }
    public DbSet<FinNex.Domain.Entities.Kredit.KreditBaxanIsci> KreditBaxanIsciler { get; set; }
    public DbSet<FinNex.Domain.Entities.Kredit.KomiteUzvu> KomiteUzvleri { get; set; }
    public DbSet<FinNex.Domain.Entities.Kredit.KreditQerar> KreditQerarlar { get; set; }
    public DbSet<FinNex.Domain.Entities.Kredit.KreditQerarImza> KreditQerarImzalar { get; set; }
    public DbSet<FinNex.Domain.Entities.Kredit.KreditZamin> KreditZaminler { get; set; }
    public DbSet<FinNex.Domain.Entities.Kredit.KreditRandevu> KreditRandevular { get; set; }
    public DbSet<FinNex.Domain.Entities.Kredit.KreditSmsLog> KreditSmsLoglar { get; set; }
    // Müqavilə nömrə sayğacları — BMI odb.muqavile_nomreleri qarşılığı (normal formada)
    public DbSet<FinNex.Domain.Entities.Kredit.MuqavileSayghaci> MuqavileSayghaclari { get; set; }

    // PİD (Problemli İşlər Departamenti)
    public DbSet<FinNex.Domain.Entities.Pid.PidSmsSablon> PidSmsSablonlar { get; set; }
    public DbSet<FinNex.Domain.Entities.Pid.PidSmsLog> PidSmsLoglar { get; set; }
    public DbSet<FinNex.Domain.Entities.Pid.MehkemeIsi> MehkemeIsleri { get; set; }
    public DbSet<FinNex.Domain.Entities.Pid.MehkemeMerhelesi> MehkemeMerheleri { get; set; }
    public DbSet<FinNex.Domain.Entities.Pid.OdenisNezareti> OdenisNezaretleri { get; set; }
    public DbSet<FinNex.Domain.Entities.Pid.MehkemeZamin> MehkemeZaminler { get; set; }
    public DbSet<FinNex.Domain.Entities.Pid.MehkemeXerci> MehkemeXercler { get; set; }
    public DbSet<FinNex.Domain.Entities.Pid.MehkemeSened> MehkemeSenedler { get; set; }
    public DbSet<FinNex.Domain.Entities.Pid.MehkemeCedvel> MehkemeCedveller { get; set; }
    public DbSet<FinNex.Domain.Entities.Pid.MehkemeCedvelIclas> MehkemeCedvelIclaslar { get; set; }

    public DbSet<OracleSorgu> OracleSorgular { get; set; }

    // =====================
    // AI Module
    // =====================
    public DbSet<SenedAnaliz> SenedAnalizler { get; set; }
    public DbSet<SenedKonstruktor> SenedKonstruktorlar { get; set; }
    public DbSet<HRSohbet> HRSohbetler { get; set; }
    public DbSet<HRSohbetMesaj> HRSohbetMesajlar { get; set; }
    public DbSet<HRQanunFayl> HRQanunFayllar { get; set; }
    public DbSet<HRDaxiliQayda> HRDaxiliQaydalar { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // İcraçı nömrəsi — bir işçidə bir unikal nömrə (NULL-lar istisnadır, çoxu ola bilər)
        builder.Entity<Isci>()
            .HasIndex(x => x.IcraciNo)
            .IsUnique()
            .HasFilter("[IcraciNo] IS NOT NULL");

        // Daxil olan məktub — BMI odb.daxil_mektub sütun adları ilə (Oracle datası yüklənə bilsin)
        builder.Entity<DaxilMektub>(e =>
        {
            e.ToTable("DaxilMektub");
            e.Property(x => x.Nom).HasColumnName("NOM");
            e.Property(x => x.Nom1).HasColumnName("NOM1");
            e.Property(x => x.DaxTarix).HasColumnName("DAX_TARIX");
            e.Property(x => x.IdareAdi).HasColumnName("IDARE_ADI").HasMaxLength(255);
            e.Property(x => x.GonTarix).HasColumnName("GON_TARIX");
            e.Property(x => x.DaxNom).HasColumnName("DAX_NOM").HasMaxLength(45);
            e.Property(x => x.MekUnvan).HasColumnName("MEK_UNVAN");
            e.Property(x => x.Il).HasColumnName("IL");
            e.Property(x => x.Mezmun).HasColumnName("MEZMUN");
            e.HasIndex(x => new { x.Il, x.Nom1 });
        });

        // Xaric olan məktub — BMI odb.xaric_mektub sütun adları ilə (Oracle datası yüklənə bilsin)
        builder.Entity<XaricMektub>(e =>
        {
            e.ToTable("XaricMektub");
            e.Property(x => x.Kod).HasColumnName("KOD");
            e.Property(x => x.QeyNom).HasColumnName("QEY_NOM").HasMaxLength(15);
            e.Property(x => x.GonYer).HasColumnName("GON_YER").HasMaxLength(255);
            e.Property(x => x.Tarix).HasColumnName("TARIX");
            e.Property(x => x.QisaMez).HasColumnName("QISA_MEZ").HasMaxLength(255);
            e.Property(x => x.Icraci).HasColumnName("ICRACI").HasMaxLength(50);
            e.Property(x => x.Dubl).HasColumnName("DUBL").HasMaxLength(10);
            e.Property(x => x.MektubMetn).HasColumnName("MEKTUB_METN");
            e.Property(x => x.Il).HasColumnName("IL");
            // FaylYolu — FinNex sütunu (Oracle-da yoxdur), default adla
            e.HasIndex(x => new { x.Il, x.Kod });
        });

        // Gedən həvalə — BMI odb.geden_hevale sütun adları ilə (Oracle datası yüklənə bilsin)
        // VM 98.2.1 — bazar faiz dərəcəsi tarixçəsi.
        // (Tarix, ValyutaKodu) üzrə indeks: axtarış həmişə «bu tarixdən əvvəlki
        // ən son sətir, həmin valyuta üçün» şəklindədir.
        builder.Entity<KreditFaizDerecesi>(e =>
        {
            e.Property(x => x.ValyutaKodu).HasMaxLength(2).IsRequired();
            e.Property(x => x.Derece).HasPrecision(9, 4);
            e.Property(x => x.Qeyd).HasMaxLength(300);
            e.HasIndex(x => new { x.ValyutaKodu, x.Tarix });
        });

        // ── Avtopark ────────────────────────────────────────────────────────
        // DİQQƏT — CASCADE YOLLARI: `MasinMuraciet`-də İşçiyə DÖRD ayrı FK var
        // (müraciətçi, rəhbər, çıxışı qeyd edən, qayıdışı qeyd edən). Defaultda
        // hamısı Cascade olardı və SQL Server «multiple cascade paths» ilə
        // migration-u RƏDD EDƏRDİ. Ona görə hamısı `Restrict`-dir — üstəlik
        // layihədə silinmə onsuz da YUMŞAQdır (`Silinib`), fiziki silinmə yoxdur.
        builder.Entity<Masin>(e =>
        {
            e.Property(x => x.DovletNomresi).HasMaxLength(20).IsRequired();
            e.Property(x => x.Marka).HasMaxLength(50);
            e.Property(x => x.Model).HasMaxLength(50);
            e.Property(x => x.Reng).HasMaxLength(30);
            e.Property(x => x.Ban).HasMaxLength(50);
            e.Property(x => x.Vin).HasMaxLength(50);
            e.Property(x => x.Novu).HasMaxLength(50);
            e.Property(x => x.Qeyd).HasMaxLength(500);

            // Unikal DEYİL (filtered index yerinə servisdə yoxlanılır): yumşaq
            // silinmiş maşının nömrəsi yenidən istifadə oluna bilməlidir.
            e.HasIndex(x => x.DovletNomresi);

            e.HasOne(x => x.Departament).WithMany()
                .HasForeignKey(x => x.DepartamentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TehkimSurucu).WithMany()
                .HasForeignKey(x => x.TehkimSurucuId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MasinMuraciet>(e =>
        {
            e.Property(x => x.Meqsed).HasMaxLength(300).IsRequired();
            e.Property(x => x.Marsrut).HasMaxLength(300);
            e.Property(x => x.ImtinaSebebi).HasMaxLength(500);

            // Üst-üstə düşmə yoxlaması «bu maşın, bu status, bu tarix aralığı»
            // şəklindədir — indeks həmin sıra ilə.
            e.HasIndex(x => new { x.MasinId, x.Status, x.PlanBaslama });
            e.HasIndex(x => new { x.IsciId, x.Status });

            e.HasOne(x => x.Masin).WithMany(m => m.Muracietler)
                .HasForeignKey(x => x.MasinId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Isci).WithMany()
                .HasForeignKey(x => x.IsciId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Rehber).WithMany()
                .HasForeignKey(x => x.RehberId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CixisQeydEden).WithMany()
                .HasForeignKey(x => x.CixisQeydEdenId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.QayidisQeydEden).WithMany()
                .HasForeignKey(x => x.QayidisQeydEdenId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MasinMuddetNovu>(e =>
        {
            e.Property(x => x.Ad).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Ad);
        });

        builder.Entity<MasinMuddet>(e =>
        {
            e.Property(x => x.Mebleg).HasPrecision(18, 2);
            e.Property(x => x.SenedFaylYolu).HasMaxLength(300);
            e.Property(x => x.SenedFaylAdi).HasMaxLength(200);
            e.Property(x => x.Qeyd).HasMaxLength(500);

            // Fon xidmətinin sorğusu: «aktiv, xəbərdarlığı getməmiş, son tarixi
            // yaxınlaşan» — indeks həmin üç sütun üzrə.
            e.HasIndex(x => new { x.Aktivdir, x.XeberdarliqGonderilib, x.SonTarix });

            e.HasOne(x => x.Masin).WithMany(m => m.Muddetler)
                .HasForeignKey(x => x.MasinId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Nov).WithMany()
                .HasForeignKey(x => x.NovId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AvtoparkXeberdarliqAlicisi>(e =>
        {
            e.HasIndex(x => x.IsciId);
            e.HasOne(x => x.Isci).WithMany()
                .HasForeignKey(x => x.IsciId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SehifeYardimi>(e =>
        {
            e.ToTable("SehifeYardimlari");
            e.Property(x => x.Acar).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.Property(x => x.Basliq).HasMaxLength(200).IsRequired();
            e.Property(x => x.Modul).HasMaxLength(100);
            e.Property(x => x.Xulase).HasMaxLength(500);
            // Mətn HTML-dir və uzun ola bilər — limit qoyulmur (nvarchar(max)).
            // Limit qoysaq uzun təlimat SQL-də «String or binary data would be
            // truncated» ilə bütün yazını sındırardı (27.08.2026 həvalə hadisəsi).

            // Açar marşrutdan qurulur və HƏR SƏHİFƏ ÜÇÜN BİRDİR — unikal olmalıdır,
            // yoxsa «?» hansı sətri gətirəcəyini bilməz.
            e.HasIndex(x => x.Acar).IsUnique();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<GedenHevale>(e =>
        {
            e.ToTable("GedenHevale");
            e.Property(x => x.HevNom).HasColumnName("HEV_NOM").HasMaxLength(10);
            e.Property(x => x.HesNom).HasColumnName("HES_NOM").HasMaxLength(20);
            e.Property(x => x.Saa).HasColumnName("SAA").HasMaxLength(50);
            e.Property(x => x.TipRes).HasColumnName("TIP_RES").HasMaxLength(16);
            e.Property(x => x.Mebleg).HasColumnName("MEBLEG").HasPrecision(14, 2);
            e.Property(x => x.ValTip).HasColumnName("VAL_TIP").HasMaxLength(10);
            e.Property(x => x.Tarix).HasColumnName("TARIX");
            e.Property(x => x.MenOlke).HasColumnName("MEN_OLKE").HasMaxLength(40);
            // 27.08.2026: 15 → 50. BMI-nin köhnə ölçüsü (15) real müqavilə/bəyannamə
            // nömrələri üçün az idi və uzun dəyər bütün INSERT-i sındırırdı.
            // Bu limitlər GedenHevaleService.UzunluqXetasi və view-lardakı
            // maxlength ilə EYNİ olmalıdır — birini dəyişəndə o birilərini də dəyiş.
            e.Property(x => x.ContracNom).HasColumnName("CONTRAC_NOM").HasMaxLength(50);
            e.Property(x => x.DeclarNom).HasColumnName("DECLAR_NOM").HasMaxLength(50);
            e.Property(x => x.Arayis).HasColumnName("ARAYIS");
            e.Property(x => x.Olke).HasColumnName("OLKE").HasMaxLength(40);
            e.Property(x => x.HevTip).HasColumnName("HEV_TIP").HasMaxLength(254);
            e.Property(x => x.GonTip).HasColumnName("GON_TIP").HasMaxLength(20);
            e.Property(x => x.AlBank).HasColumnName("AL_BANK").HasMaxLength(40);
            e.Property(x => x.Icra).HasColumnName("ICRA");
            e.HasIndex(x => x.Tarix);
            // Pul köçürməsi bağı — FinNex sahəsi (BMI-də yoxdur). Köçürmə redaktə/
            // silinəndə jurnal sətrini tapmaq üçün indeks lazımdır.
            e.HasIndex(x => x.KocurmeId);
        });

        // Gələn həvalə — BMI odb.gelen_hevale sütun adları ilə
        builder.Entity<GelenHevale>(e =>
        {
            e.ToTable("GelenHevale");
            e.Property(x => x.HevNom).HasColumnName("HEV_NOM").HasMaxLength(10);
            e.Property(x => x.HesNom).HasColumnName("HES_NOM").HasMaxLength(20);
            e.Property(x => x.Saa).HasColumnName("SAA").HasMaxLength(50);
            e.Property(x => x.TipRes).HasColumnName("TIP_RES").HasMaxLength(16);
            e.Property(x => x.Mebleg).HasColumnName("MEBLEG").HasPrecision(10, 2);
            e.Property(x => x.ValTip).HasColumnName("VAL_TIP").HasMaxLength(10);
            e.Property(x => x.Tarix).HasColumnName("TARIX");
            e.Property(x => x.MenOlke).HasColumnName("MEN_OLKE").HasMaxLength(40);
            e.Property(x => x.HevTip).HasColumnName("HEV_TIP").HasMaxLength(254);
            e.Property(x => x.Dec).HasColumnName("DEC");
            e.Property(x => x.DecNom).HasColumnName("DEC_NOM").HasMaxLength(30);
            e.Property(x => x.GelOlke).HasColumnName("GEL_OLKE").HasMaxLength(40);
            e.Property(x => x.GonTip).HasColumnName("GON_TIP").HasMaxLength(20);
            e.Property(x => x.AlBank).HasColumnName("AL_BANK").HasMaxLength(250);
            e.Property(x => x.Icra).HasColumnName("ICRA");
            e.HasIndex(x => x.Tarix);
        });

        // Əməliyyat — pul / tələbə köçürməsi (FinNex cədvəli, təmiz sütun adları)
        builder.Entity<Kocurme>(e =>
        {
            e.ToTable("Kocurme");
            e.Property(x => x.Novu).HasMaxLength(16);
            e.Property(x => x.HevaleNo).HasMaxLength(20);
            e.Property(x => x.GonderenAd).HasMaxLength(80);
            e.Property(x => x.GonderenSoyad).HasMaxLength(80);
            e.Property(x => x.GonderenAtaAd).HasMaxLength(80);
            e.Property(x => x.GonderenPassport).HasMaxLength(30);
            e.Property(x => x.GonderenTelefon).HasMaxLength(30);
            e.Property(x => x.AlanAd).HasMaxLength(80);
            e.Property(x => x.AlanSoyad).HasMaxLength(80);
            e.Property(x => x.AlanAtaAd).HasMaxLength(80);
            e.Property(x => x.AlanPassport).HasMaxLength(30);
            e.Property(x => x.AlanTelefon).HasMaxLength(30);
            e.Property(x => x.Mebleg).HasPrecision(18, 2);
            e.Property(x => x.RialCbar).HasPrecision(18, 4);
            e.Property(x => x.ValyutaCbar).HasPrecision(18, 4);
            e.Property(x => x.IranRial).HasPrecision(18, 2);
            e.Property(x => x.MedaxilValyuta).HasMaxLength(10);
            e.Property(x => x.KocurulenValyuta).HasMaxLength(10);
            e.Property(x => x.Secim).HasMaxLength(30);
            e.Property(x => x.BankAd).HasMaxLength(120);
            e.Property(x => x.Filial).HasMaxLength(120);
            e.Property(x => x.AlanHesab).HasMaxLength(40);
            e.Property(x => x.Elave).HasMaxLength(200);
            e.Property(x => x.Meqsed).HasMaxLength(200);
            e.HasIndex(x => new { x.Novu, x.Tarix });
        });

        // Tələbə köçürməsi (təhsil haqqı) — FinNex cədvəli
        builder.Entity<TelebeKocurme>(e =>
        {
            e.ToTable("TelebeKocurme");
            e.Property(x => x.HevaleNo).HasMaxLength(30);
            e.Property(x => x.Adi).HasMaxLength(120);
            e.Property(x => x.Passport).HasMaxLength(30);
            e.Property(x => x.Mebleg).HasPrecision(18, 2);
            e.Property(x => x.BmiFilial).HasMaxLength(80);
            e.Property(x => x.RefNo).HasMaxLength(50);
            e.Property(x => x.UniAd).HasMaxLength(200);
            e.Property(x => x.AlanBank).HasMaxLength(80);
            e.Property(x => x.TelebeKursu).HasMaxLength(50);
            e.Property(x => x.XH).HasPrecision(9, 4);
            e.Property(x => x.Kurs).HasPrecision(12, 4);
            e.Property(x => x.Komissiya).HasPrecision(18, 2);
            e.Property(x => x.Hes35025).HasMaxLength(30);
            e.Property(x => x.Hes45023).HasMaxLength(30);
            e.Property(x => x.Hes45011).HasMaxLength(30);
            e.Property(x => x.Hes67013).HasMaxLength(30);
            e.HasIndex(x => x.Tarix);
        });

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
            .WithMany(x => x.Maaslar)
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
            .WithOne(x => x.Maliye)
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
            .WithMany(x => x.MaasTarixcesi)
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

        // ==========================
        // Xestelik (xəstəlik bülletənləri)
        // ==========================
        builder.Entity<Xestelik>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Xestelik>()
            .Property(x => x.BulletenNomresi)
            .IsRequired()
            .HasMaxLength(50);

        builder.Entity<Xestelik>()
            .Property(x => x.MualiceMuessisesi)
            .HasMaxLength(200);

        builder.Entity<Xestelik>()
            .Property(x => x.Qeyd)
            .HasMaxLength(500);

        builder.Entity<Xestelik>()
            .HasIndex(x => new { x.IsciId, x.BaslamaTarixi });

        // ==========================
        // XestelikOdenis (audit üçün)
        // ==========================
        builder.Entity<XestelikOdenis>()
            .HasOne(x => x.Xestelik)
            .WithMany(x => x.Odenisler)
            .HasForeignKey(x => x.XestelikId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<XestelikOdenis>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<XestelikOdenis>()
            .HasOne(x => x.Maas)
            .WithMany()
            .HasForeignKey(x => x.MaasId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<XestelikOdenis>()
            .Property(x => x.BirGunluk)
            .HasPrecision(18, 4);

        builder.Entity<XestelikOdenis>()
            .Property(x => x.SirketOdenis)
            .HasPrecision(18, 2);

        builder.Entity<XestelikOdenis>()
            .Property(x => x.DsmfOdenis)
            .HasPrecision(18, 2);

        builder.Entity<XestelikOdenis>()
            .HasIndex(x => new { x.IsciId, x.Il, x.Ay });

        // ==========================
        // IsciAyliqQazanc (məzuniyyət üçün 12 ay qazanc tarixçəsi)
        // ==========================
        builder.Entity<IsciAyliqQazanc>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IsciAyliqQazanc>()
            .Property(x => x.Qazanc)
            .HasPrecision(18, 2);

        builder.Entity<IsciAyliqQazanc>()
            .Property(x => x.Qeyd)
            .HasMaxLength(500);

        // Eyni işçi üçün eyni il/ay təkrar olmasın
        builder.Entity<IsciAyliqQazanc>()
            .HasIndex(x => new { x.IsciId, x.Il, x.Ay })
            .IsUnique();

        // ==========================
        // VergiPille (pilləli vergi dərəcələri)
        // ==========================
        builder.Entity<VergiPille>()
            .Property(x => x.AsagiHedd)
            .HasPrecision(18, 2);
        builder.Entity<VergiPille>()
            .Property(x => x.YuxariHedd)
            .HasPrecision(18, 2);
        builder.Entity<VergiPille>()
            .Property(x => x.Faiz)
            .HasPrecision(6, 2);
        builder.Entity<VergiPille>()
            .Property(x => x.SabitMebleg)
            .HasPrecision(18, 2);
        builder.Entity<VergiPille>()
            .Property(x => x.Aciqlama)
            .HasMaxLength(300);
        builder.Entity<VergiPille>()
            .HasIndex(x => new { x.Nov, x.Aktivdir });

        builder.Entity<Davamiyyet>()
            .HasIndex(x => new { x.IsciId, x.Tarix })
            .IsUnique();

        // Əmr sayğacı — hər (Nov, Il) cütü üçün bir sayğac sətri (unikal)
        builder.Entity<EmrSayghaci>()
            .HasIndex(x => new { x.Nov, x.Il })
            .IsUnique();

        // Müqavilə nömrə sayğacı — bir il/növ üçün YALNIZ BİR sətir ola bilər.
        // İki sətir olsa iki müqavilə eyni nömrəni alardı; unikal indeks bunu bazada bağlayır.
        builder.Entity<FinNex.Domain.Entities.Kredit.MuqavileSayghaci>(e =>
        {
            e.ToTable("MuqavileSayghaci");
            e.HasIndex(x => new { x.Novu, x.Il }).IsUnique();
        });
        // Əmr reyestri — növ+il+nömrə üzrə axtarış/sıralama
        builder.Entity<Emr>()
            .HasIndex(x => new { x.Nov, x.Il, x.Nomre });

        // Mühasibat hesabları — açar unikal; avans Debet hesabı default seed
        builder.Entity<MuhasibatHesabi>()
            .HasIndex(x => x.Acar)
            .IsUnique();
        builder.Entity<MuhasibatHesabi>().HasData(new MuhasibatHesabi
        {
            Id = 1,
            Acar = "AvansDebet",
            Ad = "Avans — Debet hesabı (əməliyyat yazılışı)",
            HesabNomresi = "25052000010000300000",
            Aktiv = true,
            YaradilmaTarixi = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            Silinib = false
        });
        // Əmək haqqı əməliyyat yazılışı (provodka) hesabları — şablondakı nömrələrlə.
        // Rezident/qeyri-rezident bölgüsü işçinin bank hesabı prefiksinə görədir (41015 → qeyri-rezident).
        var maasHesabSeedTarix = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        builder.Entity<MuhasibatHesabi>().HasData(
            new MuhasibatHesabi { Id = 2,  Acar = "MaasKliring",                Ad = "Əmək haqqı — klirinq (45050)",                  HesabNomresi = "45050000000000400000", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 3,  Acar = "MaasXercRezident",          Ad = "Əmək haqqı xərci — rezident",                   HesabNomresi = "90020000010000700000", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 4,  Acar = "MaasXercQeyriRezident",     Ad = "Əmək haqqı xərci — qeyri-rezident",             HesabNomresi = "90020000020000700000", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 5,  Acar = "MukafatXercRezident",       Ad = "Mükafat xərci — rezident",                      HesabNomresi = "90021000000000700000", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 6,  Acar = "MukafatXercQeyriRezident",  Ad = "Mükafat xərci — qeyri-rezident",                HesabNomresi = "90021000000000700001", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 7,  Acar = "ElaveXercRezident",         Ad = "Əlavə əmək haqqı xərci — rezident",             HesabNomresi = "90020000040000700000", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 8,  Acar = "ElaveXercQeyriRezident",    Ad = "Əlavə əmək haqqı xərci — qeyri-rezident",       HesabNomresi = "90020000070000700000", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 9,  Acar = "MezuniyyetXercRezident",    Ad = "Məzuniyyət haqqı xərci — rezident",             HesabNomresi = "90020000030000700000", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 10, Acar = "MezuniyyetXercQeyriRezident", Ad = "Məzuniyyət haqqı xərci — qeyri-rezident",     HesabNomresi = "90020000080000700000", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 11, Acar = "MdssEdenXercRezident",      Ad = "MDSS işəgötürən xərci — rezident",              HesabNomresi = "90022000000000700000", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 12, Acar = "MdssEdenXercQeyriRezident", Ad = "MDSS işəgötürən xərci — qeyri-rezident",        HesabNomresi = "90022000000000700002", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 13, Acar = "IssizlikEdenXerc",          Ad = "İşsizlik işəgötürən xərci",                     HesabNomresi = "90022000000000700001", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 14, Acar = "TibbiEdenXerc",             Ad = "İcbari tibbi işəgötürən xərci",                 HesabNomresi = "90022000000000700004", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 15, Acar = "MdssKredit",                Ad = "MDSS öhdəlik (işəgötürən)",                     HesabNomresi = "45110000000000400001", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 16, Acar = "MdssOlunanKredit",          Ad = "MDSS öhdəlik (sığortaolunan)",                  HesabNomresi = "45110000000000400002", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 17, Acar = "IssizlikEdenKredit",        Ad = "İşsizlik öhdəlik (işəgötürən)",                 HesabNomresi = "45110000000000400003", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 18, Acar = "IssizlikOlunanKredit",      Ad = "İşsizlik öhdəlik (sığortaolunan)",              HesabNomresi = "45110000000000400004", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 19, Acar = "TibbiEdenKredit",           Ad = "İcbari tibbi öhdəlik (işəgötürən)",             HesabNomresi = "45110000000000400005", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 20, Acar = "TibbiOlunanKredit",         Ad = "İcbari tibbi öhdəlik (sığortaolunan)",          HesabNomresi = "45110000000000400006", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 21, Acar = "GelirVergisiKredit",        Ad = "Gəlir vergisi öhdəlik",                         HesabNomresi = "45103000000000400000", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 22, Acar = "MezuniyyetQabaqcadanKredit", Ad = "Qabaqcadan ödənilmiş məzuniyyət (öhdəlik)",     HesabNomresi = "25052000020000300000", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false },
            new MuhasibatHesabi { Id = 23, Acar = "MuavinetXerc",              Ad = "Sığortaedən müavinət xərci (xəstəlik)",         HesabNomresi = "90020000050000700000", Aktiv = true, YaradilmaTarixi = maasHesabSeedTarix, Silinib = false }
        );
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

        // QabaqcadanOdenis halında ödənişi təsdiqləyən Mühasib — optional FK,
        // heç bir cascade effect olmadan saxlanılır.
        builder.Entity<Mezuniyyet>()
            .HasOne(x => x.OdeyenMuhasib)
            .WithMany()
            .HasForeignKey(x => x.OdeyenMuhasibId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Mezuniyyet>()
            .Property(x => x.OdenenMebleg)
            .HasColumnType("decimal(18,2)");

        // ── Güzəşt kataloqu və işçi təyinatları ──
        builder.Entity<Guzest>()
            .Property(x => x.Ad)
            .HasMaxLength(200)
            .IsRequired();
        builder.Entity<Guzest>()
            .Property(x => x.Mebleg)
            .HasColumnType("decimal(18,2)");
        builder.Entity<Guzest>()
            .Property(x => x.Madde)
            .HasMaxLength(300);
        builder.Entity<Guzest>()
            .HasIndex(x => x.Aktivdir);

        builder.Entity<IsciGuzest>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<IsciGuzest>()
            .HasOne(x => x.Guzest)
            .WithMany(g => g.IsciGuzestler)
            .HasForeignKey(x => x.GuzestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IsciGuzest>()
            .HasIndex(x => new { x.IsciId, x.GuzestId });
        builder.Entity<IsciGuzest>()
            .Property(x => x.Qeyd)
            .HasMaxLength(500);

        // Avans — MuhasibId FK (NoAction — multi-cascade path)
        builder.Entity<Avans>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Avans>()
            .HasOne(x => x.Muhasib)
            .WithMany()
            .HasForeignKey(x => x.MuhasibId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<Avans>()
            .Property(x => x.Mebleg)
            .HasColumnType("decimal(18,2)");
        builder.Entity<Avans>()
            .Property(x => x.Sebeb)
            .HasMaxLength(500);
        builder.Entity<Avans>()
            .Property(x => x.ImtinaSebebi)
            .HasMaxLength(500);

        builder.Entity<Teklif>()
            .ToTable("Teklifler")
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Teklif>()
            .HasOne(x => x.CavabVeren)
            .WithMany()
            .HasForeignKey(x => x.CavabVerenIsciId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<Teklif>()
            .Property(x => x.Bashliq).HasMaxLength(200);

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
            .HasOne(x => x.SobeReisi)
            .WithMany()
            .HasForeignKey(x => x.SobeReisiId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<PerformansQiymetlendirme>()
            .HasOne(x => x.Rehber2)
            .WithMany()
            .HasForeignKey(x => x.Rehber2Id)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<PerformansQiymetlendirme>()
            .Property(x => x.SobeReisiOrtalamaQiymet).HasPrecision(5, 2);
        builder.Entity<PerformansQiymetlendirme>()
            .Property(x => x.IsciOrtalamaQiymet).HasPrecision(5, 2);
        builder.Entity<PerformansQiymetlendirme>()
            .Property(x => x.MudirOrtalamaQiymet).HasPrecision(5, 2);
        builder.Entity<PerformansQiymetlendirme>()
            .Property(x => x.Rehber2OrtalamaQiymet).HasPrecision(5, 2);
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
        builder.Entity<PerformansKriteriya>()
            .Property(x => x.Rehber2Qiymeti).HasPrecision(5, 2);
        builder.Entity<PerformansKriteriya>()
            .Property(x => x.SobeReisiQiymeti).HasPrecision(5, 2);

        builder.Entity<PerformansKriteriyaSablon>()
            .Property(x => x.Ceki).HasPrecision(5, 2);
        builder.Entity<PerformansKriteriyaSablon>()
            .ToTable("PerformansKriteriyaSablonlar");

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
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Xerc>()
            .HasOne(x => x.Departament)
            .WithMany()
            .HasForeignKey(x => x.DepartamentId)
            .IsRequired(false)
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

        builder.Entity<SirketBudcesi>()
            .HasIndex(x => x.Il).IsUnique();
        builder.Entity<SirketBudcesi>()
            .Property(x => x.Mebleg).HasPrecision(18, 2);

        // ── Dövri Ödənişlər ───────────────────────────────────
        builder.Entity<DovriOdenis>()
            .HasOne(x => x.Kateqoriya)
            .WithMany()
            .HasForeignKey(x => x.KateqoriyaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DovriOdenis>()
            .HasOne(x => x.Departament)
            .WithMany()
            .HasForeignKey(x => x.DepartamentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<DovriOdenis>()
            .Property(x => x.Mebleg).HasPrecision(18, 2);

        // ── Gözlənilən Xərclər ────────────────────────────────
        builder.Entity<GozlenilenXerc>()
            .HasOne(x => x.Kateqoriya)
            .WithMany()
            .HasForeignKey(x => x.KateqoriyaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<GozlenilenXerc>()
            .HasOne(x => x.Departament)
            .WithMany()
            .HasForeignKey(x => x.DepartamentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<GozlenilenXerc>()
            .HasOne(x => x.Xerc)
            .WithMany()
            .HasForeignKey(x => x.XercId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<GozlenilenXerc>()
            .Property(x => x.TahminiMebleg).HasPrecision(18, 2);

        builder.Entity<IsParametri>()
            .ToTable("IsParametrleri");

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

        // ── Kredit Müraciət ───────────────────────────────────
        builder.Entity<FinNex.Domain.Entities.Kredit.KreditMuraciet>()
            .HasOne(x => x.BaxanIsci)
            .WithMany()
            .HasForeignKey(x => x.BaxanIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<FinNex.Domain.Entities.Kredit.KreditMuraciet>()
            .HasOne(x => x.MkrBaxanIsci)
            .WithMany()
            .HasForeignKey(x => x.MkrBaxanIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<FinNex.Domain.Entities.Kredit.KreditMuraciet>()
            .HasOne(x => x.AsanFinanceBaxanIsci)
            .WithMany()
            .HasForeignKey(x => x.AsanFinanceBaxanIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Kredit Baxan İşçilər ──────────────────────────────
        builder.Entity<FinNex.Domain.Entities.Kredit.KreditBaxanIsci>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Komitə Üzvü ───────────────────────────────────────
        builder.Entity<FinNex.Domain.Entities.Kredit.KomiteUzvu>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Kredit Qərar ──────────────────────────────────────
        builder.Entity<FinNex.Domain.Entities.Kredit.KreditQerar>()
            .HasOne(x => x.KreditMuraciet)
            .WithOne(m => m.Qerar!)
            .HasForeignKey<FinNex.Domain.Entities.Kredit.KreditQerar>(x => x.KreditMuracietId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FinNex.Domain.Entities.Kredit.KreditQerar>()
            .HasOne(x => x.DaxilEdenIsci)
            .WithMany()
            .HasForeignKey(x => x.DaxilEdenIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Kredit Qərar İmza ────────────────────────────────
        builder.Entity<FinNex.Domain.Entities.Kredit.KreditQerarImza>()
            .HasOne(x => x.KreditQerar)
            .WithMany(q => q.Imzalar)
            .HasForeignKey(x => x.KreditQerarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FinNex.Domain.Entities.Kredit.KreditQerarImza>()
            .HasOne(x => x.KomiteUzvu)
            .WithMany()
            .HasForeignKey(x => x.KomiteUzvuId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Kredit Zamin ──────────────────────────────────────
        builder.Entity<FinNex.Domain.Entities.Kredit.KreditZamin>()
            .HasOne(x => x.KreditMuraciet)
            .WithMany(m => m.Zaminler)
            .HasForeignKey(x => x.KreditMuracietId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FinNex.Domain.Entities.Kredit.KreditZamin>()
            .HasIndex(x => x.FIN);

        builder.Entity<FinNex.Domain.Entities.Kredit.KreditZamin>()
            .HasOne(x => x.MkrBaxanIsci)
            .WithMany()
            .HasForeignKey(x => x.MkrBaxanIsciId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FinNex.Domain.Entities.Kredit.KreditZamin>()
            .HasOne(x => x.AsanFinanceBaxanIsci)
            .WithMany()
            .HasForeignKey(x => x.AsanFinanceBaxanIsciId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Kredit Randevu ────────────────────────────────────
        builder.Entity<FinNex.Domain.Entities.Kredit.KreditRandevu>()
            .HasOne(x => x.KreditMuraciet)
            .WithOne(m => m.Randevu!)
            .HasForeignKey<FinNex.Domain.Entities.Kredit.KreditRandevu>(x => x.KreditMuracietId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FinNex.Domain.Entities.Kredit.KreditRandevu>()
            .HasOne(x => x.TeyinEdenIsci)
            .WithMany()
            .HasForeignKey(x => x.TeyinEdenIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<FinNex.Domain.Entities.Kredit.KreditRandevu>()
            .HasIndex(x => x.Tarix);

        // ── Kredit SMS Log ────────────────────────────────────
        builder.Entity<FinNex.Domain.Entities.Kredit.KreditSmsLog>()
            .HasOne(x => x.KreditMuraciet)
            .WithMany(m => m.SmsLoglar)
            .HasForeignKey(x => x.KreditMuracietId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<FinNex.Domain.Entities.Kredit.KreditSmsLog>()
            .HasOne(x => x.GonderenIsci)
            .WithMany()
            .HasForeignKey(x => x.GonderenIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── PİD SMS Modulu ────────────────────────────────────
        builder.Entity<FinNex.Domain.Entities.Pid.PidSmsLog>()
            .HasOne(x => x.Sablon)
            .WithMany()
            .HasForeignKey(x => x.SablonId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<FinNex.Domain.Entities.Pid.PidSmsLog>()
            .HasOne(x => x.GonderenIsci)
            .WithMany()
            .HasForeignKey(x => x.GonderenIsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<FinNex.Domain.Entities.Pid.PidSmsLog>()
            .HasIndex(x => x.YaradilmaTarixi);

        // ── Jeton Modulu ──────────────────────────────────────
        builder.Entity<JetonTeyinati>()
            .Property(x => x.SaatDeyeri).HasPrecision(5, 2);

        builder.Entity<IsciJetonu>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<IsciJetonu>()
            .HasOne(x => x.JetonTeyinati)
            .WithMany(j => j.IsciJetonlari)
            .HasForeignKey(x => x.JetonTeyinatiId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<IsciJetonu>()
            .HasOne(x => x.RedimTelebi)
            .WithMany(r => r.XerclenenJetonlar)
            .HasForeignKey(x => x.RedimTelebiId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<IsciJetonu>()
            .HasOne(x => x.Icaze)
            .WithMany()
            .HasForeignKey(x => x.IcazeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<JetonRedimTelebi>()
            .Property(x => x.CemiSaat).HasPrecision(8, 2);

        builder.Entity<JetonRedimTelebi>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── JetonTeyinati Seed Data ───────────────────────────
        builder.Entity<JetonTeyinati>().HasData(
            new JetonTeyinati { Id = 1, Ad = "Bürünc Jeton", Nov = JetonNovu.Musbat, Rengi = JetonRengi.Burunc, SaatDeyeri = 0.5m, Ikon = "bi bi-award-fill", RengKodu = "#cd7f32", Tesvir = "Kiçik mükafat — 30 dəqiqə", Aktivdir = true, YaradilmaTarixi = new DateTime(2026, 1, 1), Silinib = false },
            new JetonTeyinati { Id = 2, Ad = "Gümüş Jeton",  Nov = JetonNovu.Musbat, Rengi = JetonRengi.Gumus,  SaatDeyeri = 1m,   Ikon = "bi bi-award-fill", RengKodu = "#9ca3af", Tesvir = "Orta mükafat — 1 saat",    Aktivdir = true, YaradilmaTarixi = new DateTime(2026, 1, 1), Silinib = false },
            new JetonTeyinati { Id = 3, Ad = "Qızıl Jeton",  Nov = JetonNovu.Musbat, Rengi = JetonRengi.Qizil,  SaatDeyeri = 4m,   Ikon = "bi bi-award-fill", RengKodu = "#f59e0b", Tesvir = "Böyük mükafat — 4 saat",  Aktivdir = true, YaradilmaTarixi = new DateTime(2026, 1, 1), Silinib = false },
            new JetonTeyinati { Id = 4, Ad = "Qara Jeton",   Nov = JetonNovu.Menfi,  Rengi = JetonRengi.Qara,   SaatDeyeri = 0m,   Ikon = "bi bi-slash-circle-fill", RengKodu = "#1f2937", Tesvir = "İntizam cəzası — mükafat dondurulur", Aktivdir = true, YaradilmaTarixi = new DateTime(2026, 1, 1), Silinib = false }
        );

        // ── Reyting Modulu ───────────────────────────────────────
        builder.Entity<IsciReytinqi>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<IsciReytinqi>()
            .HasOne(x => x.Kateqoriya)
            .WithMany(k => k.IsciReytinqleri)
            .HasForeignKey(x => x.KateqoriyaId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ManualReytingQeydi>()
            .HasOne(x => x.IsciReytinqi)
            .WithMany(r => r.ManualQeydler)
            .HasForeignKey(x => x.IsciReytinqiId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ReytingKateqoriyasi>()
            .Property(x => x.PulAmsali).HasPrecision(5, 2);
        builder.Entity<ReytingKateqoriyasi>()
            .Property(x => x.SaatAmsali).HasPrecision(5, 2);

        builder.Entity<IsciReytinqi>()
            .HasIndex(x => new { x.IsciId, x.Il, x.Ay })
            .IsUnique();

        // ── JetonTeklifi ────────────────────────────────────────
        builder.Entity<JetonTeklifi>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.NoAction);

        // Seed sətirləri üçün sabit tarix — entity-nin DateTime.Now default-u hər
        // Add-Migration-da dəyişib UpdateData "drift"i yaradırdı; sabit tarix bunu dayandırır.
        var seedTarix = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        // ── XercKateqoriyasi Seed Data ────────────────────────
        var xercKateqoriyaSeed = new[]
        {
            new XercKateqoriyasi { Id = 1, Ad = "Taksi", Ikon = "bi-taxi-front", Aktivdir = true },
            new XercKateqoriyasi { Id = 2, Ad = "Yemək", Ikon = "bi-cup-hot", Aktivdir = true },
            new XercKateqoriyasi { Id = 3, Ad = "Ofis ləvazimatı", Ikon = "bi-printer", Aktivdir = true },
            new XercKateqoriyasi { Id = 4, Ad = "Səfər xərcləri", Ikon = "bi-airplane", Aktivdir = true },
            new XercKateqoriyasi { Id = 5, Ad = "Digər", Ikon = "bi-three-dots", Aktivdir = true }
        };
        foreach (var s in xercKateqoriyaSeed) s.YaradilmaTarixi = seedTarix;
        builder.Entity<XercKateqoriyasi>().HasData(xercKateqoriyaSeed);

        // ── MaasNovu Seed Data ────────────────────────────────────
        var maasNovuSeed = new[]
        {
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
            new MaasNovu { Id = 11, Ad = "İşsizlik Sığortası (İşəgötürən)", Tip = MaasDetayTipi.IsegoturenXerci, Aktivdir = true },
            new MaasNovu { Id = 12, Ad = "İTSS (İşəgötürən)", Tip = MaasDetayTipi.IsegoturenXerci, Aktivdir = true },
            new MaasNovu { Id = 13, Ad = "Xəstəlik Ödənişi", Tip = MaasDetayTipi.Gelir, Aktivdir = true },
            new MaasNovu { Id = 14, Ad = "Fərqli Gəlir", Tip = MaasDetayTipi.Gelir, Aktivdir = true },
            new MaasNovu { Id = 17, Ad = "IH-07 Əlavə Təminat", Tip = MaasDetayTipi.Gelir, Aktivdir = true },
            new MaasNovu { Id = 18, Ad = "VM 98.2.1 Gəlirləri", Tip = MaasDetayTipi.Gelir, Aktivdir = true }
        };
        foreach (var s in maasNovuSeed) s.YaradilmaTarixi = seedTarix;
        builder.Entity<MaasNovu>().HasData(maasNovuSeed);

        // ── MaasParametri Seed Data ───────────────────────────────
        var maasParametriSeed = new[]
        {
            new MaasParametri { Id = 1, Nov = MaasParametrNovu.GelirVergisiFaizi, Tip = MaasParametrTipi.Faiz, Deyer = 14m, Aciqlama = "2026", BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new MaasParametri { Id = 2, Nov = MaasParametrNovu.DsmfFaizi, Tip = MaasParametrTipi.Faiz, Deyer = 3m, Aciqlama = "2026", BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new MaasParametri { Id = 3, Nov = MaasParametrNovu.IssizlikSigortasiFaizi, Tip = MaasParametrTipi.Faiz, Deyer = 0.5m, Aciqlama = "2026", BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new MaasParametri { Id = 4, Nov = MaasParametrNovu.IcbariTibbiSigortaFaizi, Tip = MaasParametrTipi.Faiz, Deyer = 2m, Aciqlama = "2026", BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new MaasParametri { Id = 5, Nov = MaasParametrNovu.MinimumEmekHaqqi, Tip = MaasParametrTipi.Mebleg, Deyer = 345m, Aciqlama = "2026", BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new MaasParametri { Id = 6, Nov = MaasParametrNovu.VergiGuzestiMeblegi, Tip = MaasParametrTipi.Mebleg, Deyer = 200m, Aciqlama = "2026", BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true }
        };
        foreach (var s in maasParametriSeed) s.YaradilmaTarixi = seedTarix;
        builder.Entity<MaasParametri>().HasData(maasParametriSeed);

        // ── VergiPille Seed Data — 2026 pilləli vergi dərəcələri ──
        // Qeyri-neft/Qeyri-dövlət sektoru üçün rəsmi qaydalar
        var vergiPilleSeed = new[]
        {
            // Gəlir Vergisi: 0-2500 → 3%; 2500-8000 → 75+10%; 8000+ → 625+14%
            new VergiPille { Id = 1, Nov = MaasParametrNovu.GelirVergisiFaizi, Sira = 1, AsagiHedd = 0m,    YuxariHedd = 2500m,  Faiz = 3m,  SabitMebleg = 0m,    Aciqlama = "2026: 0–2500 AZN → 3%",           BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new VergiPille { Id = 2, Nov = MaasParametrNovu.GelirVergisiFaizi, Sira = 2, AsagiHedd = 2500m, YuxariHedd = 8000m,  Faiz = 10m, SabitMebleg = 75m,   Aciqlama = "2026: 2500–8000 AZN → 75+10%",    BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new VergiPille { Id = 3, Nov = MaasParametrNovu.GelirVergisiFaizi, Sira = 3, AsagiHedd = 8000m, YuxariHedd = null,   Faiz = 14m, SabitMebleg = 625m,  Aciqlama = "2026: 8000+ AZN → 625+14%",       BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },

            // DSMF (İşçi): 0-200 → 3%; 200+ → 6+10%
            new VergiPille { Id = 4, Nov = MaasParametrNovu.DsmfFaizi, Sira = 1, AsagiHedd = 0m,   YuxariHedd = 200m, Faiz = 3m,  SabitMebleg = 0m, Aciqlama = "2026: 0–200 AZN → 3%",       BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new VergiPille { Id = 5, Nov = MaasParametrNovu.DsmfFaizi, Sira = 2, AsagiHedd = 200m, YuxariHedd = null, Faiz = 10m, SabitMebleg = 6m, Aciqlama = "2026: 200+ AZN → 6+10%",    BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },

            // DSMF (İşəgötürən): 0-200 → 22%; 200-8000 → 44+15%; 8000+ → 1214+11%
            new VergiPille { Id = 6, Nov = MaasParametrNovu.DsmfIsegoturenFaizi, Sira = 1, AsagiHedd = 0m,    YuxariHedd = 200m,  Faiz = 22m, SabitMebleg = 0m,    Aciqlama = "2026: 0–200 AZN → 22%",           BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new VergiPille { Id = 7, Nov = MaasParametrNovu.DsmfIsegoturenFaizi, Sira = 2, AsagiHedd = 200m,  YuxariHedd = 8000m, Faiz = 15m, SabitMebleg = 44m,   Aciqlama = "2026: 200–8000 AZN → 44+15%",     BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new VergiPille { Id = 8, Nov = MaasParametrNovu.DsmfIsegoturenFaizi, Sira = 3, AsagiHedd = 8000m, YuxariHedd = null,  Faiz = 11m, SabitMebleg = 1214m, Aciqlama = "2026: 8000+ AZN → 1214+11%",     BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },

            // İTSS (İşçi): 0-8000 → 2%; 8000+ → 160+0.5%
            new VergiPille { Id = 9,  Nov = MaasParametrNovu.IcbariTibbiSigortaFaizi, Sira = 1, AsagiHedd = 0m,    YuxariHedd = 2500m, Faiz = 2m,   SabitMebleg = 0m,  Aciqlama = "2026: 0–2500 AZN → 2%",       BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new VergiPille { Id = 10, Nov = MaasParametrNovu.IcbariTibbiSigortaFaizi, Sira = 2, AsagiHedd = 2500m, YuxariHedd = null,  Faiz = 0.5m, SabitMebleg = 50m, Aciqlama = "2026: 2500+ AZN → 50+0.5%",   BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },

            // İTSS (İşəgötürən): 0-2500 → 2%; 2500+ → 50+0.5%  (işçi ilə eynidir)
            new VergiPille { Id = 11, Nov = MaasParametrNovu.IcbariTibbiSigortaIsegoturenFaizi, Sira = 1, AsagiHedd = 0m,    YuxariHedd = 2500m, Faiz = 2m,   SabitMebleg = 0m,  Aciqlama = "2026: 0–2500 AZN → 2%",       BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true },
            new VergiPille { Id = 12, Nov = MaasParametrNovu.IcbariTibbiSigortaIsegoturenFaizi, Sira = 2, AsagiHedd = 2500m, YuxariHedd = null,  Faiz = 0.5m, SabitMebleg = 50m, Aciqlama = "2026: 2500+ AZN → 50+0.5%",   BaslamaTarixi = new DateTime(2026, 1, 1), Aktivdir = true }
        };
        foreach (var s in vergiPilleSeed) s.YaradilmaTarixi = seedTarix;
        builder.Entity<VergiPille>().HasData(vergiPilleSeed);

        // ── Gələn Mail Modulu ─────────────────────────────────
        builder.Entity<GelenMail>()
            .HasIndex(x => x.MessageId)
            .IsUnique();

        builder.Entity<GelenMail>()
            .HasOne(x => x.TapalanIsci)
            .WithMany()
            .HasForeignKey(x => x.TapalanIsciId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<GelenMailQosma>()
            .HasOne(x => x.GelenMail)
            .WithMany(x => x.Qosmalar)
            .HasForeignKey(x => x.GelenMailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<GelenMailIsci>()
            .HasOne(x => x.GelenMail)
            .WithMany(x => x.TapalanIsciler)
            .HasForeignKey(x => x.GelenMailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<GelenMailIsci>()
            .HasOne(x => x.Isci)
            .WithMany()
            .HasForeignKey(x => x.IsciId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── AI Module ──────────────────────────────────────────────
        builder.Entity<SenedAnaliz>(e =>
        {
            e.ToTable("SenedAnalizler");
            e.HasKey(x => x.Id);
            e.Property(x => x.OriginalText).HasColumnType("nvarchar(max)");
            e.Property(x => x.RiskyClausesJson).HasColumnType("nvarchar(max)");
            e.HasOne(x => x.AppUser).WithMany().HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SenedKonstruktor>(e =>
        {
            e.ToTable("SenedKonstruktorlar");
            e.HasKey(x => x.Id);
            e.Property(x => x.ParametrlerJson).HasColumnType("nvarchar(max)");
            e.Property(x => x.GeneratedContent).HasColumnType("nvarchar(max)");
            e.HasOne(x => x.AppUser).WithMany().HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<HRSohbet>(e =>
        {
            e.ToTable("HRSohbetler");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.AppUser).WithMany().HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Mesajlar).WithOne(m => m.Sohbet).HasForeignKey(m => m.SohbetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<HRSohbetMesaj>(e =>
        {
            e.ToTable("HRSohbetMesajlar");
            e.HasKey(x => x.Id);
            e.Property(x => x.Metn).HasColumnType("nvarchar(max)");
            e.Property(x => x.Rol).HasMaxLength(20);
        });

        builder.Entity<HRQanunFayl>(e =>
        {
            e.ToTable("HRQanunFayllar");
            e.HasKey(x => x.Id);
            e.Property(x => x.Ad).HasMaxLength(300);
            e.Property(x => x.Kateqoriya).HasMaxLength(50);
            e.Property(x => x.FaylYolu).HasMaxLength(500);
            e.Property(x => x.MetnContent).HasColumnType("nvarchar(max)");
        });

        builder.Entity<HRDaxiliQayda>(e =>
        {
            e.ToTable("HRDaxiliQaydalar");
            e.HasKey(x => x.Id);
            e.Property(x => x.Kod).HasMaxLength(20);
            e.Property(x => x.Ad).HasMaxLength(300);
            e.Property(x => x.Kateqoriya).HasMaxLength(100);
            e.Property(x => x.Mezmun).HasColumnType("nvarchar(max)");
            e.HasOne(x => x.YazilanKim).WithMany().HasForeignKey(x => x.YazilanKimId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Ezamiyyət modulu ─────────────────────────────────
        builder.Entity<EzamiyyetMekan>(e =>
        {
            e.ToTable("EzamiyyetMekanlar");
            e.HasKey(x => x.Id);
            e.Property(x => x.Ad).HasMaxLength(200).IsRequired();
        });

        builder.Entity<EzamiyyetMuraciet>(e =>
        {
            e.ToTable("EzamiyyetMuracietler");
            e.HasKey(x => x.Id);
            e.Property(x => x.Baslig).HasMaxLength(300).IsRequired();
            e.Property(x => x.SenedYolu).HasMaxLength(500);
            e.Property(x => x.SenedAd).HasMaxLength(300);
            e.Property(x => x.Qeyd).HasMaxLength(1000);
            e.Property(x => x.RehberQeydi).HasMaxLength(500);
            e.HasOne(x => x.Isci).WithMany().HasForeignKey(x => x.IsciId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Mekan).WithMany(m => m.Muracietler).HasForeignKey(x => x.MekanId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Rehber).WithMany().HasForeignKey(x => x.RehberId).OnDelete(DeleteBehavior.NoAction);
        });

    }

    // İzlənəcək entity növləri və onların Azərbaycanca adları
    private static readonly Dictionary<string, string> _izlenecekCedveller = new()
    {
        { "Isci",                 "İşçi" },
        { "IsciMaasTarixcesi",    "Maaş Dəyişikliyi" },
        { "IsciMaliye",           "İşçi Maliyyə" },
        { "Mezuniyyet",           "Məzuniyyət" },
        { "MezuniyyetOdenis",     "Məz. Ödənişi" },
        { "Maas",                 "Maaş Hesablaması" },
        { "Xestelik",             "Xəstəlik" },
        { "IsciGuzest",           "İşçi Güzəşti" },
        { "IsciTeyinat",          "İşçi Təyinat" },
        { "Teklif",               "Teklif/Boşluq/Şikayət" },
    };

    // Audit sahələri — bunlar dəyişiklik siyahısına daxil edilmir
    private static readonly HashSet<string> _auditSaheleri = new()
    {
        "YaradilmaTarixi", "YenilenmeTarixi", "SilinmeTarixi",
        "YaradanIcraciId", "YenileyenIcraciId", "SilenIcraciId",
        "Silinib"
    };

    // ƏN VACİB HİSSƏ: SaveChanges zamanı avtomatik Audit məlumatlarının doldurulması
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>().ToList();
        var userId = GetCurrentUserId();
        var now = DateTime.Now;

        var logs = new List<FealiyyetJurnali>();

        foreach (var entry in entries)
        {
            var typeName = entry.Entity.GetType().Name;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.YaradilmaTarixi = now;
                    entry.Entity.Silinib = false;
                    entry.Entity.YaradanIcraciId = userId;
                    if (_izlenecekCedveller.TryGetValue(typeName, out var addFarsi))
                        logs.Add(new FealiyyetJurnali { UserId = userId, Emeliyyat = "C",
                            CedvelAdi = typeName, CedvelFarsi = addFarsi,
                            RecordId = 0, Tarix = now });
                    break;

                case EntityState.Modified:
                    if (entry.Entity.Silinib && entry.Entity.SilinmeTarixi == null)
                    {
                        entry.Entity.SilinmeTarixi = now;
                        entry.Entity.SilenIcraciId = userId;
                        if (_izlenecekCedveller.TryGetValue(typeName, out var delFarsi))
                            logs.Add(new FealiyyetJurnali { UserId = userId, Emeliyyat = "D",
                                CedvelAdi = typeName, CedvelFarsi = delFarsi,
                                RecordId = entry.Entity.Id, Tarix = now });
                    }
                    else if (!entry.Entity.Silinib)
                    {
                        // Dəyişiklik detallarını audit sahələri xaric olaraq topla
                        var deyisiklikler = entry.Properties
                            .Where(p => p.IsModified && !_auditSaheleri.Contains(p.Metadata.Name))
                            .Select(p =>
                            {
                                var kohneDeger = FormatDeger(p.OriginalValue);
                                var yeniDeger  = FormatDeger(p.CurrentValue);
                                return $"{p.Metadata.Name}: {kohneDeger} → {yeniDeger}";
                            })
                            .ToList();

                        entry.Entity.YenilenmeTarixi = now;
                        entry.Entity.YenileyenIcraciId = userId;

                        if (_izlenecekCedveller.TryGetValue(typeName, out var updFarsi))
                            logs.Add(new FealiyyetJurnali
                            {
                                UserId      = userId,
                                Emeliyyat   = "U",
                                CedvelAdi   = typeName,
                                CedvelFarsi = updFarsi,
                                RecordId    = entry.Entity.Id,
                                Acıqlama    = deyisiklikler.Count > 0
                                                  ? string.Join("\n", deyisiklikler)
                                                  : null,
                                Tarix = now
                            });
                    }
                    break;
            }
        }

        if (logs.Count > 0)
            FealiyyetJurnali.AddRange(logs);

        return base.SaveChangesAsync(cancellationToken);
    }

    private static string FormatDeger(object? deger)
    {
        if (deger == null) return "boş";
        var s = deger.ToString() ?? "boş";
        return s.Length > 120 ? s[..120] + "…" : s;
    }

    private int? GetCurrentUserId()
    {
        var userIdStr = _httpContextAccessor?.HttpContext?.User
            ?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdStr, out int id) ? id : null;
    }
}