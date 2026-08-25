using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAPI.Services;

namespace PharmacyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        // ============================================
        // GET CLIENT BY PHONE
        // GET: api/clients/by-phone/01012345678
        // ============================================

        [HttpGet("by-phone/{phone}")]
        [AllowAnonymous]

        public async Task<IActionResult> GetByPhone(string phone)
        {
            var client =
                await _clientService.GetByPhone(phone);

            if (client == null)
            {
                return NotFound(new
                {
                    message = "Client not found."
                });
            }

            return Ok(client);
        }
    }
}