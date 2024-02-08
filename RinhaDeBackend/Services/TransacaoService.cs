using Npgsql;
using ProjeteMais.Shared;
using RinhaDeBackend.Cache;
using RinhaDeBackend.Dtos;
using RinhaDeBackend.Entities;
using System.Data;

namespace RinhaDeBackend.Services
{
    public class TransacaoService : ITransacaoService
    {
        public const string ConnectionString = "Host=host.docker.internal;Port=5433;Database=rinha;Username=postgres;Password=123;Pooling=true;Maximum Pool Size=400;";
        //

        public async Task<OperationResult<ResponseExtratoDto>> GetExtrato(int id)
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

        public async Task<OperationResult<ResponseTransacaoDto>> EfetuarTransacaoAsync(int id, RequestTransacaoDto transacaoDto)
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

        private async Task<int> ObterSaldo(int clienteId, NpgsqlConnection connection) {
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



        //private async Task AtualizarSaldoAsync(int id, int saldoValor, NpgsqlConnection connection)
        //{
        //    var query = "UPDATE saldos SET valor = @saldoValor WHERE id = @id";
        //    await connection.ExecuteAsync(query, new { saldoValor, id });
        //}

        //private async Task InserirTransacaoAsync(Transacao transacao, IDbConnection connection)
        //{
        //    var query = "INSERT INTO Transacoes (Cliente_Id, Valor, Tipo, Descricao, Realizada_Em) " +
        //                "VALUES (@Cliente_Id, @Valor, @Tipo, @Descricao, @Realizada_Em)";

        //    await connection.ExecuteAsync(query, transacao);
        //}

        //private async Task<List<UltimasTransacoes>> ObterTransacoesDoClientePorIdAsync(int id, IDbConnection connection)
        //{
        //    var query = "SELECT valor, tipo, descricao, realizada_em FROM transacoes WHERE cliente_id = @ClienteId ORDER BY Realizada_Em desc LIMIT 10";
        //    return (List<UltimasTransacoes>)await connection.QueryAsync<UltimasTransacoes>(query, new { ClienteId = id });
        //}

        //private async Task<int> ObterSaldoDoClienteAsync(int id, IDbConnection connection)
        //{
        //    var query = "SELECT valor FROM Saldos WHERE Cliente_Id = @ClienteId";
        //    return await connection.QueryFirstOrDefaultAsync<int>(query, new { ClienteId = id });
        //}

        //private async Task<int> ObterLimiteDoClientePorIdAsync(int id, IDbConnection connection)
        //{
        //    var query = "SELECT limite FROM Clientes WHERE Id = @ClienteId";
        //    return await connection.QueryFirstOrDefaultAsync<int>(query, new { ClienteId = id });
        //}

        //private async Task<(int Limite, int Saldo)> ObterLimiteESaldoDoClienteAsync(int id, IDbConnection connection)
        //{
        //    var query = @"
        //        SELECT c.limite AS Limite, s.valor AS Saldo
        //        FROM Clientes c
        //        JOIN Saldos s ON c.Id = s.Cliente_Id
        //        WHERE c.Id = @ClienteId";

        //    var result = await connection.QueryFirstOrDefaultAsync<(int Limite, int Saldo)>(query, new { ClienteId = id });
        //    return result;
        //}
        //private async Task<(int Limite, int Saldo, List<UltimasTransacoes> Transacoes)> ObterInformacoesDoClienteAsync(int id, IDbConnection connection)
        //{
        //    var query = @"
        //        SELECT 
        //            c.limite AS Limite,
        //            s.valor AS Saldo,
        //            t.valor, t.tipo, t.descricao, t.realizada_em
        //        FROM Clientes c
        //        LEFT JOIN Saldos s ON c.Id = s.Cliente_Id
        //        LEFT JOIN Transacoes t ON c.Id = t.Cliente_Id
        //        WHERE c.Id = @ClienteId
        //        ORDER BY t.realizada_em DESC
        //        LIMIT 10";

        //    var result = await connection.QueryAsync<(int Limite, int Saldo, int? valor, char? tipo, string descricao, DateTime? realizada_em)>(query, new { ClienteId = id });

        //    var transacoes = result
        //           .Where(t => t.valor.HasValue && t.tipo.HasValue && t.realizada_em.HasValue)
        //           .Select(t => new UltimasTransacoes
        //           {
        //               Valor = t.valor.Value,
        //               Tipo = t.tipo.Value,
        //               Descricao = t.descricao,
        //               realizada_em = t.realizada_em.Value
        //           })
        //           .ToList();

        //    return result.Any()
        //        ? (result.First().Limite, result.First().Saldo, transacoes)
        //        : (0, 0, new List<UltimasTransacoes>());
        //}

        //private async Task<ClienteData> ObterDadosDoClienteAsync(int id, IDbConnection connection)
        //{
        //    var query = @"
        //        SELECT valor, tipo, descricao, realizada_em 
        //        FROM transacoes 
        //        WHERE cliente_id = @ClienteId 
        //        ORDER BY realizada_em DESC 
        //        LIMIT 10;

        //        SELECT valor 
        //        FROM Saldos 
        //        WHERE Cliente_Id = @ClienteId;
        //    ";

        //    using (var result = await connection.QueryMultipleAsync(query, new { ClienteId = id }))
        //    {
        //        var transacoes = await result.ReadAsync<UltimasTransacoes>();
        //        var saldo = await result.ReadFirstOrDefaultAsync<int>();

        //        return new ClienteData
        //        {
        //            ClienteId = id,
        //            Transacoes = transacoes.ToList(),
        //            Saldo = saldo
        //        };
        //    }
        //}
    }
}