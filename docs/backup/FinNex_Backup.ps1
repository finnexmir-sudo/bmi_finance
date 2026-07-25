<#
================================================================================
 FinNex — Avtomatik TAM Backup (PowerShell)  —  SQL Express + FULL model
--------------------------------------------------------------------------------
 Axın (etibarlı üsul — şəbəkə icazə problemini keçir):
   1) SQL bazasını LOKAL staging papkasına .bak kimi yazır (SQL xidmət hesabı
      lokal papkaya yaza bilir; şəbəkəyə birbaşa yaza bilməyə də bilər).
   2) .bak faylını ŞƏBƏKƏ papkasına köçürür (bu PowerShell sənin hesabınla işləyir).
   3) C:\FinNex_DMS sənəd qovluğunu şəbəkəyə güzgüləyir (robocopy).
   4) Şəbəkədə N gündən köhnə .bak-ları silir; lokal staging-i təmizləyir.
   5) Hər işi log-a yazır.

 Planlaşdırma: Task Scheduler, gündəlik 02:00.
 Task-ı ŞƏBƏKƏ papkasına icazəsi olan hesabla işlət ("Run whether user is
 logged on or not" + parol saxla).
================================================================================
#>

# ── AYARLAR ────────────────────────────────────────────────────────────────────
$SqlInstance   = "localhost\SQLEXPRESS"       # serverdə adlı instansiya (MSSQL$SQLEXPRESS). Default olsaydı: "localhost"
$Database      = "FinNex_Maliyye_Db"
$LocalStaging  = "C:\FinNex_Backup\staging"                                   # SQL bura yazır (lokal)
$BackupRoot    = "\\192.168.0.37\fs2\12345\Personal\Samir\tast_setup_local\Backup_BMI_Finance"  # şəbəkə hədəf
$DmsSource     = "C:\FinNex_DMS"
$RetentionDays = 30
$UseWindowsAuth = $true
$SqlUser       = "sa"
$SqlPassword   = ""

# ── Hazırlıq ───────────────────────────────────────────────────────────────────
$ErrorActionPreference = "Stop"
$stamp    = Get-Date -Format "yyyyMMdd_HHmmss"
$netSql   = Join-Path $BackupRoot "sql"
$netDms   = Join-Path $BackupRoot "dms"
$netLog   = Join-Path $BackupRoot "log"
$localBak = Join-Path $LocalStaging "$($Database)_$stamp.bak"
$logFile  = Join-Path $netLog "backup_$(Get-Date -Format 'yyyyMM').log"

foreach ($d in @($LocalStaging, $netSql, $netDms, $netLog)) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}
function Log($m) {
    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $m"
    Add-Content -Path $logFile -Value $line
    Write-Host $line
}
# -C = TrustServerCertificate (ODBC Driver 18 default Encrypt=Yes + self-signed sertifikat üçün MƏCBURİ)
$auth = if ($UseWindowsAuth) { @("-E", "-C") } else { @("-U", $SqlUser, "-P", $SqlPassword, "-C") }

Log "=== FinNex TAM backup BAŞLADI ==="

# ── 1) SQL backup → LOKAL staging (sıxılmış cəhd; Express-də sıxılmasız) ────────
$tsqlC = "BACKUP DATABASE [$Database] TO DISK = N'$localBak' WITH INIT, COMPRESSION, CHECKSUM, STATS = 10;"
$tsqlP = "BACKUP DATABASE [$Database] TO DISK = N'$localBak' WITH INIT, CHECKSUM, STATS = 10;"
try {
    Log "SQL backup (sıxılmış) → $localBak"
    & sqlcmd -S $SqlInstance @auth -b -Q $tsqlC
    if ($LASTEXITCODE -ne 0) { throw "compression rejected" }
} catch {
    Log "Sıxılma alınmadı (Express) — sıxılmasız təkrar."
    & sqlcmd -S $SqlInstance @auth -b -Q $tsqlP
    if ($LASTEXITCODE -ne 0) { Log "XƏTA: SQL backup uğursuz (kod $LASTEXITCODE)"; exit 1 }
}
Log "Lokal .bak hazır: $([math]::Round((Get-Item $localBak).Length/1MB,1)) MB"

# ── 2) .bak → ŞƏBƏKƏ (köçür) ───────────────────────────────────────────────────
try {
    Copy-Item $localBak -Destination $netSql -Force
    Log "Şəbəkəyə köçürüldü: $netSql"
    Remove-Item $localBak -Force            # staging təmizlə (şəbəkədə nüsxə var)
    Log "Lokal staging silindi."
} catch {
    Log "XƏTA: şəbəkəyə köçürmə uğursuz — .bak LOKAL qaldı: $localBak ($($_.Exception.Message))"
    exit 1
}

# ── 3) DMS sənəd qovluğu → şəbəkə güzgü ────────────────────────────────────────
if (Test-Path $DmsSource) {
    Log "DMS güzgülənir: $DmsSource → $netDms"
    & robocopy $DmsSource $netDms /MIR /R:2 /W:5 /NP /NFL /NDL /LOG+:$logFile
    if ($LASTEXITCODE -ge 8) { Log "XƏBƏRDARLIQ: DMS robocopy xəta kodu $LASTEXITCODE" }
    else { Log "DMS güzgü TAMAM (kod $LASTEXITCODE)" }
} else { Log "DMS mənbəyi yoxdur ($DmsSource) — keçildi." }

# ── 4) Retention — şəbəkədə köhnə .bak sil ─────────────────────────────────────
$limit = (Get-Date).AddDays(-$RetentionDays)
Get-ChildItem -Path $netSql -Filter "*.bak" | Where-Object { $_.LastWriteTime -lt $limit } |
    ForEach-Object { Remove-Item $_.FullName -Force; Log "Köhnə backup silindi: $($_.Name)" }

Log "=== FinNex TAM backup BİTDİ ==="
