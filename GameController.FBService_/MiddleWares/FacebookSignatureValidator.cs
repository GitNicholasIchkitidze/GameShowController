
using System;
using System.Security.Cryptography;
using System.Text;

namespace GameController.FBService.MiddleWares
{
    public class FacebookSignatureValidator : IFacebookSignatureValidator
    {
        private readonly string _appSecret;

        public FacebookSignatureValidator(string appSecret)
        {
            _appSecret = appSecret ?? throw new ArgumentNullException(nameof(appSecret));
        }
        public bool IsSignatureValid(string requestBody, string signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader))
                return false;

            if (string.IsNullOrWhiteSpace(requestBody))
                return false;

            var expectedPrefix = "sha256=";

            if (!signatureHeader.StartsWith(expectedPrefix))
                return false;

            var providedHash = signatureHeader.Substring(expectedPrefix.Length);

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_appSecret)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
                var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                return computedHash.Equals(providedHash, StringComparison.OrdinalIgnoreCase);
            }
        }

    }
}
