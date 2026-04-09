using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace FinNex.UI.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class VapidController : Controller
    {
        [HttpGet]
        public IActionResult Generate()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var parameters = ecdsa.ExportParameters(true);

            var publicKeyRaw = new byte[65];
            publicKeyRaw[0] = 0x04;
            Buffer.BlockCopy(parameters.Q.X!, 0, publicKeyRaw, 1, 32);
            Buffer.BlockCopy(parameters.Q.Y!, 0, publicKeyRaw, 33, 32);

            var publicKey = Base64UrlEncode(publicKeyRaw);
            var privateKey = Base64UrlEncode(parameters.D!);

            return Json(new
            {
                publicKey,
                privateKey,
                instruction = "Bu dəyərləri appsettings.json-a yazın"
            });
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
