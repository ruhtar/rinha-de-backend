using Dapper;
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
        //private readonly NpgsqlConnection _dbConnection;
        private readonly DatabaseContext dbContext;

        public ClientesController(DatabaseContext _dbContext)
        {
            dbContext = _dbContext;
        }


        //[HttpGet("/clientes/{id}/extrato")]
        //public async Task<IActionResult> Extrato(int id)
        //{
        //    using (var conn = _dbConnection)
        //    {
        //        conn.Open();

        //        var cliente = await conn.QueryFirstOrDefaultAsync<Cliente>("SELECT * FROM clientes WHERE id = @Id", new { Id = id });

        //        if (cliente == null)
        //        {
        //            return NotFound();
        //        }

        //        var transacoes = await conn.QueryAsync<Transacao>("SELECT * FROM transacoes WHERE cliente_id = @Id ORDER BY ID DESC LIMIT 10", new { Id = id });

        //        return Ok(new
        //        {
        //            code = 200,
        //            data = new
        //            {
        //                saldo = new
        //                {
        //                    total = cliente.Saldo,
        //                    data_extrato = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
        //                    limite = cliente.Limite
        //                },
        //                ultimas_transacoes = transacoes
        //            }
        //        });
        //    }
        //}

        //[HttpPost("/clientes/{id}/transacoes")]
        //public async Task<IActionResult> Transacoes(int id, [FromBody] TransacaoRequest data)
        //{
        //    if (data.Valor < 0 || data.Valor != (int)data.Valor)
        //    {
        //        return UnprocessableEntity();
        //    }

        //    if (!new[] { 'c', 'd' }.Contains(data.Tipo))
        //    {
        //        return UnprocessableEntity();
        //    }

        //    if (string.IsNullOrEmpty(data.Descricao) || data.Descricao.Length == 0 || data.Descricao.Length > 10)
        //    {
        //        return UnprocessableEntity();
        //    }

        //    using (var conn = _dbConnection)
        //    {
        //        conn.Open();

        //        var cliente = await conn.QueryFirstOrDefaultAsync<Cliente>("SELECT saldo, limite FROM clientes WHERE id = @Id", new { Id = id });

        //        if (cliente == null)
        //        {
        //            return NotFound();
        //        }

        //        if (data.Tipo == 'c')
        //        {
        //            cliente.Saldo += (int)data.Valor;
        //        }
        //        else
        //        {
        //            cliente.Saldo -= data.Valor;

        //            if (cliente.Saldo < -cliente.Limite)
        //            {
        //                return UnprocessableEntity();
        //            }
        //        }

        //        await conn.ExecuteAsync("INSERT INTO transacoes (cliente_id, tipo, valor, descricao) VALUES (@Id, @Tipo, @Valor, @Descricao)", new { Id = id, data.Tipo, data.Valor, data.Descricao });
        //        await conn.ExecuteAsync("UPDATE clientes SET saldo = @Saldo WHERE id = @Id", new { Saldo = cliente.Saldo, Id = id });

        //        return Ok(new
        //        {
        //            code = 200,
        //            data = new
        //            {
        //                limite = cliente.Limite,
        //                saldo = cliente.Saldo
        //            }
        //        });
        //    }
        //}

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
                return Ok(new
                {
                    limite = response!.Limite,
                    saldo = response!.Saldo,
                });
            }
        }

        private async Task<OperationResult<ResponseExtratoDto>> GetExtrato(int id)
        {
            try
            {
                var conn = await dbContext.GetConnectionAsync();
                {
                    //await conn.OpenAsync();
                    int limiteCliente = ClientesCache.ObterLimiteCliente(id);
                    var saldo = await ObterSaldo(id, conn);
                    var transacoes = await ObterTransacoes(id, conn);

                    //await conn.CloseAsync();

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
                var conn = await dbContext.GetConnectionAsync();
                {
                    //await conn.OpenAsync();
                    var transaction = await conn.BeginTransactionAsync();
                    var saldoValor = await ObterSaldo(id, conn); //criar variavel nova e usar ao inves de saldoValor
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
                        //await conn.CloseAsync();

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

                    await AtualizarSaldoAsync(id, novoSaldo, conn); //Posso tentar usar uma subquery aqui que consulta o saldo e tenta atualizar ele. Deve haver alguma regra no proprio banco pra nao deixar o saldo ser atualizado caso ele ultrapasse o limite.
                    await InserirTransacaoAsync(transacao, conn);


                    await transaction.CommitAsync();
                    //await conn.CloseAsync();

                    var response = new ResponseTransacaoDto
                    {
                        Limite = limiteCliente,
                        Saldo = novoSaldo
                    };

                    var result = new OperationResult<ResponseTransacaoDto>(true, "Sucesso", response, 200);

                    return result;
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
