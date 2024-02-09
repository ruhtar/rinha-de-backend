﻿using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ProjeteMais.Shared;
using RinhaDeBackend.Cache;
using RinhaDeBackend.DbContext;
using RinhaDeBackend.Dtos;
using RinhaDeBackend.Entities;
using RinhaDeBackend.Services;
using System.Data;

namespace RinhaDeBackend.Controllers
{
    [ApiController]
    public class ClientesController : ControllerBase
    {
        [HttpGet("clientes/{id:int}/extrato")]
        public async Task<IActionResult> GetExtratoAsync([FromRoute] int id)
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
            return result.GetResponseWithStatusCode(); //ALTERAR ISSO PARA O FORMATO DA API CORRETO
        }

        [HttpPost("clientes/{id:int}/transacoes")]
        public async Task<ActionResult<ResponseTransacaoDto>> FazerTransacaoAsync([FromRoute] int id, [FromBody] RequestTransacaoDto transacaoDto)
        {
            {
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
                //if (response == null)
                //{
                //    return NotFound("Resposta não encontrada");
                //}

                return Ok(new
                {
                    limite = ClientesCache.ObterLimiteCliente(id),
                    saldo = response!.Saldo,
                });
            }
        }

        private async Task<OperationResult<ResponseExtratoDto>> GetExtrato(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(Utils.ConnectionString);
                {
                    await conn.OpenAsync();
                    int limiteCliente = ClientesCache.ObterLimiteCliente(id);
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
                    var limiteCliente = ClientesCache.ObterLimiteCliente(id);

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
                    //var saldoValor = await ObterSaldo(id, conn); //criar variavel nova e usar ao inves de saldoValor

                    //var novoSaldo = 0;

                    //if (transacaoDto.Tipo == 'c')
                    //{
                    //    novoSaldo = saldoValor + transacaoDto.Valor;
                    //}
                    //else
                    //{
                    //    novoSaldo = saldoValor - transacaoDto.Valor;
                    //}

                    //if ((limiteCliente + novoSaldo) < 0)
                    //{
                    //    await conn.CloseAsync();

                    //    return new OperationResult<ResponseTransacaoDto>(false, "Passou o limite", null, 422);
                    //}

                    var transacao = new Transacao
                    {
                        Cliente_Id = id,
                        Valor = transacaoDto.Valor,
                        Tipo = transacaoDto.Tipo,
                        Descricao = transacaoDto.Descricao,
                        Realizada_Em = DateTime.UtcNow
                    };
                    //var transaction = await conn.BeginTransactionAsync();

                    //await AtualizarSaldoAsync(id, novoSaldo, conn); 
                    //Posso tentar usar uma subquery aqui que consulta o saldo e tenta atualizar ele. Deve haver alguma regra no proprio banco pra nao deixar o saldo ser atualizado caso ele ultrapasse o limite.

                    //  criar uma FUNCTION que atualiza o saldo do cliente, verifica se (limiteCliente + novoSaldo) < 0. Se for true, ela dá rollback na transação de atualizar o dado.
                    // O saldo a ser atualizado do cliente segue a seguinte logica 
                    //if (transacaoDto.Tipo == 'c')
                    //{
                    //    novoSaldo = saldoValor + transacaoDto.Valor;
                    //}
                    //else
                    //{
                    //    novoSaldo = saldoValor - transacaoDto.Valor;
                    //}
                    //  A function recebe como parametro o valor da transacao e o tipo dela ( 'c', 'd')
                    //  Por fim ela insere a transacao

                    //await InserirTransacaoAsync(transacao, conn);

                    //await transaction.CommitAsync();
                    //await conn.CloseAsync();

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

        //private async Task AtualizarSaldoAsync(int clienteId, int novoSaldo, NpgsqlConnection connection)
        //{
        //    var query = "UPDATE saldos SET valor = @novoSaldo WHERE cliente_id = @clienteId";

        //    using var cmd = new NpgsqlCommand(query, connection);
        //    cmd.Parameters.AddWithValue("@novoSaldo", novoSaldo);
        //    cmd.Parameters.AddWithValue("@clienteId", clienteId);

        //    await cmd.ExecuteNonQueryAsync();
        //}

        //private async Task InserirTransacaoAsync(Transacao transacao, NpgsqlConnection connection)
        //{
        //    var query = "INSERT INTO transacoes (cliente_id, valor, tipo, descricao, realizada_em) VALUES (@clienteId, @valor, @tipo, @descricao, @realizadaEm)";

        //    using var cmd = new NpgsqlCommand(query, connection);
        //    cmd.Parameters.AddWithValue("@clienteId", transacao.Cliente_Id);
        //    cmd.Parameters.AddWithValue("@valor", transacao.Valor);
        //    cmd.Parameters.AddWithValue("@tipo", transacao.Tipo);
        //    cmd.Parameters.AddWithValue("@descricao", transacao.Descricao);
        //    cmd.Parameters.AddWithValue("@realizadaEm", transacao.Realizada_Em);

        //    await cmd.ExecuteNonQueryAsync();
        //}

        //private async Task<int> ObterSaldo(int clienteId, NpgsqlConnection connection)
        //{
        //    using var cmd = new NpgsqlCommand("SELECT valor FROM saldos WHERE cliente_id = @clienteId ", connection); //FOR UPDATE
        //    cmd.Parameters.AddWithValue("@clienteId", clienteId);
        //    var result = await cmd.ExecuteScalarAsync();

        //    if (result != null && result != DBNull.Value)
        //    {
        //        return Convert.ToInt32(result);
        //    }

        //    return 0;
        //}

        //private async Task<List<UltimasTransacoes>> ObterTransacoes(int clienteId, NpgsqlConnection connection)
        //{
        //    var query = "SELECT valor, tipo, descricao, realizada_em FROM transacoes WHERE cliente_id = @clienteId ORDER BY Realizada_Em desc LIMIT 10";
        //    using var cmd = new NpgsqlCommand(query, connection);
        //    cmd.Parameters.AddWithValue("@clienteId", clienteId);
        //    var reader = await cmd.ExecuteReaderAsync();

        //    var transacoes = new List<UltimasTransacoes>();

        //    while (await reader.ReadAsync())
        //    {
        //        var ultimaTransacao = new UltimasTransacoes
        //        {
        //            Valor = reader.GetInt32(0),
        //            Tipo = reader.GetChar(1),
        //            Descricao = reader.GetString(2),
        //            realizada_em = reader.GetDateTime(3)
        //        };

        //        transacoes.Add(ultimaTransacao);
        //    }

        //    reader.Close();

        //    return transacoes;
        //}
    }
}
