# FinNex — Avtomatik Backup

## ⭐ BU SERVER ÜÇÜN DƏQİQ PLAN (yoxlanmış: Express + FULL)

Server: **SQL Server 2022 Express (64-bit)**, baza **FULL** recovery model-də.
Deməli: **PowerShell + Task Scheduler** (Agent yoxdur) **VƏ log backup MÜTLƏQ**.

**Sıra ilə et:**

1. **Backup hədəfini seç** — əsas serverdən BAŞQA yer (ayrı disk/NAS). Skriptlərdə
   `$BackupRoot`-u ora yönəlt (məs. `\\NAS01\Backups\FinNex` və ya `E:\Backups\FinNex`).
2. **Tam backup qur** — `FinNex_Backup.ps1` (baza + `C:\FinNex_DMS`), gündəlik 02:00.
   `$SqlInstance = "localhost\SQLEXPRESS"` təyin et, əl ilə bir dəfə işlət, `.bak` yarandığını gör.
3. **Log backup qur** — `FinNex_LogBackup.ps1`, hər **15 dəqiqə**. FULL olduğu üçün bu
   şərtdir: nöqtə-vaxt bərpası verir + `.ldf` şişməsini dayandırır.
4. **Task Scheduler**-də hər iki skriptə tapşırıq (aşağıda addımlar + log üçün "Repeat").
5. **Test**: hər iki task-ı əl ilə "Run", `\sql` və `\translog` qovluqlarında fayl yoxla.
6. **Bərpa testi**: başqa/test serverdə son `.bak` + sonrakı `.trn`-ləri `RESTORE ... WITH
   NORECOVERY` / `STOPAT` ilə bərpa et (ayda bir).

> ⚠️ FULL + log backup olmadan `.ldf` faylı sonsuz böyüyür. Log backup qurulan kimi
> log hər dəfə "təkrar istifadəyə" açılır və şişmə dayanır. Log backup-ı MÜTLƏQ qur.

---


İki fayl var, **birini** seç (hər ikisini işlətmə):

| Vəziyyət | İstifadə et |
|---|---|
| SQL Server **Standard / Enterprise** (Agent var) | `FinNex_Backup_AgentJob.sql` |
| SQL Server **Express** (Agent yoxdur) və ya sadə yol | `FinNex_Backup.ps1` + Task Scheduler |

> Buraxılışı bilmirsənsə SSMS-də işlət: `SELECT SERVERPROPERTY('Edition'), SERVERPROPERTY('EngineEdition');`
> `EngineEdition = 4` → **Express** (Agent yoxdur → PowerShell yolu).

Backup olunan: **`FinNex_Maliyye_Db`** bazası + **`C:\FinNex_DMS`** sənəd qovluğu.
Oracle (BMI) DAXİL DEYİL — o, yalnız-oxu bazadır, backup-ı bank tərəfindən idarə olunur.

---

## Yol A — SQL Server Agent (tövsiyə, Standard/Enterprise)

1. SSMS-də `FinNex_Backup_AgentJob.sql` aç.
2. Yuxarıdakı `@backupDir`-i **ayrı diskə/şəbəkə payına** dəyiş (məs. `\\NAS01\Backups\FinNex\sql`).
3. İşlət. Job yaranır: **SQL Server Agent → Jobs → "FinNex - Gündəlik Backup"** (hər gün 02:00).
4. Agent xidmət hesabının həmin qovluğa **yazma icazəsi** olmalıdır.
5. **DMS qovluğu** üçün ayrıca Task Scheduler-də bir robocopy tapşırığı qur (aşağı Yol B-dəki robocopy sətrini istifadə et) — Agent job yalnız SQL-i backup edir.

---

## Yol B — Task Scheduler + PowerShell (Express daxil hamıda işləyir)

1. `FinNex_Backup.ps1` faylını serverə köçür (məs. `C:\FinNex_Backup\FinNex_Backup.ps1`).
2. Faylın yuxarısındakı **AYARLAR** blokunu dəyiş:
   - `$SqlInstance` — məs. `localhost\SQLEXPRESS`
   - `$BackupRoot` — **ayrı disk/NAS** (məs. `\\NAS01\Backups\FinNex`)
   - lazımsa `$UseWindowsAuth = $false` + `$SqlUser`/`$SqlPassword`
3. Əl ilə bir dəfə yoxla (PowerShell-i **Administrator** aç):
   ```powershell
   powershell -ExecutionPolicy Bypass -File "C:\FinNex_Backup\FinNex_Backup.ps1"
   ```
   `\\NAS01\Backups\FinNex\sql`-də `.bak`, `\dms`-də sənədlər, `\log`-da jurnal yaranmalıdır.
4. **Task Scheduler** → Create Task:
   - **General**: "Run whether user is logged on or not" + "Run with highest privileges". İstifadəçi: SQL-ə və backup qovluğuna icazəsi olan hesab.
   - **Triggers**: Daily, 02:00.
   - **Actions**: Start a program
     - Program: `powershell.exe`
     - Arguments: `-ExecutionPolicy Bypass -File "C:\FinNex_Backup\FinNex_Backup.ps1"`
5. Task-ı sağ-klik → **Run** ilə bir dəfə test et.

### Log backup tapşırığı (FinNex_LogBackup.ps1 — FULL üçün, hər 15 dəq)

`FinNex_Backup.ps1` üçün olan addımların EYNİSİ, iki fərqlə:
- **Action → Arguments**: `-ExecutionPolicy Bypass -File "C:\FinNex_Backup\FinNex_LogBackup.ps1"`
- **Triggers**: Daily, başlanğıc 00:00 → **"Repeat task every: 15 minutes"**, **"for a duration of: 1 day"** (Indefinitely).

Beləliklə tam backup gündə 1 dəfə (02:00), log backup gün boyu hər 15 dəqiqə işləyir.

### Bərpa nümunəsi (nöqtə-vaxt — məs. 14:37-yə qədər)
```sql
-- 1) Son tam backup-ı NORECOVERY ilə bərpa et
RESTORE DATABASE FinNex_Maliyye_Db FROM DISK='...\sql\FinNex_Maliyye_Db_20260725_020000.bak'
  WITH NORECOVERY, REPLACE;
-- 2) 02:00-dan sonrakı bütün .trn-ləri sıra ilə NORECOVERY ilə tətbiq et
RESTORE LOG FinNex_Maliyye_Db FROM DISK='...\translog\FinNex_Maliyye_Db_20260725_021500.trn' WITH NORECOVERY;
-- ... aralıqdakı hamısı ...
-- 3) Son .trn-də dəqiq vaxta dayan
RESTORE LOG FinNex_Maliyye_Db FROM DISK='...\translog\FinNex_Maliyye_Db_20260725_143000.trn'
  WITH RECOVERY, STOPAT = '2026-07-25T14:37:00';
```

---

## Vacib tövsiyələr

- **3-2-1 qaydası**: 3 nüsxə, 2 fərqli media, 1 offsite. Backup-ı **əsas serverdən başqa** yerə yaz (NAS/başqa server). Eyni diskə yazma — disk sınsa hər ikisi itir.
- **Bərpanı test et**: backup varsa da, ayda bir dəfə test serverdə `RESTORE DATABASE ... FROM DISK` ilə bərpanı yoxla. Test edilməyən backup — backup deyil.
- **Recovery model**:
  - `SIMPLE` — yalnız gündəlik tam backup (gün ərzindəki dəyişikliklər itə bilər).
  - `FULL` — nöqtə-vaxt bərpası üçün **log backup** da lazımdır (məs. hər 1 saatdan bir `BACKUP LOG`). Maliyyə sistemi üçün `FULL` + saatlıq log tövsiyə olunur.
  Yoxla: `SELECT name, recovery_model_desc FROM sys.databases WHERE name='FinNex_Maliyye_Db';`
- **Şifrələmə**: backup şəbəkə payındadırsa, `BACKUP ... WITH ENCRYPTION` və ya NAS-da şifrələnmiş həcm istifadə et (maaş/maliyyə datası).
- **Monitorinq**: `\log` qovluğundakı jurnala vaxtaşırı bax; job/task uğursuz olsa e-poçt bildirişi qur (Agent-də `sp_add_operator` + `@notify_...`).

## Fayl strukturu (backup yerində yaranır)
```
\\NAS01\Backups\FinNex\
├─ sql\   FinNex_Maliyye_Db_20260725_020000.bak ...
├─ dms\   (C:\FinNex_DMS güzgüsü)
└─ log\   backup_202607.log
```
