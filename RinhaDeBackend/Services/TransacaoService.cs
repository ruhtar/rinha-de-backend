using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProjeteMais.Shared;
using RinhaDeBackend.Data;
using RinhaDeBackend.Dtos;
using RinhaDeBackend.Entities;

namespace RinhaDeBackend.Services
{
    public class TransacaoService : ITransacaoService
    {
        private readonly AppDbContext _context;

        public TransacaoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OperationResult<ResponseExtratoDto>> GetExtrato(int id)
        {
            var cliente = await _context.Clientes.Include(x => x.Transacoes).FirstOrDefaultAsync(x => x.Id == id);
            if (cliente == null)
            {
                var operationResult = new OperationResult<ResponseExtratoDto>(false, "Cliente não encontrado", null, 404);
                return operationResult;
            }
            var response = new ResponseExtratoDto()
            {
                Saldo = new Saldo
                {
                    Total = cliente.Saldo,
                    data_extrato = DateTime.UtcNow,
                    Limite = cliente.Limite,
                },
                ultimas_transacoes = cliente.Transacoes.Select(t => new UltimasTransacoes()
                {
                    Valor = t.Valor,
                    Tipo = t.Tipo,
                    Descricao = t.Descricao,
                    realizada_em = t.RealizadaEm
                }).ToList(),
            };
            var result = new OperationResult<ResponseExtratoDto>(true, "Sucesso", response, 200);
            return result;
        }

        public async Task<OperationResult<ResponseTransacaoDto>> EfetuarTransacaoAsync(int id, RequestTransacaoDto transacaoDto)
        {
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id); //Add AsNoTracking?

            if (cliente == null)
            {
                var operationResult = new OperationResult<ResponseTransacaoDto>(false, "Cliente não encontrado", null, 404);
                return operationResult;
            }

            if (transacaoDto.Tipo == 'd')
            {
                cliente.Saldo -= transacaoDto.Valor;
                if (cliente.Saldo * -1 > cliente.Limite)
                {
                    //dado inconsistente, tratar
                    //retornar um 422 sem corpo e sem completar a transacao
                    return new OperationResult<ResponseTransacaoDto>(false, "Passou o limite", null, 422);
                }
            }

            var transacao = new Transacao
            {
                ClienteId = id,
                Valor = transacaoDto.Valor,
                Tipo = transacaoDto.Tipo,
                Descricao = transacaoDto.Descricao,
                RealizadaEm = DateTime.UtcNow
            };

            await _context.Transacoes.AddAsync(transacao);

            var response = new ResponseTransacaoDto
            {
                Limite = cliente.Limite,
                Saldo = cliente.Saldo
            };

            var result = new OperationResult<ResponseTransacaoDto>(true, "Sucesso", response, 200);
            await _context.SaveChangesAsync();
            return result;

        }
    }
}
