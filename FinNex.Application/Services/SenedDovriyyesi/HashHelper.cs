namespace FinNex.Application.Services.SenedDovriyyesi
{
    public static class HashHelper
    {
        public static async Task<string> Sha256Async(Stream stream)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = await sha.ComputeHashAsync(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
