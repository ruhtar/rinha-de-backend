using ProjeteMais.Shared;
using RinhaDeBackend.Dtos;

namespace RinhaDeBackend.Services
{
    public interface ITransacaoService
    {
        Task<OperationResult<ResponseTransacaoDto>> EfetuarTransacaoAsync(int id, RequestTransacaoDto transacaoDto);
        Task<OperationResult<ResponseExtratoDto>> GetExtrato(int id);
    }
}