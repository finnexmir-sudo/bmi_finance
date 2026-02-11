using FinNex.Application.Interfaces.SenedDovriyyesi;
using Microsoft.Extensions.Configuration;

namespace FinNex.Application.Services.SenedDovriyyesi;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;

    public LocalFileStorageService(IConfiguration config)
    {
        _root = config["DocumentStorage:RootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Documents");
        Directory.CreateDirectory(_root);
    }

    public async Task<(string storedName, string path, string sha256)> SaveAsync(
        Stream stream, string originalName, string contentType)
    {
        var ext = Path.GetExtension(originalName);
        var safeExt = string.IsNullOrEmpty(ext) ? ".bin" : ext;
        var storedName = $"{Guid.NewGuid():N}{safeExt}";

        var datePath = DateTime.Now.ToString("yyyy/MM");
        var dir = Path.Combine(_root, datePath);
        Directory.CreateDirectory(dir);

        var fullPath = Path.Combine(dir, storedName);

        // Compute SHA256 first
        stream.Position = 0;
        var sha256 = await HashHelper.Sha256Async(stream);

        // Write file
        stream.Position = 0;
        using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fs);

        return (storedName, fullPath, sha256);
    }

    public Task<Stream> OpenReadAsync(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Fayl tapılmadı.", path);

        Stream s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(s);
    }
}
