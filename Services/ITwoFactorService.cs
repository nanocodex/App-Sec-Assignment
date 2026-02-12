namespace WebApplication1.Services
{
    public interface ITwoFactorService
    {
        /// <summary>
        /// Generates a shared secret key for TOTP authentication
        /// </summary>
        string GenerateSecretKey();

        /// <summary>
        /// Generates a QR code as a base64 string for authenticator app setup
        /// </summary>
        string GenerateQrCodeUri(string email, string secretKey, string issuer = "AppSecAssignment");

        /// <summary>
        /// Generates a QR code image as base64 string
        /// </summary>
        string GenerateQrCodeImage(string qrCodeUri);

        /// <summary>
        /// Verifies a TOTP code against the user's secret key
        /// </summary>
        bool VerifyTotpCode(string secretKey, string code);

        /// <summary>
        /// Generates recovery codes for backup authentication
        /// </summary>
        List<string> GenerateRecoveryCodes(int count = 10);

        /// <summary>
        /// Encrypts recovery codes for storage
        /// </summary>
        string EncryptRecoveryCodes(List<string> codes);

        /// <summary>
        /// Decrypts recovery codes from storage
        /// </summary>
        List<string> DecryptRecoveryCodes(string encryptedCodes);

        /// <summary>
        /// Verifies and consumes a recovery code
        /// </summary>
        bool VerifyAndConsumeRecoveryCode(string encryptedCodes, string code, out string updatedEncryptedCodes);
    }
}
