using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ProjeteMais.Shared;
using RinhaDeBackend.Cache;
using RinhaDeBackend.Dtos;
using RinhaDeBackend.Entities;
using System.Data;
using System.Text.Json;

namespace RinhaDeBackend.Controllers
{
    [ApiController]
    public class ClientesController : ControllerBase
    {
        [HttpGet("clientes/{id:int}/extrato")]
        public async Task<IActionResult> GetExtratoAsync([FromRoute] int id)
        {
            try
            {
                if (id > 5 || id <= 0) //fui mlk aqui
                {
                    return NotFound("Usuário não encontrado");
                }
                var result = await GetExtrato(id);
                if (result.IsSuccess)
                {
                    return Ok(result.Data);
                }
                return UnprocessableEntity();
            }
            catch (Exception ex)
            {
                var result = new OperationResult<ResponseExtratoDto>(false, $"Erro: {ex.Message}", null, 500);
                return result.GetResponseWithStatusCode();
            }
        }

        [HttpPost("clientes/{id:int}/transacoes")]
        public async Task<IActionResult> FazerTransacaoAsync([FromRoute] int id, [FromBody] RequestTransacaoDto transacaoDto)
        {
            try
            {
                if (id > 5 || id <= 0) //fui mlk aqui
                {
                    return NotFound("Usuário não encontrado");
                }

                if ((int)transacaoDto.Valor != transacaoDto.Valor) {
                    return UnprocessableEntity("O campo 'Valor' deve ser um número inteiro.");
                }

                if (string.IsNullOrWhiteSpace(transacaoDto.Descricao)) {
                    return UnprocessableEntity("Campo descrição vazio");
                }

                if (transacaoDto == null || (int)transacaoDto.Valor <= 0 || (transacaoDto.Tipo != 'c' && transacaoDto.Tipo != 'd') || transacaoDto.Descricao.Length < 0 || transacaoDto.Descricao.Length > 10)
                {
                    return UnprocessableEntity("Erro na validação");
                }

                var result = await EfetuarTransacaoAsync(id, transacaoDto);
                if (!result.IsSuccess)
                {
                    return UnprocessableEntity();
                }
                var response = result.Data;

                return Ok(new
                {
                    limite = response.Limite, 
                    saldo = response!.Saldo,
                });
            }
            catch (Exception ex)
            {
                var result = new OperationResult<ResponseExtratoDto>(false, $"Erro: {ex.Message}", null, 500);
                return result.GetResponseWithStatusCode();
            }
        }

        private async Task<OperationResult<ResponseExtratoDto>> GetExtrato(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(Utils.ConnectionString);
                {
                    var limiteCliente = ClientesCache.GetLimiteCliente(id);
                    if (limiteCliente == 0) {
                        var limite = await ConsultarLimite(id, conn);
                        ClientesCache.SetLimiteCliente(id, limite);
                        limiteCliente = ClientesCache.GetLimiteCliente(id);
                    }

                    var results = await ObterSaldoETransacoes(id, conn);

                    var saldo = results.saldo;
                    var transacoes = results.ultimas_transacoes;

                    var response = new ResponseExtratoDto
                    {
                        Saldo = new SaldoInfo
                        {
                            Total = saldo,
                            data_extrato = DateTime.UtcNow,
                            Limite = limiteCliente,
                        },
                        ultimas_transacoes = transacoes ?? new List<UltimasTransacoes>(),
                    };

                    var result = new OperationResult<ResponseExtratoDto>(true, "Sucesso", response, 200);
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new OperationResult<ResponseExtratoDto>(false, $"Erro: {ex.Message}", null, 500);
            }
        }

        private async Task<OperationResult<ResponseTransacaoDto>> EfetuarTransacaoAsync(int id, RequestTransacaoDto transacaoDto)
        {
            try
            {
                using var conn = new NpgsqlConnection(Utils.ConnectionString);
                {
                    var limiteCliente = ClientesCache.GetLimiteCliente(id);
                    if (limiteCliente == 0)
                    {
                        var limite = await ConsultarLimite(id, conn);
                        ClientesCache.SetLimiteCliente(id, limite);
                        limiteCliente = ClientesCache.GetLimiteCliente(id);
                    }

                    var result = await conn.QuerySingleAsync<(bool success, int? new_saldo)>(
                    "SELECT * FROM atualizar_saldo_transacao(@ClientId, @TransactionValue, @TransactionType, @DescriptionType)",
                    new { ClientId = id, TransactionValue = (int)transacaoDto.Valor, TransactionType = transacaoDto.Tipo, DescriptionType = transacaoDto.Descricao });

                    var success = result.success;
                    var novoSaldo = result.new_saldo;

                    if (!success || novoSaldo == null)
                    {
                        return new OperationResult<ResponseTransacaoDto>(false, "Passou o limite", null, 422);
                    }

                    var transacao = new Transacao
                    {
                        Cliente_Id = id,
                        Valor = (int)transacaoDto.Valor,
                        Tipo = transacaoDto.Tipo,
                        Descricao = transacaoDto.Descricao,
                        Realizada_Em = DateTime.UtcNow
                    };

                    var response = new ResponseTransacaoDto
                    {
                        Limite = limiteCliente,
                        Saldo = (int)novoSaldo
                    };

                    return new OperationResult<ResponseTransacaoDto>(true, "Sucesso", response, 200);
                }
            }
            catch (Exception ex)
            {
                return new OperationResult<ResponseTransacaoDto>(false, $"Erro: {ex.Message}", null, 500);
            }
        }

        private async Task<int> ConsultarLimite(int clienteId, IDbConnection connection) {
            var query = "SELECT limite FROM clientes WHERE id = @clienteId";

            return await connection.ExecuteScalarAsync<int>(query, new { clienteId });
        }

        private static async Task<(int saldo, List<UltimasTransacoes> ultimas_transacoes)> ObterSaldoETransacoes(int clienteId, IDbConnection connection)
        {
            var query = "SELECT * FROM ObterSaldoETransacoes(@clienteId)";
            var parameters = new { clienteId };
            var result = await connection.QueryFirstOrDefaultAsync<(int saldo, string ultimas_transacoes)>(query, parameters);
            if (result.ultimas_transacoes == null) {
                return (result.saldo, new List<UltimasTransacoes>());
            }
            var ultimasTransacoesList = JsonSerializer.Deserialize<List<UltimasTransacoes>>(result.ultimas_transacoes);

            return (result.saldo, ultimasTransacoesList);
        }
    }
}
