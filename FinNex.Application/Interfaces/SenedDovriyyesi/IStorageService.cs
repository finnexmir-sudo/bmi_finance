namespace FinNex.Application.Interfaces.SenedDovriyyesi
{
    public interface IStorageService
    {
        Task<(string storedFileName, string fullPath)> SaveAsync(
            Stream fileStream,
            string originalFileName,
            string extension,
            string folderPath);

        Task<Stream> OpenReadAsync(string fullPath);
    }

}
