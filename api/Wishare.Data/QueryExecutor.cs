using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Wishare.Data
{
	public class QueryExecutor : IQueryExecutor
	{
		private readonly DbProviderFactory _dbProviderFactory;
		private readonly string _connectionString;

		public QueryExecutor(DbProviderFactory dbProviderFactory, string connectionString)
		{
			_dbProviderFactory = dbProviderFactory ?? throw new ArgumentNullException(nameof(dbProviderFactory));
			_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}

		public Task<object?> ExecuteScalarAsync(string query, CancellationToken cancellationToken = default)
			=> ExecuteAsync(query, (command) => command.ExecuteScalarAsync(cancellationToken), cancellationToken);

		private async Task<T> ExecuteAsync<T>(string query, Func<DbCommand, Task<T>> execute, CancellationToken cancellationToken = default)
		{
			await using var connection = await CreateConnection(cancellationToken);
			using var command = CreateCommand(connection, query);

			return await execute(command);
		}

		private async Task<DbConnection> CreateConnection(CancellationToken cancellationToken = default)
		{
			var connection = _dbProviderFactory.CreateConnection();

			connection.ConnectionString = _connectionString;
			await connection.OpenAsync(cancellationToken);
			
			return connection;
		}

		private DbCommand CreateCommand(DbConnection connection, string query)
		{
			var command = connection.CreateCommand();

			command.CommandText = query;
			command.CommandType = System.Data.CommandType.Text;

			return command;
		}
	}
}
