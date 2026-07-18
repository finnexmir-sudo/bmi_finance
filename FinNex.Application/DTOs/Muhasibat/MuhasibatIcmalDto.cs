namespace FinNex.Application.DTOs.Muhasibat;

// Günlük İcmal (executive) — bütün mühasibat bölmələrinin ən vacib göstəriciləri
// bir səhifədə + dünənlə (əvvəlki iş günü) müqayisə. Rəqəmlər müvafiq tab-larla
// EYNİDİR (eyni servis metodları paralel çağırılır) — ayrıca hesablama yoxdur.
public class MuhasibatIcmalDto
{
    public DateTime Tarix   { get; set; }
    public bool     Ugurlu  { get; set; }
    public string?  Xeta    { get; set; }

    // ── Balans ──────────────────────────────────────────────
    public decimal UmumiAktiv     { get; set; }
    public decimal UmumiOhdelik   { get; set; }
    public decimal Kapital        { get; set; }
    public decimal XalisMenfeet   { get; set; }   // YTD (50130)
    public decimal Roa            { get; set; }
    public decimal Roe            { get; set; }
    public decimal KapitalAktiv   { get; set; }   // kapital / aktiv, %
    public decimal AktivDeyisme   { get; set; }
    public decimal OhdelikDeyisme { get; set; }
    public decimal KapitalDeyisme { get; set; }
    public decimal MenfeetDeyisme { get; set; }
    public bool    MuqayiseVar    { get; set; }

    // ── Depozit ─────────────────────────────────────────────
    public decimal DepozitPortfel { get; set; }
    public int     DepozitorSayi  { get; set; }

    // ── Kredit ──────────────────────────────────────────────
    public decimal KreditPortfel { get; set; }
    public int     KreditSayi    { get; set; }
    public decimal Npl           { get; set; }
    public decimal NplFaiz       { get; set; }

    // ── Likvidlik ───────────────────────────────────────────
    public decimal Lcr          { get; set; }
    public decimal Hqla         { get; set; }
    public decimal AniLikvidlik { get; set; }

    // ── Mənfəət (P&L, YTD) ──────────────────────────────────
    public decimal Nii                   { get; set; }
    public decimal Nim                   { get; set; }
    public decimal EhtiyatdanEvvelMenfeet { get; set; }
}
