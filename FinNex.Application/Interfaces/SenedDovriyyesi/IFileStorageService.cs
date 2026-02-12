namespace FinNex.Application.Interfaces.SenedDovriyyesi
{
    public interface IFileStorageService
    {
        Task<(string storedName, string path, string sha256)> SaveAsync(
            Stream stream, string originalName, string contentType);

        Task<Stream> OpenReadAsync(string path);
    }

}
