/* ============================================================================
   BMI → FinNex köçürmə — Məktub + Həvalə jurnalları (SQL Server)
   ----------------------------------------------------------------------------
   Bu skript köhnə BMI Oracle cədvəllərinin (odb.*) SQL Server qarşılığını
   yaradır. Sütun adları Oracle ilə EYNİ saxlanılıb ki, Oracle → SQL Server
   data köçürməsi (SSMS Import Wizard / linked server / CSV) 1:1 map olsun.

   Tip xəritəsi:
     VARCHAR2(n)   → NVARCHAR(n)      (Unicode — AZ/fars hərfləri itməsin)
     NUMBER(p,s)   → DECIMAL(p,s)     (məbləğ)
     NUMBER(1..2)  → SMALLINT         (bayraq / kiçik kod)
     NUMBER (tam)  → BIGINT / INT     (say / il)
     DATE          → DATETIME2        (Oracle DATE saat da saxlayır)
     CLOB          → NVARCHAR(MAX)
     LONG RAW      → VARBINARY(MAX)   (skan/ikili məzmun)

   Qeydlər:
     • Yalnız Oracle-da NOT NULL olan sütun (təbii açar) PRIMARY KEY edilib.
     • IDENTITY YOXDUR — açar dəyərləri Oracle-dan olduğu kimi yüklənir
       (yeni qeydlərin nömrələnməsi FinNex servis qatında BMI kimi idarə olunacaq).
     • Təkrar açar (məs. tarixi datada eyni HEV_NOM) load-u pozarsa,
       həmin cədvəldə PRIMARY KEY-i qeyri-unikal INDEX ilə əvəz et.
   ============================================================================ */

/* 1) Daxil olan məktub  (odb.daxil_mektub) --------------------------------- */
IF OBJECT_ID('dbo.DaxilMektub', 'U') IS NULL
CREATE TABLE dbo.DaxilMektub (
    NOM        BIGINT          NOT NULL,   -- Qeydiyyat № (açar)
    DAX_TARIX  DATETIME2       NULL,       -- Daxil olma tarixi
    IDARE_ADI  NVARCHAR(255)   NULL,       -- Göndərən idarə/təşkilat adı
    GON_TARIX  DATETIME2       NULL,       -- Göndərilmə tarixi
    MEZMUN     VARBINARY(MAX)  NULL,       -- Məzmun (Oracle LONG RAW → ikili)
    DAX_NOM    NVARCHAR(45)    NULL,       -- Məktub №
    MEK_UNVAN  BIGINT          NULL,       -- İcraçı kodu (BMI: nameoi.code)
    IL         INT             NULL,       -- İl
    NOM1       BIGINT          NULL,       -- Göstərilən nömrə
    CONSTRAINT PK_DaxilMektub PRIMARY KEY (NOM)
);
GO

/* 2) Xaric olan məktub  (odb.xaric_mektub) --------------------------------- */
IF OBJECT_ID('dbo.XaricMektub', 'U') IS NULL
CREATE TABLE dbo.XaricMektub (
    KOD          BIGINT         NOT NULL,  -- Sıra/açar
    QEY_NOM      NVARCHAR(15)   NULL,      -- Qeydiyyat №
    GON_YER      NVARCHAR(255)  NULL,      -- Göndərilən yer (təyinat)
    TARIX        DATETIME2      NULL,      -- Tarix
    QISA_MEZ     NVARCHAR(255)  NULL,      -- Qısa məzmun
    ICRACI       NVARCHAR(50)   NULL,      -- İcraçı
    DUBL         NVARCHAR(10)   NULL,      -- Dublikat işarəsi
    MEKTUB_METN  NVARCHAR(MAX)  NULL,      -- Məktubun tam mətni (Oracle CLOB)
    IL           INT            NULL,      -- İl
    CONSTRAINT PK_XaricMektub PRIMARY KEY (KOD)
);
GO

/* 3) Gedən həvalə  (odb.geden_hevale) -------------------------------------- */
IF OBJECT_ID('dbo.GedenHevale', 'U') IS NULL
CREATE TABLE dbo.GedenHevale (
    HEV_NOM      NVARCHAR(10)   NOT NULL,  -- Həvalə № (YY-T-N) (açar)
    HES_NOM      NVARCHAR(20)   NULL,      -- Hesab №
    SAA          NVARCHAR(50)   NULL,      -- Soyad Ad Ata (S.A.A.)
    TIP_RES      NVARCHAR(16)   NULL,      -- Rezident tipi (fiziki/hüquqi)
    MEBLEG       DECIMAL(14,2)  NULL,      -- Məbləğ
    VAL_TIP      NVARCHAR(10)   NULL,      -- Valyuta tipi
    TARIX        DATETIME2      NULL,      -- Tarix
    MEN_OLKE     NVARCHAR(40)   NULL,      -- Mənşə ölkə
    CONTRAC_NOM  NVARCHAR(15)   NULL,      -- Müqavilə №
    DECLAR_NOM   NVARCHAR(15)   NULL,      -- Bəyannamə №
    ARAYIS       SMALLINT       NULL,      -- Arayış (bayraq, NUMBER(1))
    OLKE         NVARCHAR(40)   NULL,      -- Təyinat ölkə
    HEV_TIP      NVARCHAR(254)  NULL,      -- Həvalə tipi
    GON_TIP      NVARCHAR(20)   NULL,      -- Göndərən tipi
    AL_BANK      NVARCHAR(40)   NULL,      -- Alan bank
    ICRA         SMALLINT       NULL,      -- İcraçı (NUMBER(2))
    CONSTRAINT PK_GedenHevale PRIMARY KEY (HEV_NOM)
);
GO

/* 4) Gələn həvalə  (odb.gelen_hevale) -------------------------------------- */
IF OBJECT_ID('dbo.GelenHevale', 'U') IS NULL
CREATE TABLE dbo.GelenHevale (
    HEV_NOM   NVARCHAR(10)   NOT NULL,     -- Həvalə № (açar)
    HES_NOM   NVARCHAR(20)   NULL,         -- Hesab №
    SAA       NVARCHAR(50)   NULL,         -- Soyad Ad Ata
    TIP_RES   NVARCHAR(16)   NULL,         -- Rezident tipi
    MEBLEG    DECIMAL(10,2)  NULL,         -- Məbləğ
    VAL_TIP   NVARCHAR(10)   NULL,         -- Valyuta tipi
    TARIX     DATETIME2      NULL,         -- Tarix
    MEN_OLKE  NVARCHAR(40)   NULL,         -- Mənşə ölkə
    HEV_TIP   NVARCHAR(254)  NULL,         -- Həvalə tipi
    [DEC]     BIGINT         NULL,         -- Oracle: DEC (NUMBER(10)) — DEC SQL-də açar sözdür, ona görə [DEC]
    DEC_NOM   NVARCHAR(30)   NULL,         -- Bəyannamə №
    GEL_OLKE  NVARCHAR(40)   NULL,         -- Gələn (mənbə) ölkə
    GON_TIP   NVARCHAR(20)   NULL,         -- Göndərən tipi
    AL_BANK   NVARCHAR(250)  NULL,         -- Alan bank
    ICRA      SMALLINT       NULL,         -- İcraçı (NUMBER(2))
    CONSTRAINT PK_GelenHevale PRIMARY KEY (HEV_NOM)
);
GO

/* ----------------------------------------------------------------------------
   Data yükləmə (Oracle → SQL Server) — tövsiyə:
     • SSMS → verilənlər bazasına sağ klik → Tasks → Import Data
       (mənbə: Oracle/ODBC; hədəf: bu cədvəllər; sütunlar adla map olunur).
     • Və ya linked server: INSERT INTO dbo.GedenHevale SELECT ... FROM ORACLE..geden_hevale
     • DATE sahələrini DATETIME2-ə çevirərkən format problemi olarsa, mənbə
       sorğusunda TO_CHAR/CAST istifadə et.
   ---------------------------------------------------------------------------- */
