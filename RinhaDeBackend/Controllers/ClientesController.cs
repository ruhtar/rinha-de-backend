﻿using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ProjeteMais.Shared;
using RinhaDeBackend.Cache;
using RinhaDeBackend.Dtos;
using RinhaDeBackend.Entities;
using System.Data;

namespace RinhaDeBackend.Controllers
{
    [ApiController]
    public class ClientesController : ControllerBase
    {
        [HttpGet("clientes/{id:int}/extrato")]
        public async Task<IActionResult> GetExtratoAsync([FromRoute] int id)
        {
            try {
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
            try {
                if (id > 5 || id <= 0) //fui mlk aqui
                {
                    return NotFound("Usuário não encontrado");
                }

                if (transacaoDto == null || transacaoDto.Valor <= 0 || (transacaoDto.Tipo != 'c' && transacaoDto.Tipo != 'd') || (transacaoDto.Descricao.Length < 1 || transacaoDto.Descricao.Length > 10))
                {
                    return UnprocessableEntity();
                }

                var result = await EfetuarTransacaoAsync(id, transacaoDto);
                if (!result.IsSuccess)
                {
                    return UnprocessableEntity();
                }
                var response = result.Data;

                return Ok(new
                {
                    limite = ClientesCache.ObterLimiteCliente(id), //TODO: ALTERAR
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
                    int limiteCliente = ClientesCache.ObterLimiteCliente(id); //TODO: ALTERAR

                    await conn.OpenAsync();

                    var saldo = await ObterSaldo(id, conn);
                    var transacoes = await ObterTransacoes(id, conn);

                    await conn.CloseAsync();

                    var response = new ResponseExtratoDto
                    {
                        Saldo = new SaldoInfo
                        {
                            Total = saldo,
                            data_extrato = DateTime.UtcNow,
                            Limite = limiteCliente,
                        },
                        ultimas_transacoes = transacoes,
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
                    var limiteCliente = ClientesCache.ObterLimiteCliente(id); //TODO: ALTERAR

                    await conn.OpenAsync();

                    var result = await conn.QuerySingleAsync<(bool success, int? new_saldo)>(
                    "SELECT * FROM atualizar_saldo_transacao(@ClientId, @TransactionValue, @TransactionType, @DescriptionType)",
                    new { ClientId = id, TransactionValue = transacaoDto.Valor, TransactionType = transacaoDto.Tipo, DescriptionType = transacaoDto.Descricao });

                    await conn.CloseAsync();

                    var success = result.success;
                    var novoSaldo = result.new_saldo;

                    if (!success || novoSaldo == null)
                    {
                        return new OperationResult<ResponseTransacaoDto>(false, "Passou o limite", null, 422);
                    }

                    var transacao = new Transacao
                    {
                        Cliente_Id = id,
                        Valor = transacaoDto.Valor,
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

        private async Task AtualizarSaldoAsync(int clienteId, int novoSaldo, IDbConnection connection)
        {
            var query = "UPDATE saldos SET valor = @novoSaldo WHERE cliente_id = @clienteId";

            await connection.ExecuteAsync(query, new { novoSaldo, clienteId });
        }

        private async Task InserirTransacaoAsync(Transacao transacao, IDbConnection connection)
        {
            var query = "INSERT INTO transacoes (cliente_id, valor, tipo, descricao, realizada_em) " +
                        "VALUES (@Cliente_Id, @Valor, @Tipo, @Descricao, @Realizada_Em)";

            await connection.ExecuteAsync(query, transacao);
        }

        private async Task<int> ObterSaldo(int clienteId, IDbConnection connection)
        {
            var query = "SELECT valor FROM saldos WHERE cliente_id = @clienteId FOR UPDATE"; //FOR UPDATE
            var result = await connection.ExecuteScalarAsync<int>(query, new { clienteId });

            return result;
        }

        private async Task<List<UltimasTransacoes>> ObterTransacoes(int clienteId, IDbConnection connection)
        {
            var query = "SELECT valor, tipo, descricao, realizada_em " +
                        "FROM transacoes WHERE cliente_id = @clienteId ORDER BY Realizada_Em DESC LIMIT 10";

            var transacoes = await connection.QueryAsync<UltimasTransacoes>(query, new { clienteId });

            return transacoes.AsList();
        }
    }
}
