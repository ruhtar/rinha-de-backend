using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RinhaDeBackend.Dtos;
using RinhaDeBackend.Services;

namespace RinhaDeBackend.Controllers
{
    [ApiController]
    public class ClientesController : ControllerBase
    {

        private readonly ITransacaoService _transacaoService;

        public ClientesController(ITransacaoService transacaoService)
        {
            _transacaoService = transacaoService;
        }

        [HttpGet("clientes/{id}/extrato")]
        public async Task<IActionResult> GetExtratoAsync([FromRoute] int id)
        {
            var result = await _transacaoService.GetExtrato(id);
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            return result.GetResponseWithStatusCode(); //ALTERAR ISSO PARA O FORMATO DA API CORRETO
        }

        [HttpPost("clientes/{id}/transacoes")]
        public async Task<IActionResult> FazerTransacaoAsync([FromRoute] int id, [FromBody] RequestTransacaoDto transacaoDto)
        {
            {
                if (id > 5) //fui mlk aqui
                {
                    return NotFound("Usuário não encontrado");
                }

                if (transacaoDto == null)
                {
                    return BadRequest("Os dados da transação não foram fornecidos.");
                }

                if (transacaoDto.Valor <= 0)
                {
                    return BadRequest("O valor da transação deve ser um número inteiro positivo.");
                }

                if (transacaoDto.Tipo != 'c' && transacaoDto.Tipo != 'd')
                {
                    return BadRequest("O tipo de transação deve ser 'c' para crédito ou 'd' para débito.");
                }

                if (string.IsNullOrEmpty(transacaoDto.Descricao) || transacaoDto.Descricao.Length < 1 || transacaoDto.Descricao.Length > 10)
                {
                    return BadRequest("A descrição da transação deve ter entre 1 e 10 caracteres.");
                }

                // TODO: Lógica de processamento da transação
                var result = await _transacaoService.EfetuarTransacaoAsync(id, transacaoDto);
                //if (result.HttpStatusCode == 404) return NotFound("Usuário não encontrado");
                if (!result.IsSuccess) {
                    return UnprocessableEntity();
                }
                return Ok(result.Data);
            }
        }
    }
}
