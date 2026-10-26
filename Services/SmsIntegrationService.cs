using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PharmacyAPI.Models.RequestsModels;

namespace PharmacyAPI.Services
{
    public class SmsIntegrationService
    {
        private const int OtpLength = 6;
        private const int MaxAttempts = 5;
        private const string DefaultErrorEmail = "ereny.karam.noshy@gmail.com";

        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

        // When the user asks for a resend and the current code still has at least
        // this much time left, the SAME code is sent again instead of a new one.
        // This way an older SMS that arrives late still contains a valid code.
        private static readonly TimeSpan MinRemainingToReuse = TimeSpan.FromMinutes(2);

        private const string CostCacheKey = "sms-cost-total";
        private static readonly TimeSpan CostCacheLifetime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CostRequestTimeout = TimeSpan.FromSeconds(15);

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        // Must be static: AddHttpClient<T> registers this service as transient,
        // so every request gets a new instance, but they all share one IMemoryCache.
        // The lock is only held for a few microseconds (no I/O inside it).
        private static readonly object OtpLock = new();

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;
        private readonly ILogger<SmsIntegrationService> _logger;
        private readonly string _smsApiUrl;
        private readonly string _smsSenderId;
        private readonly string _smsSenderName;
        private readonly string _defaultCountryCode;
        private readonly string _errorEmail;

        public SmsIntegrationService(
            HttpClient httpClient,
            IConfiguration configuration,
            IMemoryCache cache,
            IEmailService emailService,
            ILogger<SmsIntegrationService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _emailService = emailService;
            _logger = logger;

            var link = configuration["SmsIntegration:Link"]
                ?? throw new InvalidOperationException(
                    "SmsIntegration:Link is not configured.");

            // Guarantees exactly one "/" between the base URL and the endpoint path.
            _smsApiUrl = link.TrimEnd('/') + "/";

            _smsSenderId = configuration["SmsIntegration:senderId"]
                ?? throw new InvalidOperationException(
                    "SmsIntegration:senderId is not configured.");

            _smsSenderName = configuration["SmsIntegration:senderName"]
                ?? throw new InvalidOperationException(
                    "SmsIntegration:senderName is not configured.");

            // Optional settings with safe defaults.
            _defaultCountryCode = configuration["SmsIntegration:DefaultCountryCode"] ?? "20";
            _errorEmail = configuration["SmsIntegration:ErrorEmail"] ?? DefaultErrorEmail;
        }

        // ------------------------------------------------------------------
        // OTP
        // ------------------------------------------------------------------

        public async Task<SmsResponse> SendOtpAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default)
        {
            var normalizedPhone = NormalizePhone(phoneNumber);

            if (string.IsNullOrEmpty(normalizedPhone))
            {
                throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
            }

            var otpKey = OtpKey(normalizedPhone);
            var cooldownKey = CooldownKey(normalizedPhone);

            string otp;

            // 1) Reserve the cooldown and store the OTP BEFORE calling the SMS provider.
            //    Previously the OTP was cached only after the provider answered, so if the
            //    provider was slow, timed out, returned an error after sending, or the client
            //    disconnected, the SMS arrived but the code was never saved
            //    -> "OTP is invalid or expired."
            //    Doing check + set inside a lock also stops a double-tap from generating
            //    two different codes at the same time.
            lock (OtpLock)
            {
                if (_cache.TryGetValue(cooldownKey, out _))
                {
                    throw new InvalidOperationException(
                        "Please wait before requesting another OTP.");
                }

                var now = DateTimeOffset.UtcNow;

                if (_cache.TryGetValue(otpKey, out OtpEntry? existing)
                    && existing is not null
                    && existing.Attempts < MaxAttempts
                    && existing.ExpiresAt - now >= MinRemainingToReuse)
                {
                    // Resend the same code; keep its expiry and attempt count
                    // so resending can't be used to reset the brute-force limit.
                    otp = existing.Code;
                }
                else
                {
                    var entry = new OtpEntry(GenerateOtp(), now.Add(OtpLifetime));
                    _cache.Set(otpKey, entry, entry.ExpiresAt);
                    otp = entry.Code;
                }

                _cache.Set(cooldownKey, true, ResendCooldown);
            }

            // 2) Send the SMS (outside the lock, since it is slow network I/O).
            try
            {
                var request = new
                {
                    // The original number is sent to the provider unchanged,
                    // so the provider receives exactly the format it got before.
                    phoneNumber,
                    message = $"Avera\nOTP: رمز التحقق {otp}",
                    senderId = _smsSenderId,
                    senderName = _smsSenderName
                };

                using var response = await _httpClient.PostAsJsonAsync(
                    _smsApiUrl + "api/sms/send",
                    request,
                    cancellationToken);

                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "SMS provider returned {StatusCode} while sending OTP: {Response}",
                        (int)response.StatusCode,
                        json);

                    response.EnsureSuccessStatusCode();
                }

                _logger.LogInformation("SMS API response (OTP): {Response}", json);

                var smsResponse = JsonSerializer.Deserialize<SmsResponse>(json, JsonOptions);

                if (smsResponse == null)
                {
                    throw new InvalidOperationException(
                        "Invalid response received from SMS provider.");
                }

                return smsResponse;
            }
            catch
            {
                // Let the user retry immediately.
                // The OTP itself is kept on purpose: the SMS may have been delivered even
                // though we didn't get a proper response, and the retry will resend the
                // same code (see the reuse rule above), so whichever SMS arrives works.
                _cache.Remove(cooldownKey);
                throw;
            }
        }

        public OtpVerificationResult VerifyOtp(
            string phoneNumber,
            string enteredOtp)
        {
            try
            {
                var normalizedPhone = NormalizePhone(phoneNumber);
                var otpKey = OtpKey(normalizedPhone);
                var code = NormalizeOtp(enteredOtp);

                // The lock makes "read attempts -> compare -> increment" atomic, so parallel
                // requests can't bypass the 5-attempt limit or corrupt the counter.
                lock (OtpLock)
                {
                    if (!_cache.TryGetValue(otpKey, out OtpEntry? entry)
                        || entry is null
                        || entry.ExpiresAt <= DateTimeOffset.UtcNow)
                    {
                        _logger.LogInformation(
                            "OTP not found or expired for key {Key} (process {ProcessId}).",
                            otpKey,
                            Environment.ProcessId);

                        return Fail("OTP is invalid or expired.", 0);
                    }

                    if (entry.Attempts >= MaxAttempts)
                    {
                        _cache.Remove(otpKey);
                        return Fail("Too many incorrect attempts. Please request a new OTP.", 0);
                    }

                    if (code.Length == OtpLength && FixedTimeEquals(entry.Code, code))
                    {
                        _cache.Remove(otpKey);
                        _cache.Remove(CooldownKey(normalizedPhone));

                        return new OtpVerificationResult
                        {
                            Success = true,
                            Message = "OTP verified successfully.",
                            AttemptsRemaining = MaxAttempts - entry.Attempts
                        };
                    }

                    // IMemoryCache stores the object by reference,
                    // so updating it here updates the cached entry directly.
                    entry.Attempts++;

                    var remaining = MaxAttempts - entry.Attempts;

                    if (remaining <= 0)
                    {
                        _cache.Remove(otpKey);
                        return Fail("Too many incorrect attempts. Please request a new OTP.", 0);
                    }

                    return Fail("Incorrect OTP.", remaining);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while verifying OTP.");

                // Fire-and-forget, but safely: any failure is logged instead of lost.
                _ = NotifyErrorAsync("otp error", ex.ToString());

                return Fail("An error occurred while verifying the OTP.", 0);
            }
        }

        // ------------------------------------------------------------------
        // Other SMS operations
        // ------------------------------------------------------------------

        public async Task<SmsResponse> SendMessageAsync(
            string phoneNumber,
            string message,
            CancellationToken cancellationToken = default)
        {
            var request = new
            {
                phoneNumber,
                message,
                senderId = _smsSenderId,
                senderName = _smsSenderName
            };

            using var response = await _httpClient.PostAsJsonAsync(
                _smsApiUrl + "api/sms/send",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var smsResponse = await response.Content
                .ReadFromJsonAsync<SmsResponse>(JsonOptions, cancellationToken);

            if (smsResponse == null)
            {
                throw new InvalidOperationException(
                    "Invalid response received from SMS provider.");
            }

            return smsResponse;
        }

        public async Task<SmsCostResponse> getTotalCostAsync(
            CancellationToken cancellationToken = default)
        {
            // The provider endpoint can be slow, so the result is cached briefly:
            // dashboard visits don't need a real-time total.
            if (_cache.TryGetValue(CostCacheKey, out SmsCostResponse? cached)
                && cached is not null)
            {
                return cached;
            }

            var address = _smsApiUrl
                + "api/sms/sender-names/"
                + Uri.EscapeDataString(_smsSenderName)
                + "/sms-part-count-total";

            // Fail fast instead of waiting on the HttpClient default (100s),
            // which is longer than the gateway timeout and ends in a 504.
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(CostRequestTimeout);

            try
            {
                using var response = await _httpClient.GetAsync(address, timeoutCts.Token);

                response.EnsureSuccessStatusCode();

                var costResponse = await response.Content
                    .ReadFromJsonAsync<SmsCostResponse>(JsonOptions, timeoutCts.Token);

                if (costResponse == null)
                {
                    throw new InvalidOperationException(
                        "Invalid response received from SMS provider.");
                }

                _cache.Set(CostCacheKey, costResponse, CostCacheLifetime);

                return costResponse;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Our timeout fired (not the client disconnecting).
                _logger.LogWarning(
                    "SMS provider did not answer the total cost request within {Timeout}s.",
                    CostRequestTimeout.TotalSeconds);

                throw new TimeoutException(
                    "SMS provider did not respond in time.");
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static string OtpKey(string normalizedPhone) => $"otp:{normalizedPhone}";

        private static string CooldownKey(string normalizedPhone) => $"otp-cooldown:{normalizedPhone}";

        private static string GenerateOtp() =>
            RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Turns "+20 100 123 4567", "00201001234567", "01001234567" and
        /// "٠١٠٠١٢٣٤٥٦٧" into the same value "201001234567", so the cache key
        /// is identical no matter how the number was typed.
        /// </summary>
        private string NormalizePhone(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(input.Length);

            foreach (var c in input)
            {
                if (char.IsDigit(c))
                {
                    var value = (int)char.GetNumericValue(c);

                    if (value is >= 0 and <= 9)
                    {
                        sb.Append((char)('0' + value));
                    }
                }
                // '+', spaces, dashes, brackets, etc. are ignored.
            }

            var digits = sb.ToString();

            if (digits.StartsWith("00", StringComparison.Ordinal))
            {
                digits = digits[2..];
            }
            else if (digits.StartsWith('0'))
            {
                digits = _defaultCountryCode + digits[1..];
            }

            return digits;
        }

        /// <summary>
        /// Converts Arabic-Indic / Persian digits to 0-9 and ignores spaces and dashes.
        /// Any other character makes the code invalid.
        /// </summary>
        private static string NormalizeOtp(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(OtpLength);

            foreach (var c in input)
            {
                if (char.IsWhiteSpace(c) || c == '-')
                {
                    continue;
                }

                if (!char.IsDigit(c))
                {
                    return string.Empty;
                }

                var value = (int)char.GetNumericValue(c);

                if (value is < 0 or > 9)
                {
                    return string.Empty;
                }

                sb.Append((char)('0' + value));
            }

            return sb.ToString();
        }

        // Constant-time comparison so response timing can't leak how many digits matched.
        private static bool FixedTimeEquals(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(a),
                Encoding.ASCII.GetBytes(b));

        private static OtpVerificationResult Fail(string message, int attemptsRemaining) =>
            new()
            {
                Success = false,
                Message = message,
                AttemptsRemaining = attemptsRemaining
            };

        private async Task NotifyErrorAsync(string subject, string body)
        {
            try
            {
                await _emailService.SendEmailAsync(_errorEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send error notification email.");
            }
        }

        private sealed class OtpEntry
        {
            public OtpEntry(string code, DateTimeOffset expiresAt)
            {
                Code = code;
                ExpiresAt = expiresAt;
            }

            public string Code { get; }

            public DateTimeOffset ExpiresAt { get; }

            public int Attempts { get; set; }
        }
    }
}