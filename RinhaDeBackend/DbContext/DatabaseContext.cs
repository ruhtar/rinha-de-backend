using Npgsql;

namespace RinhaDeBackend.DbContext
{
    public class DatabaseContext
    {
        private NpgsqlConnection _connection;

        public async Task<NpgsqlConnection> GetConnectionAsync()
        {
            if (_connection == null)
            {
                _connection = new NpgsqlConnection(Utils.ConnectionString);
                await _connection.OpenAsync();
            }

            return _connection;
        }

        public async Task CloseConnectionAsync()
        {
            if (_connection != null && _connection.State == System.Data.ConnectionState.Open)
            {
                await _connection.CloseAsync();
            }
        }
    }
}
