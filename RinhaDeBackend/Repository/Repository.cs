using Microsoft.EntityFrameworkCore;
using RinhaDeBackend.Data;
using RinhaDeBackend.Entities;

namespace RinhaDeBackend.Infra
{
    public class Repository
    {
        private readonly AppDbContext _context;

        public Repository(AppDbContext appDbContext)
        {
            _context = appDbContext;
        }

        public async Task<Cliente> GetClienteByIdAsync(int id) 
        {
            var result = await _context.Clientes.FirstOrDefaultAsync(x => x.Id == id);
            return result;
        }

    }
}
