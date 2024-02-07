using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProjeteMais.Shared;
using RinhaDeBackend.Dtos;
using RinhaDeBackend.Entities;
using System.Data;

namespace RinhaDeBackend.Services
{
    public class TransacaoService : ITransacaoService
    {
        public const string ConnectionString = "Host=host.docker.internal;Port=5433;Database=rinha;Username=postgres;Password=123";

        public async Task<OperationResult<ResponseExtratoDto>> GetExtrato(int id)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var limiteCliente = await ObterLimiteDoClientePorIdAsync(id, connection);
                var saldoValor = await ObterSaldoDoClienteAsync(id, connection);
                var transacoes = await ObterTransacoesDoClientePorIdAsync(id, connection);

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

                var result = new OperationResult<ResponseExtratoDto>(true, "Sucesso", response, 200);
                return result;
            }
            catch (Exception ex) {
                await transaction.RollbackAsync();
                return new OperationResult<ResponseExtratoDto>(false, $"Erro: {ex.Message}", null, 500);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<OperationResult<ResponseTransacaoDto>> EfetuarTransacaoAsync(int id, RequestTransacaoDto transacaoDto)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var limiteCliente = await ObterLimiteDoClientePorIdAsync(id, connection);
                var saldoValor = await ObterSaldoDoClienteAsync(id, connection);

                if (transacaoDto.Tipo == 'c')
                {
                    saldoValor += transacaoDto.Valor;
                }
                else
                {
                    if (saldoValor - transacaoDto.Valor < -limiteCliente)
                    {
                        return new OperationResult<ResponseTransacaoDto>(false, "Passou o limite", null, 422);
                    }
                    saldoValor -= transacaoDto.Valor;
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
                await AtualizarSaldoAsync(id, saldoValor, connection);

                var response = new ResponseTransacaoDto
                {
                    Limite = limiteCliente,
                    Saldo = saldoValor
                };

                await transaction.CommitAsync();

                var result = new OperationResult<ResponseTransacaoDto>(true, "Sucesso", response, 200);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new OperationResult<ResponseTransacaoDto>(false, $"Erro: {ex.Message}", null, 500);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private async Task AtualizarSaldoAsync(int id, int saldoValor, NpgsqlConnection connection)
        {
            var query = "UPDATE saldos SET valor = @saldoValor WHERE id = @id";
            await connection.QueryFirstOrDefaultAsync(query, new { saldoValor, id });
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

        private async Task InserirTransacaoAsync(Transacao transacao, IDbConnection connection)
        {
            var query = "INSERT INTO Transacoes (Cliente_Id, Valor, Tipo, Descricao, Realizada_Em) " +
                        "VALUES (@Cliente_Id, @Valor, @Tipo, @Descricao, @Realizada_Em)";

            await connection.ExecuteAsync(query, transacao);
        }
    }
}