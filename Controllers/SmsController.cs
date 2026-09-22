using Microsoft.AspNetCore.Mvc;
using PharmacyAPI.Models.RequestsModels;
using PharmacyAPI.Services;

namespace PharmacyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SmsController : Controller
    {
        private readonly SmsIntegrationService _smsService;

        public SmsController(SmsIntegrationService smsService)
        {
            _smsService = smsService;
        }
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp(SmsRequest request)
        {
            try
            {
                var result = _smsService.VerifyOtp(
                    request.PhoneNumber,
                    request.Otp);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // ============================================================
        // SEND OTP
        // ============================================================

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp(
            [FromBody] SmsRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _smsService.SendOtpAsync(
                    request.PhoneNumber,
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "OTP sent successfully.",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // ============================================================
        // SEND NORMAL MESSAGE
        // ============================================================

        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage(
            [FromBody] SmsRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _smsService.SendMessageAsync(
                    request.PhoneNumber,
                    "test",
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "SMS sent successfully.",
                    data = result
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        message = "SMS provider request failed.",
                        error = ex.Message
                    });
            }
        }
        // ============================================================
        // Sms total  cost
        // ============================================================

        [HttpGet("cost")]
        public async Task<IActionResult> GetTotalCost(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _smsService.getTotalCostAsync( cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "SMS sent successfully.",
                    data = result
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        success = false,
                        message = "SMS provider request failed.",
                        error = ex.Message
                    });
            }
        }

    }
}
