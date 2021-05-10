using Moq;
using Moq.Protected;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Wishare.Data.Tests
{
	public class QueryExecutorTests
	{
		private readonly Mock<DbProviderFactory> _dbProviderFactory = new();
		private readonly Mock<DbConnection> _dbConnection = new();
		private readonly Mock<DbCommand> _dbCommand = new();
		private readonly Mock<DbParameterCollection> _dbParameters = new();
		private readonly Mock<DbParameter> _dbParameter = new();

		private const string _connectionString = "Server=serverAddress;Database=db;User Id=user;Password=password;";
		private const string _query = "query";

		private readonly QueryExecutor _target;

		public QueryExecutorTests()
		{
			_dbProviderFactory.Setup(x => x.CreateConnection()).Returns(_dbConnection.Object);
			_dbConnection.Protected().Setup<DbCommand>("CreateDbCommand").Returns(_dbCommand.Object);
			_dbCommand.Protected().SetupGet<DbParameterCollection>("DbParameterCollection").Returns(_dbParameters.Object);
			_dbCommand.Protected().Setup<DbParameter>("CreateDbParameter").Returns(() => _dbParameter.Object);

			_dbParameter.SetupSet(p => p.ParameterName = "name").Verifiable();
			_dbParameter.SetupSet(p => p.Value = "value").Verifiable();

			_target = new QueryExecutor(_dbProviderFactory.Object, _connectionString);
		}

		#region ExecuteScalar

		[Fact]
		public async Task ExecuteScalarAsync_Success()
		{
			await _target.ExecuteScalarAsync(new Query(_query));

			VerifyConnection();
			VerifyCommand(_query);
		}

		[Fact]
		public async Task ExecuteScalarAsync_WithParameters_Success()
		{
			await _target.ExecuteScalarAsync(new Query(_query)
			{
				{ "name", "value" }
			});

			VerifyConnection();
			VerifyCommand(_query);
			VerifyParameters();
		}

		[Theory]
		[InlineData(10061)]
		[InlineData(4060)]
		public async Task ExecuteScalarAsync_Error_Throws(int errorCode)
		{
			_dbProviderFactory.Setup(x => x.CreateConnection()).Returns(_dbConnection.Object);
			_dbConnection.Setup(x => x.OpenAsync(CancellationToken.None)).Throws(SqlExceptionFactory.Create(errorCode));

			await Assert.ThrowsAsync<SqlException>(() => _target.ExecuteScalarAsync(It.IsAny<Query>()));

			VerifyConnection();
			_dbConnection.VerifySet(c => c.ConnectionString = _connectionString);
			_dbConnection.Verify(c => c.OpenAsync(CancellationToken.None));
			_dbConnection.Verify(c => c.DisposeAsync(), Times.Once());
		}

		#endregion

		#region ExecuteNonQuery

		[Fact]
		public async Task ExecuteNonQueryAsync_Success()
		{
			await _target.ExecuteNonQueryAsync(new Query(_query));

			VerifyConnection();
			VerifyCommand(_query);
		}

		[Fact]
		public async Task ExecuteNonQueryAsync_WithParameters_Success()
		{
			await _target.ExecuteNonQueryAsync(new Query(_query)
			{
				{ "name", "value" }
			});

			VerifyConnection();
			VerifyCommand(_query);
			VerifyParameters();
		}

		[Theory]
		[InlineData(10061)]
		[InlineData(4060)]
		public async Task ExecuteNonQueryAsync_Error_Throws(int errorCode)
		{
			_dbProviderFactory.Setup(x => x.CreateConnection()).Returns(_dbConnection.Object);
			_dbConnection.Setup(x => x.OpenAsync(CancellationToken.None)).Throws(SqlExceptionFactory.Create(errorCode));

			await Assert.ThrowsAsync<SqlException>(() => _target.ExecuteScalarAsync(It.IsAny<Query>()));

			VerifyConnection();
			_dbConnection.VerifySet(c => c.ConnectionString = _connectionString);
			_dbConnection.Verify(c => c.OpenAsync(CancellationToken.None));
			_dbConnection.Verify(c => c.DisposeAsync(), Times.Once());
		}


		#endregion

		private void VerifyConnection()
		{
			_dbConnection.VerifySet(c => c.ConnectionString = _connectionString);
			_dbConnection.Verify(c => c.OpenAsync(CancellationToken.None));
			_dbConnection.Verify(c => c.DisposeAsync(), Times.Once());
		}

		private void VerifyCommand(string query)
		{
			_dbCommand.VerifySet(c => c.CommandText = query);
			_dbCommand.VerifySet(c => c.CommandType = System.Data.CommandType.Text);
			_dbCommand.Protected().Verify("Dispose", Times.Once(), true, ItExpr.IsAny<bool>());
		}

		private void VerifyParameters()
		{
			_dbParameter.VerifySet(p => p.ParameterName = "name", Times.Once());
			_dbParameter.VerifySet(p => p.Value = "value", Times.Once());
			_dbParameters.Verify(p => p.Add(_dbParameter.Object), Times.Once());
		}
	}
}
