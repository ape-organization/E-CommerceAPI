namespace PharmacyAPI.Models.RequestsModels
{
    public class OtpVerificationResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int AttemptsRemaining { get; set; }
    }
}
