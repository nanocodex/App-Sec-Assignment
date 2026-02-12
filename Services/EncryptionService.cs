using Microsoft.AspNetCore.DataProtection;

namespace WebApplication1.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string encryptedText);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly IDataProtector _protector;

        public EncryptionService(IDataProtectionProvider dataProtectionProvider)
        {
            // Create a protector with a specific purpose string
            // This ensures that data encrypted for this purpose can only be decrypted by the same purpose
            _protector = dataProtectionProvider.CreateProtector("UserSensitiveData.Protection.v1");
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            return _protector.Protect(plainText);
        }

        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return string.Empty;
            }

            try
            {
                return _protector.Unprotect(encryptedText);
            }
            catch
            {
                // If decryption fails, return a masked value instead of throwing
                // This prevents errors if encryption keys change
                return "****";
            }
        }
    }
}
