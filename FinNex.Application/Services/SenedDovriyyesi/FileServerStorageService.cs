using FinNex.Application.Interfaces.SenedDovriyyesi;
using Microsoft.Extensions.Configuration;

namespace FinNex.Application.Services.SenedDovriyyesi
{
    public class FileServerStorageService : IStorageService
    {
        private readonly string _root;

        public FileServerStorageService(IConfiguration config)
        {
            _root = config["DocumentStorage:RootPath"] ?? throw new Exception("DocumentStorage:RootPath yoxdur");
            Directory.CreateDirectory(_root);
        }

        public async Task<(string storedFileName, string fullPath)> SaveAsync(
            Stream fileStream, string originalFileName, string extension, string folderPath)
        {
            var safeExt = extension.StartsWith(".") ? extension : "." + extension;
            var stored = $"{Guid.NewGuid():N}{safeExt}";

            var dir = Path.Combine(_root, folderPath);
            Directory.CreateDirectory(dir);

            var fullPath = Path.Combine(dir, stored);

            using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await fileStream.CopyToAsync(fs);

            return (stored, fullPath);
        }

        public Task<Stream> OpenReadAsync(string fullPath)
        {
            Stream s = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(s);
        }
    }

}
