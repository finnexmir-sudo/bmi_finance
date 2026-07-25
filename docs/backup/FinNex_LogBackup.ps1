<#
================================================================================
 FinNex — Transaction LOG backup (PowerShell)  —  recovery model = FULL
--------------------------------------------------------------------------------
 Axın (tam backup ilə eyni etibarlı üsul):
   1) Log-u LOKAL staging papkasına .trn kimi yazır.
   2) .trn faylını ŞƏBƏKƏ papkasına köçürür.
   3) Şəbəkədə N gündən köhnə .trn-ləri silir; lokal staging-i təmizləyir.

 Faydası: nöqtə-vaxt bərpası (istənilən dəqiqəyə) + .ldf faylının şişməsini
          dayandırır (FULL-da log backup olmasa .ldf sonsuz böyüyür).

 ŞƏRT: Ən azı bir TAM backup (FinNex_Backup.ps1) artıq alınmış olmalıdır.
 Tezlik: Task Scheduler-də hər 15 dəqiqə ("Repeat every 15 minutes").
 SIMPLE model-də təhlükəsiz keçir (log backup icra edilmir).
================================================================================
#>

# ── AYARLAR — FinNex_Backup.ps1 ilə EYNİ ───────────────────────────────────────
$SqlInstance    = "localhost\SQLEXPRESS"
$Database       = "FinNex_Maliyye_Db"
$LocalStaging   = "C:\FinNex_Backup\staging"
$BackupRoot     = "\\192.168.0.37\fs2\12345\Personal\Samir\tast_setup_local\Backup_BMI_Finance"
$RetentionDays  = 15
$UseWindowsAuth = $true
$SqlUser        = "sa"
$SqlPassword    = ""

# ── Hazırlıq ───────────────────────────────────────────────────────────────────
$ErrorActionPreference = "Stop"
$stamp    = Get-Date -Format "yyyyMMdd_HHmmss"
$netTrn   = Join-Path $BackupRoot "translog"
$logFile  = Join-Path (Join-Path $BackupRoot "log") "logbackup_$(Get-Date -Format 'yyyyMM').log"
$localTrn = Join-Path $LocalStaging "$($Database)_$stamp.trn"

foreach ($d in @($LocalStaging, $netTrn, (Split-Path $logFile))) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}
function Log($m) { Add-Content -Path $logFile -Value ("$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $m") }
$auth = if ($UseWindowsAuth) { @("-E") } else { @("-U", $SqlUser, "-P", $SqlPassword) }

# ── 1) Log backup → LOKAL (yalnız FULL-da) ─────────────────────────────────────
$tsql = "IF (SELECT recovery_model_desc FROM sys.databases WHERE name=N'$Database') = N'FULL'
         BACKUP LOG [$Database] TO DISK = N'$localTrn' WITH INIT, CHECKSUM;
         ELSE RAISERROR('Recovery model FULL deyil — log backup keçildi.',10,1);"
& sqlcmd -S $SqlInstance @auth -b -Q $tsql
if ($LASTEXITCODE -ne 0) { Log "XƏTA: log backup uğursuz (kod $LASTEXITCODE)"; exit 1 }

# FULL deyilsə fayl yaranmaya bilər — varsa köçür
if (-not (Test-Path $localTrn)) { Log "Log faylı yaranmadı (yəqin SIMPLE) — keçildi."; exit 0 }

# ── 2) .trn → ŞƏBƏKƏ ───────────────────────────────────────────────────────────
try {
    Copy-Item $localTrn -Destination $netTrn -Force
    Remove-Item $localTrn -Force
    Log "Log backup TAMAM → $netTrn\$(Split-Path $localTrn -Leaf)"
} catch {
    Log "XƏTA: .trn şəbəkəyə köçürülmədi — LOKAL qaldı: $localTrn ($($_.Exception.Message))"
    exit 1
}

# ── 3) Retention — şəbəkədə köhnə .trn sil ─────────────────────────────────────
$limit = (Get-Date).AddDays(-$RetentionDays)
Get-ChildItem -Path $netTrn -Filter "*.trn" | Where-Object { $_.LastWriteTime -lt $limit } |
    ForEach-Object { Remove-Item $_.FullName -Force; Log "Köhnə log silindi: $($_.Name)" }
