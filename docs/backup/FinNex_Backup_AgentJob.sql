/* ============================================================================
   FinNex — SQL Server AGENT ilə avtomatik backup (Standard / Enterprise)
   ----------------------------------------------------------------------------
   SQL Server Agent varsa (Express-də YOXDUR) planlı job ən rahat yoldur.
   Bu skript hər gün 02:00-da sıxılmış tam backup götürən Agent job yaradır
   və 30 gündən köhnə .bak fayllarını təmizləyir.

   ⚠️ @backupDir-i AYRI diskə/şəbəkə payına yönəlt (3-2-1 qaydası).
      Agent xidmət hesabının həmin qovluğa YAZMA icazəsi olmalıdır.
   ============================================================================ */
USE msdb;
GO

DECLARE @jobName  SYSNAME = N'FinNex - Gündəlik Backup';
DECLARE @db       SYSNAME = N'FinNex_Maliyye_Db';
DECLARE @backupDir NVARCHAR(260) = N'\\NAS01\Backups\FinNex\sql';   -- ⚠️ dəyiş
DECLARE @retDays  INT = 30;

-- Köhnə job varsa sil (təkrar quraşdırma üçün təhlükəsiz)
IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = @jobName)
    EXEC msdb.dbo.sp_delete_job @job_name = @jobName;

EXEC msdb.dbo.sp_add_job @job_name = @jobName,
     @description = N'FinNex bazasının gündəlik sıxılmış tam backup-ı + retention';

-- Addım 1: tam backup (tarix damğalı fayl)
DECLARE @cmdBackup NVARCHAR(MAX) = N'
DECLARE @f NVARCHAR(260) = N''' + @backupDir + N'\' + @db + N'_'' +
    REPLACE(CONVERT(VARCHAR(20), GETDATE(), 112) + ''_'' +
            REPLACE(CONVERT(VARCHAR(8), GETDATE(), 108), '':'', ''''), '' '', '''') + N''.bak'';
BACKUP DATABASE [' + @db + N'] TO DISK = @f
WITH INIT, COMPRESSION, CHECKSUM, STATS = 10;';

EXEC msdb.dbo.sp_add_jobstep @job_name = @jobName,
     @step_name = N'Tam backup', @subsystem = N'TSQL',
     @database_name = @db, @command = @cmdBackup, @on_success_action = 3;

-- Addım 2: retention — köhnə .bak sil (xMp_cmdshell yox, sp_delete file API)
DECLARE @cmdClean NVARCHAR(MAX) = N'
EXECUTE master.dbo.xp_delete_file 0, N''' + @backupDir + N''', N''bak'',
        DATEADD(DAY, -' + CAST(@retDays AS NVARCHAR(10)) + N', GETDATE());';

EXEC msdb.dbo.sp_add_jobstep @job_name = @jobName,
     @step_name = N'Köhnə backup təmizlə', @subsystem = N'TSQL',
     @database_name = N'master', @command = @cmdClean;

-- Cədvəl: hər gün 02:00
EXEC msdb.dbo.sp_add_schedule @schedule_name = N'FinNex-Gundelik-02',
     @freq_type = 4, @freq_interval = 1,          -- gündəlik
     @active_start_time = 020000;                 -- 02:00:00
EXEC msdb.dbo.sp_attach_schedule @job_name = @jobName, @schedule_name = N'FinNex-Gundelik-02';

EXEC msdb.dbo.sp_add_jobserver @job_name = @jobName;   -- lokal serverdə işlə

PRINT N'Job yaradıldı: ' + @jobName + N'. SSMS → SQL Server Agent → Jobs-da görünür.';
GO
