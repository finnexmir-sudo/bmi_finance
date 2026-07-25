<#
================================================================================
 FinNex — Transaction LOG backup (PowerShell)  —  YALNIZ recovery model = FULL
--------------------------------------------------------------------------------
 Nə edir:
   1) FinNex_Maliyye_Db bazasının transaction log-unu (.trn) backup edir.
   2) Bu, NÖQTƏ-VAXT bərpasına imkan verir (istənilən dəqiqəyə geri qayıt).
   3) Log-u "təkrar istifadəyə açır" → .ldf faylının sonsuz şişməsini dayandırır.

 ŞƏRT: Ən azı BİR tam backup (FinNex_Backup.ps1) artıq alınmış olmalıdır —
       log zənciri tam backup-dan başlayır. Əvvəlcə tam backup-ı qur, sonra bunu.

 Tezlik: hər 15 dəqiqədən bir tövsiyə olunur (maliyyə sistemi). Minimum saatlıq.
         Task Scheduler-də "Repeat every 15 minutes" ilə işlədilir.

 QEYD: SIMPLE recovery model-də BU SKRİPT İŞLƏMİR (log backup icazə verilmir) —
       o halda ehtiyac da yoxdur. Yalnız FULL-da işlət.
================================================================================
#>

# ── AYARLAR — FinNex_Backup.ps1 ilə eyni saxla ─────────────────────────────────
$SqlInstance    = "localhost\SQLEXPRESS"     # Express instansiyası
$Database       = "FinNex_Maliyye_Db"
$BackupRoot     = "\\NAS01\Backups\FinNex"   # tam backup ilə eyni kök
$RetentionDays  = 15                          # neçə gün .trn saxlanılsın
$UseWindowsAuth = $true
$SqlUser        = "sa"
$SqlPassword    = ""

# ── Hazırlıq ───────────────────────────────────────────────────────────────────
$ErrorActionPreference = "Stop"
$stamp   = Get-Date -Format "yyyyMMdd_HHmmss"
$logDir2 = Join-Path $BackupRoot "translog"          # .trn faylları
$logFile = Join-Path (Join-Path $BackupRoot "log") "logbackup_$(Get-Date -Format 'yyyyMM').log"
$trnFile = Join-Path $logDir2 "$($Database)_$stamp.trn"

foreach ($d in @($logDir2, (Split-Path $logFile))) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}
function Log($m) {
    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $m"
    Add-Content -Path $logFile -Value $line
}

$auth = if ($UseWindowsAuth) { @("-E") } else { @("-U", $SqlUser, "-P", $SqlPassword) }

# ── Log backup ─────────────────────────────────────────────────────────────────
$tsql = "IF (SELECT recovery_model_desc FROM sys.databases WHERE name=N'$Database') = N'FULL'
         BACKUP LOG [$Database] TO DISK = N'$trnFile' WITH INIT, CHECKSUM;
         ELSE RAISERROR('Recovery model FULL deyil — log backup keçildi.',10,1);"

& sqlcmd -S $SqlInstance @auth -b -Q $tsql
if ($LASTEXITCODE -ne 0) { Log "XƏTA: log backup uğursuz (kod $LASTEXITCODE)"; exit 1 }
Log "Log backup TAMAM: $trnFile"

# ── Retention — köhnə .trn sil ─────────────────────────────────────────────────
$limit = (Get-Date).AddDays(-$RetentionDays)
Get-ChildItem -Path $logDir2 -Filter "*.trn" |
    Where-Object { $_.LastWriteTime -lt $limit } |
    ForEach-Object { Remove-Item $_.FullName -Force; Log "Köhnə log silindi: $($_.Name)" }
