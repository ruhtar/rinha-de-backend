﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ProjeteMais.Shared;
using RinhaDeBackend.Cache;
using RinhaDeBackend.Dtos;
using RinhaDeBackend.Entities;
using RinhaDeBackend.Services;

namespace RinhaDeBackend.Controllers
{
    [ApiController]
    public class ClientesController : ControllerBase
    {
        //private readonly ITransacaoService _transacaoService;

        //public ClientesController(ITransacaoService transacaoService)
        //{
        //    _transacaoService = transacaoService;
        //}

        [HttpGet("clientes/{id}/extrato")]
        public async Task<IActionResult> GetExtratoAsync([FromRoute] int id)
        {
            var result = await GetExtrato(id);
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }
            return result.GetResponseWithStatusCode(); //ALTERAR ISSO PARA O FORMATO DA API CORRETO
        }

        [HttpPost("clientes/{id}/transacoes")]
        public async Task<ActionResult<ResponseTransacaoDto>> FazerTransacaoAsync([FromRoute] int id, [FromBody] RequestTransacaoDto transacaoDto)
        {
            {
                if (id > 5 && id <= 0) //fui mlk aqui
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

                if (transacaoDto.Descricao.Length < 1 || transacaoDto.Descricao.Length > 10)
                {
                    return BadRequest("A descrição da transação deve ter entre 1 e 10 caracteres.");
                }

                var result = await EfetuarTransacaoAsync(id, transacaoDto);
                if (!result.IsSuccess)
                {
                    return UnprocessableEntity();
                }
                var response = result.Data;
                return Ok(new
                {
                    limite = response!.Limite,
                    saldo = response!.Saldo,
                });
            }
        }

        public const string ConnectionString = "Host=host.docker.internal;Port=5433;Database=rinha;Username=postgres;Password=123;Pooling=true;Maximum Pool Size=400;";

        private async Task<OperationResult<ResponseExtratoDto>> GetExtrato(int id)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    //using var transaction = await connection.BeginTransactionAsync();

                    try
                    {
                        int limiteCliente = ClientesCache.ObterLimiteCliente(id);
                        //int saldoValor = await ObterSaldoDoClienteAsync(id, connection);
                        //List<UltimasTransacoes> transacoes = await ObterTransacoesDoClientePorIdAsync(id, connection);

                        //var data = await ObterDadosDoClienteAsync(id, connection);
                        var saldo = await ObterSaldo(id, connection);
                        var transacoes = await ObterTransacoes(id, connection);

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

                        //await transaction.CommitAsync();
                        await connection.CloseAsync();

                        var result = new OperationResult<ResponseExtratoDto>(true, "Sucesso", response, 200);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        //await transaction.RollbackAsync();
                        await connection.CloseAsync();
                        return new OperationResult<ResponseExtratoDto>(false, $"Erro: {ex.Message}", null, 500);
                    }
                }
                catch (Exception ex)
                {
                    await connection.CloseAsync();
                    return new OperationResult<ResponseExtratoDto>(false, $"Erro de conexão: {ex.Message}", null, 500);
                }
            }
        }

        private async Task<OperationResult<ResponseTransacaoDto>> EfetuarTransacaoAsync(int id, RequestTransacaoDto transacaoDto)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {

                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    var saldoValor = await ObterSaldo(id, connection); //criar variavel nova e usar ao inves de saldoValor
                    var limiteCliente = ClientesCache.ObterLimiteCliente(id);

                    var novoSaldo = 0;

                    if (transacaoDto.Tipo == 'c')
                    {
                        novoSaldo = saldoValor + transacaoDto.Valor;
                    }
                    else
                    {
                        novoSaldo = saldoValor - transacaoDto.Valor;
                    }

                    if ((limiteCliente + novoSaldo) < 0)
                    {
                        await transaction.RollbackAsync();
                        await connection.CloseAsync();
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

                    await AtualizarSaldoAsync(id, novoSaldo, connection); //Posso tentar usar uma subquery aqui que consulta o saldo e tenta atualizar ele. Deve haver alguma regra no proprio banco pra nao deixar o saldo ser atualizado caso ele ultrapasse o limite.
                    await InserirTransacaoAsync(transacao, connection);


                    var response = new ResponseTransacaoDto
                    {
                        Limite = limiteCliente,
                        Saldo = novoSaldo
                    };

                    await transaction.CommitAsync();

                    var result = new OperationResult<ResponseTransacaoDto>(true, "Sucesso", response, 200);
                    await connection.CloseAsync();
                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await connection.CloseAsync();
                    return new OperationResult<ResponseTransacaoDto>(false, $"Erro: {ex.Message}", null, 500);
                }
            }
        }

        private async Task AtualizarSaldoAsync(int clienteId, int novoSaldo, NpgsqlConnection connection)
        {
            var query = "UPDATE saldos SET valor = @novoSaldo WHERE cliente_id = @clienteId";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@novoSaldo", novoSaldo);
            cmd.Parameters.AddWithValue("@clienteId", clienteId);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InserirTransacaoAsync(Transacao transacao, NpgsqlConnection connection)
        {
            var query = "INSERT INTO transacoes (cliente_id, valor, tipo, descricao, realizada_em) VALUES (@clienteId, @valor, @tipo, @descricao, @realizadaEm)";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@clienteId", transacao.Cliente_Id);
            cmd.Parameters.AddWithValue("@valor", transacao.Valor);
            cmd.Parameters.AddWithValue("@tipo", transacao.Tipo);
            cmd.Parameters.AddWithValue("@descricao", transacao.Descricao);
            cmd.Parameters.AddWithValue("@realizadaEm", transacao.Realizada_Em);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> ObterSaldo(int clienteId, NpgsqlConnection connection)
        {
            using var cmd = new NpgsqlCommand("SELECT valor FROM saldos WHERE cliente_id = @clienteId FOR UPDATE", connection);
            cmd.Parameters.AddWithValue("@clienteId", clienteId);
            var result = await cmd.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return 0;
        }

        private async Task<List<UltimasTransacoes>> ObterTransacoes(int clienteId, NpgsqlConnection connection)
        {
            var query = "SELECT valor, tipo, descricao, realizada_em FROM transacoes WHERE cliente_id = @clienteId ORDER BY Realizada_Em desc LIMIT 10";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@clienteId", clienteId);
            var reader = await cmd.ExecuteReaderAsync();

            var transacoes = new List<UltimasTransacoes>();

            while (await reader.ReadAsync())
            {
                var ultimaTransacao = new UltimasTransacoes
                {
                    Valor = reader.GetInt32(0),
                    Tipo = reader.GetChar(1),
                    Descricao = reader.GetString(2),
                    realizada_em = reader.GetDateTime(3)
                };

                transacoes.Add(ultimaTransacao);
            }

            reader.Close();

            return transacoes;
        }

    }
}
