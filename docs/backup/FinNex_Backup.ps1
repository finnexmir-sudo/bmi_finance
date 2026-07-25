<#
================================================================================
 FinNex — Avtomatik Backup skripti (PowerShell)
--------------------------------------------------------------------------------
 Nə edir:
   1) SQL Server bazasını (.bak) tarix damğası ilə backup edir (mümkünsə sıxılmış).
   2) C:\FinNex_DMS sənəd qovluğunu backup yerinə güzgüləyir (robocopy /MIR).
   3) N gündən köhnə .bak fayllarını avtomatik silir (retention).
   4) Hər işi log faylına yazır.

 İşləyir: SQL Server Express daxil BÜTÜN buraxılışlarda (Agent tələb olunmur).
 Planlaşdırma: Windows Task Scheduler ilə gündəlik (aşağıda README-yə bax).

 QEYD: Yalnız oxu/yaz SQL Server-ə aiddir — Oracle (BMI) bu skriptə DAXİL DEYİL.
       Oracle backup-ı bank tərəfindən ayrıca idarə olunur (yalnız-oxu bazadır).
================================================================================
#>

# ── AYARLAR — mühitə görə dəyiş ────────────────────────────────────────────────
$SqlInstance   = "localhost"                 # məs: "localhost\SQLEXPRESS" və ya "SERVER01"
$Database      = "FinNex_Maliyye_Db"         # baza adı (appsettings-dən)
$BackupRoot    = "\\NAS01\Backups\FinNex"    # ⚠️ AYRI disk/NAS tövsiyə olunur (3-2-1)
$DmsSource     = "C:\FinNex_DMS"             # sənəd qovluğu (yoxdursa keçilir)
$RetentionDays = 30                          # neçə gün .bak saxlanılsın
$UseWindowsAuth = $true                      # $true = Windows auth; $false = SQL login
$SqlUser       = "sa"                         # UseWindowsAuth=$false olduqda
$SqlPassword   = ""                           # UseWindowsAuth=$false olduqda

# ── Hazırlıq ───────────────────────────────────────────────────────────────────
$ErrorActionPreference = "Stop"
$stamp   = Get-Date -Format "yyyyMMdd_HHmmss"
$dbDir   = Join-Path $BackupRoot "sql"
$dmsDir  = Join-Path $BackupRoot "dms"
$logDir  = Join-Path $BackupRoot "log"
$bakFile = Join-Path $dbDir "$($Database)_$stamp.bak"
$logFile = Join-Path $logDir "backup_$(Get-Date -Format 'yyyyMM').log"

foreach ($d in @($dbDir, $dmsDir, $logDir)) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}

function Log($msg) {
    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $msg"
    Add-Content -Path $logFile -Value $line
    Write-Host $line
}

# sqlcmd üçün auth arqumentləri
$auth = if ($UseWindowsAuth) { @("-E") } else { @("-U", $SqlUser, "-P", $SqlPassword) }

Log "=== FinNex backup BAŞLADI (baza=$Database, instance=$SqlInstance) ==="

# ── 1) SQL bazası backup ───────────────────────────────────────────────────────
# Əvvəl COMPRESSION ilə cəhd (Standard/Enterprise); Express-də alınmasa sıxılmasız.
$tsqlCompressed = "BACKUP DATABASE [$Database] TO DISK = N'$bakFile' WITH INIT, COMPRESSION, CHECKSUM, STATS = 10;"
$tsqlPlain      = "BACKUP DATABASE [$Database] TO DISK = N'$bakFile' WITH INIT, CHECKSUM, STATS = 10;"

try {
    Log "SQL backup (sıxılmış) → $bakFile"
    & sqlcmd -S $SqlInstance @auth -b -Q $tsqlCompressed
    if ($LASTEXITCODE -ne 0) { throw "compression rejected" }
}
catch {
    Log "Sıxılma alınmadı (yəqin Express) — sıxılmasız təkrar cəhd."
    & sqlcmd -S $SqlInstance @auth -b -Q $tsqlPlain
    if ($LASTEXITCODE -ne 0) { Log "XƏTA: SQL backup uğursuz (kod $LASTEXITCODE)"; exit 1 }
}
Log "SQL backup TAMAM: $((Get-Item $bakFile).Length / 1MB) MB"

# ── 2) DMS sənəd qovluğu (robocopy /MIR — cari vəziyyətin güzgüsü) ──────────────
if (Test-Path $DmsSource) {
    Log "DMS güzgülənir: $DmsSource → $dmsDir"
    # /MIR mirror, /R:2 retry, /W:5 gözləmə, /NP progress yox, /NFL/NDL az log
    & robocopy $DmsSource $dmsDir /MIR /R:2 /W:5 /NP /NFL /NDL /LOG+:$logFile
    # robocopy 0-7 = uğur (8+ = xəta)
    if ($LASTEXITCODE -ge 8) { Log "XƏBƏRDARLIQ: DMS robocopy xəta kodu $LASTEXITCODE" }
    else { Log "DMS güzgü TAMAM (robocopy kod $LASTEXITCODE)" }
} else {
    Log "DMS mənbəyi tapılmadı ($DmsSource) — keçilir."
}

# ── 3) Retention — köhnə .bak fayllarını sil ───────────────────────────────────
$limit = (Get-Date).AddDays(-$RetentionDays)
$old = Get-ChildItem -Path $dbDir -Filter "*.bak" | Where-Object { $_.LastWriteTime -lt $limit }
foreach ($f in $old) {
    Remove-Item $f.FullName -Force
    Log "Köhnə backup silindi: $($f.Name)"
}

Log "=== FinNex backup BİTDİ ==="
