using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;

namespace PharmacyAPI.Services
{
    public interface IClientService
    {
        Task<Client?> GetByPhone(string phone);
    }
    public class ClientService : IClientService
    {
        private readonly PharmacyDbContext _context;

        public ClientService(PharmacyDbContext context)
        {
            _context = context;
        }

        public async Task<Client?> GetByPhone(string phone)
        {
            return await _context.Clients
                .FirstOrDefaultAsync(x =>
                    x.PhoneNumber == phone);
        }
    }
}
