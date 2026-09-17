namespace PharmacyAPI.Models.RequestsModels
{
    public class SmsResponse
    {
        public int id { get; set; }

        public string senderId { get; set; } = string.Empty;

        public string phoneNumber { get; set; } = string.Empty;

        public string message { get; set; } = string.Empty;

        public string encoding { get; set; } = string.Empty;

        public int smsPartCount { get; set; } 

        public string status { get; set; } = string.Empty;

        public string createdAt { get; set; } = string.Empty;
    }
}