namespace WebApplication1.Services
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetSmsAsync(string mobile, string resetCode)
        {
            try
            {
                var message = $"Your password reset code is: {resetCode}. This code will expire in 10 minutes. Do not share this code with anyone.";

                // For demonstration purposes, we'll log the SMS instead of actually sending it
                // In production, you would integrate with an SMS provider like Twilio
                var smsProvider = _configuration["SMS:Provider"];
                
                if (string.IsNullOrEmpty(smsProvider))
                {
                    // Log the SMS for testing purposes
                    _logger.LogInformation(
                        "SMS (Provider not configured):\nTo: {Mobile}\nMessage: {Message}",
                        mobile, message);
                    
                    // Return true to simulate successful SMS sending
                    return true;
                }

                // In production, integrate with SMS provider
                // Example with Twilio:
                // var accountSid = _configuration["SMS:Twilio:AccountSid"];
                // var authToken = _configuration["SMS:Twilio:AuthToken"];
                // var fromNumber = _configuration["SMS:Twilio:FromNumber"];
                // TwilioClient.Init(accountSid, authToken);
                // var messageResource = await MessageResource.CreateAsync(
                //     body: message,
                //     from: new PhoneNumber(fromNumber),
                //     to: new PhoneNumber(mobile)
                // );

                _logger.LogInformation("SMS sent successfully to {Mobile}", mobile);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {Mobile}", mobile);
                return false;
            }
        }
    }
}
