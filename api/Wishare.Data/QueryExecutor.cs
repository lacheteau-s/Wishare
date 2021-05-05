using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Linq;
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

		public Task<object?> ExecuteScalarAsync(string query, IReadOnlyDictionary<string, object> parameters = default, CancellationToken cancellationToken = default)
			=> ExecuteAsync(query, parameters, (command) => command.ExecuteScalarAsync(cancellationToken), cancellationToken);

		public Task<int> ExecuteNonQueryAsync(string query, IReadOnlyDictionary<string, object> parameters = default, CancellationToken cancellationToken = default)
			=> ExecuteAsync(query, parameters, (command) => command.ExecuteNonQueryAsync(cancellationToken), cancellationToken);
		
		private async Task<T> ExecuteAsync<T>(string query, IReadOnlyDictionary<string, object> parameters, Func<DbCommand, Task<T>> execute, CancellationToken cancellationToken = default)
		{
			await using var connection = await CreateConnection(cancellationToken);
			using var command = CreateCommand(connection, query, parameters);

			return await execute(command);
		}

		private async Task<DbConnection> CreateConnection(CancellationToken cancellationToken = default)
		{
			var connection = _dbProviderFactory.CreateConnection();

			try
			{
				connection.ConnectionString = _connectionString;
				await connection.OpenAsync(cancellationToken);

				return connection;
			}
			catch
			{
				await connection.DisposeAsync();
				throw;
			}
		}

		private DbCommand CreateCommand(DbConnection connection, string query, IReadOnlyDictionary<string, object> parameters)
		{
			var command = connection.CreateCommand();

			try
			{
				command.CommandText = query;
				command.CommandType = System.Data.CommandType.Text;

				if (parameters != null && parameters.Any())
				{
					foreach (var p in parameters)
					{
						var parameter = command.CreateParameter();
						parameter.ParameterName = p.Key;
						parameter.Value = p.Value;
						command.Parameters.Add(parameter);
					}
				}

				return command;
			}
			catch
			{
				command.Dispose();
				throw;
			}
		}
	}
}
