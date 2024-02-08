using Dapper;
using Microsoft.EntityFrameworkCore;
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
        public const string ConnectionString = "Host=host.docker.internal;Port=5433;Database=rinha;Username=postgres;Password=123;Pooling=true;Maximum Pool Size=1500;";


        public async Task<OperationResult<ResponseExtratoDto>> GetExtrato(int id)
        {
            using var connection = new NpgsqlConnection(ConnectionString);

            try
            {
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // var informacoesCliente = await ObterInformacoesDoClienteAsync(id, connection);

                    int limiteCliente = ClientesCache.ObterLimiteCliente(id);
                    int saldoValor = await ObterSaldoDoClienteAsync(id, connection);
                    List<UltimasTransacoes> transacoes = await ObterTransacoesDoClientePorIdAsync(id, connection);

                    var response = new ResponseExtratoDto
                    {
                        Saldo = new SaldoInfo
                        {
                            Total = saldoValor,
                            data_extrato = DateTime.UtcNow,
                            Limite = limiteCliente,
                        },
                        ultimas_transacoes = transacoes,
                    };

                    await transaction.CommitAsync();
                    await connection.CloseAsync();

                    var result = new OperationResult<ResponseExtratoDto>(true, "Sucesso", response, 200);
                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new OperationResult<ResponseExtratoDto>(false, $"Erro: {ex.Message}", null, 500);
                }
            }
            catch (Exception ex)
            {
                return new OperationResult<ResponseExtratoDto>(false, $"Erro de conexão: {ex.Message}", null, 500);
            }
        }

        public async Task<OperationResult<ResponseTransacaoDto>> EfetuarTransacaoAsync(int id, RequestTransacaoDto transacaoDto)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // var limiteCliente = await ObterLimiteDoClientePorIdAsync(id, connection);
                // var saldoValor = await ObterSaldoDoClienteAsync(id, connection);
                var saldoValor = await ObterSaldoDoClienteAsync(id, connection); //criar variavel nova e usar ao inves de saldoValor
                var limiteCliente = ClientesCache.ObterLimiteCliente(id);

                var novoSaldo = 0;

                if (transacaoDto.Tipo == 'c')
                {
                    novoSaldo = saldoValor + transacaoDto.Valor;
                }
                else
                {
                    //if (saldoValor - transacaoDto.Valor < -limiteCliente)
                    //{
                    //    return new OperationResult<ResponseTransacaoDto>(false, "Passou o limite", null, 422);
                    //}
                    novoSaldo = saldoValor - transacaoDto.Valor;
                }

                if ((limiteCliente + novoSaldo) < 0)
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

                await InserirTransacaoAsync(transacao, connection);
                await AtualizarSaldoAsync(id, novoSaldo, connection);

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
                return new OperationResult<ResponseTransacaoDto>(false, $"Erro: {ex.Message}", null, 500);
            }
        }

        private async Task AtualizarSaldoAsync(int id, int saldoValor, NpgsqlConnection connection)
        {
            var query = "UPDATE saldos SET valor = @saldoValor WHERE id = @id";
            await connection.ExecuteAsync(query, new { saldoValor, id });
        }

        private async Task InserirTransacaoAsync(Transacao transacao, IDbConnection connection)
        {
            var query = "INSERT INTO Transacoes (Cliente_Id, Valor, Tipo, Descricao, Realizada_Em) " +
                        "VALUES (@Cliente_Id, @Valor, @Tipo, @Descricao, @Realizada_Em)";

            await connection.ExecuteAsync(query, transacao);
        }
        

        private async Task<List<UltimasTransacoes>> ObterTransacoesDoClientePorIdAsync(int id, IDbConnection connection)
        {
            var query = "SELECT valor, tipo, descricao, realizada_em FROM transacoes WHERE cliente_id = @ClienteId ORDER BY Realizada_Em desc LIMIT 10";
            return (List<UltimasTransacoes>)await connection.QueryAsync<UltimasTransacoes>(query, new { ClienteId = id });
        }

        private async Task<int> ObterSaldoDoClienteAsync(int id, IDbConnection connection)
        {
            var query = "SELECT valor FROM Saldos WHERE Cliente_Id = @ClienteId";
            return await connection.QueryFirstOrDefaultAsync<int>(query, new { ClienteId = id });
        }

        private async Task<int> ObterLimiteDoClientePorIdAsync(int id, IDbConnection connection)
        {
            var query = "SELECT limite FROM Clientes WHERE Id = @ClienteId";
            return await connection.QueryFirstOrDefaultAsync<int>(query, new { ClienteId = id });
        }

        private async Task<(int Limite, int Saldo)> ObterLimiteESaldoDoClienteAsync(int id, IDbConnection connection)
        {
            var query = @"
                SELECT c.limite AS Limite, s.valor AS Saldo
                FROM Clientes c
                JOIN Saldos s ON c.Id = s.Cliente_Id
                WHERE c.Id = @ClienteId";

            var result = await connection.QueryFirstOrDefaultAsync<(int Limite, int Saldo)>(query, new { ClienteId = id });
            return result;
        }
        private async Task<(int Limite, int Saldo, List<UltimasTransacoes> Transacoes)> ObterInformacoesDoClienteAsync(int id, IDbConnection connection)
        {
            var query = @"
                SELECT 
                    c.limite AS Limite,
                    s.valor AS Saldo,
                    t.valor, t.tipo, t.descricao, t.realizada_em
                FROM Clientes c
                LEFT JOIN Saldos s ON c.Id = s.Cliente_Id
                LEFT JOIN Transacoes t ON c.Id = t.Cliente_Id
                WHERE c.Id = @ClienteId
                ORDER BY t.realizada_em DESC
                LIMIT 10";

            var result = await connection.QueryAsync<(int Limite, int Saldo, int? valor, char? tipo, string descricao, DateTime? realizada_em)>(query, new { ClienteId = id });

            var transacoes = result
                   .Where(t => t.valor.HasValue && t.tipo.HasValue && t.realizada_em.HasValue)
                   .Select(t => new UltimasTransacoes
                   {
                       Valor = t.valor.Value,
                       Tipo = t.tipo.Value,
                       Descricao = t.descricao,
                       realizada_em = t.realizada_em.Value
                   })
                   .ToList();

            return result.Any()
                ? (result.First().Limite, result.First().Saldo, transacoes)
                : (0, 0, new List<UltimasTransacoes>());
        }
    }
}