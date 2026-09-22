using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using PharmacyAPI.Models.RequestsModels;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace PharmacyAPI.Services
{
    public class SmsIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly string _smsApiUrl;
        private readonly string _smsSenderId;
        private readonly string _smsSenderName;
        public SmsIntegrationService(
            HttpClient httpClient,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;

            _smsApiUrl = _configuration["SmsIntegration:Link"]
                ?? throw new InvalidOperationException(
                    "SmsIntegration:Link is not configured.");
            _smsSenderId = _configuration["SmsIntegration:senderId"]
                ?? throw new InvalidOperationException(
                    "SmsIntegration:send id is not configured.");
            _smsSenderName = _configuration["SmsIntegration:senderName"]
                ?? throw new InvalidOperationException(
                    "SmsIntegration:senderName is not configured.");
        }

        public async Task<SmsResponse> SendOtpAsync(
      string phoneNumber,
      CancellationToken cancellationToken = default)
        {
            var cooldownKey = $"otp-cooldown:{phoneNumber}";
            var otpKey = $"otp:{phoneNumber}";
            var attemptsKey = $"otp-attempts:{phoneNumber}";

            if (_cache.TryGetValue(cooldownKey, out _))
            {
                throw new InvalidOperationException(
                    "Please wait before requesting another OTP.");
            }

            var otp = RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString();

            var request = new
            {
                phoneNumber,
                message = $"Pinky Aura\nOTP: رمز التحقق {otp}",
            senderId = _smsSenderId,
                senderName = _smsSenderName
            };

            var response = await _httpClient.PostAsJsonAsync(
                _smsApiUrl+ "api/sms/send",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            Console.WriteLine("SMS API RESPONSE:");
            Console.WriteLine(json);

            var smsResponse = JsonSerializer.Deserialize<SmsResponse>(json);

            if (smsResponse == null)
            {
                throw new InvalidOperationException(
                    "Invalid response received from SMS provider.");
            }

            // OTP is valid for 5 minutes
            _cache.Set(
                otpKey,
                otp,
                TimeSpan.FromMinutes(5));

            // Reset attempts
            _cache.Set(
                attemptsKey,
                0,
                TimeSpan.FromMinutes(5));

            // Resend cooldown
            _cache.Set(
                cooldownKey,
                true,
                TimeSpan.FromSeconds(60));

            return smsResponse;
        }

        public async Task<SmsResponse> SendMessageAsync(
            string phoneNumber,
            string message,
            CancellationToken cancellationToken = default)
        {
            var request = new
            {
                phoneNumber = phoneNumber,
                message = message,
                senderId = _smsSenderId,
                senderName = _smsSenderName
            };

            var response = await _httpClient.PostAsJsonAsync(
                _smsApiUrl + "api/sms/send",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var smsResponse = await response.Content
                .ReadFromJsonAsync<SmsResponse>(
                    cancellationToken);

            if (smsResponse == null)
            {
                throw new InvalidOperationException(
                    "Invalid response received from SMS provider.");
            }

            return smsResponse;
        }


        public OtpVerificationResult VerifyOtp(
           string phoneNumber,
           string enteredOtp)
        {
            var otpKey = $"otp:{phoneNumber}";
            var attemptsKey = $"otp-attempts:{phoneNumber}";

            if (!_cache.TryGetValue<string>(
                    otpKey,
                    out var storedOtp))
            {
                return new OtpVerificationResult
                {
                    Success = false,
                    Message = "OTP is invalid or expired.",
                    AttemptsRemaining = 0
                };
            }

            var attempts = _cache.Get<int>(attemptsKey);

            if (attempts >= 5)
            {
                _cache.Remove(otpKey);
                _cache.Remove(attemptsKey);

                return new OtpVerificationResult
                {
                    Success = false,
                    Message = "Too many incorrect attempts. Please request a new OTP.",
                    AttemptsRemaining = 0
                };
            }

            if (string.Equals(
                    storedOtp,
                    enteredOtp,
                    StringComparison.Ordinal))
            {
                _cache.Remove(otpKey);
                _cache.Remove(attemptsKey);

                return new OtpVerificationResult
                {
                    Success = true,
                    Message = "OTP verified successfully.",
                    AttemptsRemaining = 5 - attempts
                };
            }

            attempts++;

            if (attempts >= 5)
            {
                _cache.Remove(otpKey);
                _cache.Remove(attemptsKey);

                return new OtpVerificationResult
                {
                    Success = false,
                    Message = "Too many incorrect attempts. Please request a new OTP.",
                    AttemptsRemaining = 0
                };
            }

            _cache.Set(
                attemptsKey,
                attempts,
                TimeSpan.FromMinutes(5));

            return new OtpVerificationResult
            {
                Success = false,
                Message = "Incorrect OTP.",
                AttemptsRemaining = 5 - attempts
            };
        }

        public async Task<SmsCostResponse> getTotalCostAsync( CancellationToken cancellationToken = default)
        {

            var address = _smsApiUrl + "api/sms/sender-names/" + _smsSenderName + "/sms-part-count-total";
            var response = await _httpClient.GetAsync(address,
                
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var smsResponse = await response.Content
                .ReadFromJsonAsync<SmsCostResponse>(
                    cancellationToken);

            if (smsResponse == null)
            {
                throw new InvalidOperationException(
                    "Invalid response received from SMS provider.");
            }

            return smsResponse;
        }



    }
}