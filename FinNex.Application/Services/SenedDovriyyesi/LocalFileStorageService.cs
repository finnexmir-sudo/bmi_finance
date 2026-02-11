using FinNex.Application.Interfaces.SenedDovriyyesi;
using Microsoft.Extensions.Configuration;

namespace FinNex.Application.Services.SenedDovriyyesi;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;

    public LocalFileStorageService(IConfiguration config)
    {
        _root = config["DocumentStorage:RootPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Documents");

        Directory.CreateDirectory(_root);
    }

    public async Task<(string storedName, string path, string sha256)> SaveAsync(
        Stream stream, string originalName, string contentType)
    {
        var ext = Path.GetExtension(originalName);
        var safeExt = string.IsNullOrWhiteSpace(ext) ? ".bin" : ext;

        var storedName = $"{Guid.NewGuid():N}{safeExt}";

        var datePath = DateTime.Now.ToString("yyyy/MM");
        var relativePath = Path.Combine(datePath, storedName);

        var fullDirectory = Path.Combine(_root, datePath);
        Directory.CreateDirectory(fullDirectory);

        var fullPath = Path.Combine(fullDirectory, storedName);

        // SHA256
        stream.Position = 0;
        var sha256 = await HashHelper.Sha256Async(stream);

        // Write file
        stream.Position = 0;
        using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fs);

        // DB-ə relative path qaytarırıq
        return (storedName, relativePath, sha256);
    }

    public Task<Stream> OpenReadAsync(string relativePath)
    {
        var fullPath = Path.Combine(_root, relativePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Fayl tapılmadı.", fullPath);

        Stream s = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(s);
    }
}
