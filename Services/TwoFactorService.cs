using System.Security.Cryptography;
using System.Text;
using QRCoder;

namespace WebApplication1.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<TwoFactorService> _logger;

        public TwoFactorService(IEncryptionService encryptionService, ILogger<TwoFactorService> logger)
        {
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public string GenerateSecretKey()
        {
            // Generate a random 20-byte secret (160 bits as per RFC 4226)
            var buffer = new byte[20];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }
            
            // Convert to Base32 (standard for TOTP)
            return Base32Encode(buffer);
        }

        public string GenerateQrCodeUri(string email, string secretKey, string issuer = "AppSecAssignment")
        {
            // Format: otpauth://totp/Issuer:user@email.com?secret=SECRETKEY&issuer=Issuer
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedEmail = Uri.EscapeDataString(email);
            
            return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secretKey}&issuer={encodedIssuer}";
        }

        public string GenerateQrCodeImage(string qrCodeUri)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeImage = qrCode.GetGraphic(20);
                
                return Convert.ToBase64String(qrCodeImage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code");
                throw;
            }
        }

        public bool VerifyTotpCode(string secretKey, string code)
        {
            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(code))
            {
                return false;
            }

            // Allow for time drift: check current time window and 1 window before/after
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timeStep = 30; // 30 seconds per TOTP window

            for (int i = -1; i <= 1; i++)
            {
                var timeWindow = (unixTimestamp / timeStep) + i;
                var expectedCode = GenerateTotpCode(secretKey, timeWindow);
                
                if (expectedCode == code)
                {
                    return true;
                }
            }

            return false;
        }

        public List<string> GenerateRecoveryCodes(int count = 10)
        {
            var codes = new List<string>();
            
            for (int i = 0; i < count; i++)
            {
                codes.Add(GenerateRecoveryCode());
            }
            
            return codes;
        }

        public string EncryptRecoveryCodes(List<string> codes)
        {
            var joined = string.Join(";", codes);
            return _encryptionService.Encrypt(joined);
        }

        public List<string> DecryptRecoveryCodes(string encryptedCodes)
        {
            if (string.IsNullOrEmpty(encryptedCodes))
            {
                return new List<string>();
            }

            try
            {
                var decrypted = _encryptionService.Decrypt(encryptedCodes);
                return decrypted.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting recovery codes");
                return new List<string>();
            }
        }

        public bool VerifyAndConsumeRecoveryCode(string encryptedCodes, string code, out string updatedEncryptedCodes)
        {
            updatedEncryptedCodes = encryptedCodes;
            
            if (string.IsNullOrEmpty(encryptedCodes) || string.IsNullOrEmpty(code))
            {
                return false;
            }

            var codes = DecryptRecoveryCodes(encryptedCodes);
            
            // Case-insensitive comparison and remove any whitespace
            var normalizedInputCode = code.Replace("-", "").Replace(" ", "").ToUpperInvariant();
            
            for (int i = 0; i < codes.Count; i++)
            {
                var normalizedStoredCode = codes[i].Replace("-", "").Replace(" ", "").ToUpperInvariant();
                
                if (normalizedStoredCode == normalizedInputCode)
                {
                    // Remove the used code
                    codes.RemoveAt(i);
                    updatedEncryptedCodes = EncryptRecoveryCodes(codes);
                    return true;
                }
            }

            return false;
        }

        // Private helper methods

        private string GenerateTotpCode(string secretKey, long timeCounter)
        {
            var secretBytes = Base32Decode(secretKey);
            var counterBytes = BitConverter.GetBytes(timeCounter);
            
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }

            using var hmac = new HMACSHA1(secretBytes);
            var hash = hmac.ComputeHash(counterBytes);
            
            // Dynamic truncation as per RFC 4226
            var offset = hash[^1] & 0x0F;
            var binary = ((hash[offset] & 0x7F) << 24)
                       | ((hash[offset + 1] & 0xFF) << 16)
                       | ((hash[offset + 2] & 0xFF) << 8)
                       | (hash[offset + 3] & 0xFF);

            var otp = binary % 1000000;
            return otp.ToString("D6"); // 6-digit code
        }

        private string GenerateRecoveryCode()
        {
            // Generate 8-character alphanumeric code (e.g., ABCD-1234)
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed confusing characters
            var random = new byte[8];
            
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(random);
            }

            var code = new StringBuilder(9);
            for (int i = 0; i < 8; i++)
            {
                code.Append(chars[random[i] % chars.Length]);
                if (i == 3)
                {
                    code.Append('-');
                }
            }

            return code.ToString();
        }

        private static string Base32Encode(byte[] data)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new StringBuilder((data.Length * 8 + 4) / 5);

            int buffer = data[0];
            int next = 1;
            int bitsLeft = 8;

            while (bitsLeft > 0 || next < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (next < data.Length)
                    {
                        buffer <<= 8;
                        buffer |= data[next++] & 0xFF;
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = 5 - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }

                int index = (buffer >> (bitsLeft - 5)) & 0x1F;
                bitsLeft -= 5;
                result.Append(base32Chars[index]);
            }

            return result.ToString();
        }

        private static byte[] Base32Decode(string encoded)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            encoded = encoded.TrimEnd('=').ToUpperInvariant();
            
            var result = new List<byte>();
            int buffer = 0;
            int bitsLeft = 0;

            foreach (char c in encoded)
            {
                int value = base32Chars.IndexOf(c);
                if (value < 0)
                {
                    throw new ArgumentException("Invalid Base32 character");
                }

                buffer <<= 5;
                buffer |= value & 0x1F;
                bitsLeft += 5;

                if (bitsLeft >= 8)
                {
                    result.Add((byte)(buffer >> (bitsLeft - 8)));
                    bitsLeft -= 8;
                }
            }

            return result.ToArray();
        }
    }
}
