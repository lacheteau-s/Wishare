using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Internal;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Wishare.Data.Tests
{
	public class DatabaseManagerTests
	{
		private Mock<IFileProvider> _sqlFileProvider = new();
		private Mock<IQueryExecutor> _queryExecutor = new();
		private Mock<ILogger<DatabaseManager>> _logger = new();

		private readonly DatabaseManager _target;

		public DatabaseManagerTests()
		{
			_target = new DatabaseManager(_sqlFileProvider.Object, _queryExecutor.Object, _logger.Object);
		}

		#region ExpectedSchemaVersion

		[Theory]
		[InlineData("0000_test.sql", "0001_test.sql", "0002_test.sql")]
		[InlineData("0", "01", "002", "0003", "0004_", "0005.sql", "0006_.sql", "0007__.sql", "test.sql", "test", "0000_test.sql", "0001_test.sql", "0002_test.sql")]
		public void ExpectedSchemaVersion_WithFiles_ReturnsVersion(params string[] files)
		{
			SetupSqlDirectoryContent(files);

			Assert.Equal(2, _target.ExpectedSchemaVersion);
		}

		[Theory]
		[InlineData(new object[] { new string[] {  } })] // https://github.com/xunit/xunit/issues/2060
		[InlineData(new object[] { new string[] { "0", "01", "002", "0003", "0004_", "0005.sql", "0006_.sql", "0007__.sql", "test.sql", "test" } })]
		public void ExpectedSchemaVersion_NoFiles_Throws(string[] files)
		{
			SetupSqlDirectoryContent(files);

			var ex = Assert.Throws<InvalidOperationException>(() => _target.ExpectedSchemaVersion);
			Assert.Equal("Sequence contains no elements", ex.Message);
		}

		#endregion

		#region TryGetCurrentSchemaVersion

		[Fact]
		public async Task TryGetCurrentSchemaVersion_WithValue_ReturnsValue()
		{
			var expectedVersion = 42;

			_queryExecutor.Setup(x => x.ExecuteScalarAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>())).ReturnsAsync(expectedVersion);

			var actualVersion = await _target.TryGetCurrentSchemaVersion();

			Assert.Equal(expectedVersion, actualVersion);
		}

		[Fact]
		public async Task TryGetCurrentSchemaVersion_NoTable_ReturnsNull()
		{
			_queryExecutor.Setup(x => x.ExecuteScalarAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(SqlExceptionFactory.Create(208));

			var version = await _target.TryGetCurrentSchemaVersion();

			Assert.Null(version);
		}

		[Fact]
		public async Task TryGetCurrentSchemaVersion_EmptyTable_Throws()
		{
			_queryExecutor.Setup(x => x.ExecuteScalarAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>())).ReturnsAsync(DBNull.Value);

			var ex = await Assert.ThrowsAsync<DatabaseException>(() => _target.TryGetCurrentSchemaVersion());
			Assert.Equal("Failed to retrieve version from database: table 'schema_version' is empty.", ex.Message);
		}

		#endregion

		#region CheckDatabaseVersion

		[Fact]
		public async Task CheckDatabaseVersion_NoVersion_ReturnsFalse()
		{
			var tableNotFoundErrorCode = 208;

			_queryExecutor.Setup(x => x.ExecuteScalarAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(SqlExceptionFactory.Create(tableNotFoundErrorCode));

			var result = await _target.CheckDatabaseVersion();

			Assert.False(result);
			VerifyLog(LogLevel.Warning, "Database unitialized.");
		}

		[Fact]
		public async Task CheckDatabaseVersion_OutOfDate_ReturnsFalse()
		{
			var expectedServer = 1;
			var expectedLocal = 2;

			SetupSqlDirectoryContent(new[]{ "0000_test.sql", "0001_test.sql", "0002_test.sql" });
			_queryExecutor.Setup(x => x.ExecuteScalarAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>())).ReturnsAsync(expectedServer);

			var result = await _target.CheckDatabaseVersion();

			Assert.False(result);
			VerifyLog(LogLevel.Warning, $"Database is out of date. Expected version: {expectedLocal}. Current version: {expectedServer}");
		}

		[Fact]
		public async Task CheckDatabaseVersion_Ahead_Throws()
		{
			var expectedServer = 3;
			var expectedLocal = 2;

			SetupSqlDirectoryContent(new[] { "0000_test.sql", "0001_test.sql", "0002_test.sql" });
			_queryExecutor.Setup(x => x.ExecuteScalarAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>())).ReturnsAsync(expectedServer);

			var ex = await Assert.ThrowsAsync<DatabaseException>(() => _target.CheckDatabaseVersion());

			Assert.Equal($"Database version ({expectedServer}) is ahead of target ({expectedLocal}). The application was likely downgraded.", ex.Message);
		}

		 [Fact]
		 public async Task CheckDatabaseVersion_UpToDate_ReturnsTrue()
		{
			var expected = 2;

			SetupSqlDirectoryContent(new[] { "0000_test.sql", "0001_test.sql", "0002_test.sql" });
			_queryExecutor.Setup(x => x.ExecuteScalarAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

			var result = await _target.CheckDatabaseVersion();

			Assert.True(result);
			VerifyLog(LogLevel.Information, "Database is up to date.");
		}

		#endregion

		#region UpdateDatabase

		[Theory]
		[InlineData(-1, 2)] // -1 == return from TryGetCurrentVersion when DB is uninitialized
		[InlineData(0, 1)] 
		public async Task UpdateDatabase_MissingScript_Throws(int startVersion, int iterations)
		{
			var missingVersion = 2;

			SetupSqlDirectoryContent(new Dictionary<string, string>
			{ 
				["0000_test.sql"] = "script 0",
				["0001_test.sql"] = "script 1",
				["0003_test.sql"] = "script 3"
			});
			_queryExecutor.Setup(x => x.ExecuteScalarAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>())).ReturnsAsync(startVersion);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _target.UpdateDatabase());

			Assert.Equal($"Missing script for version {missingVersion}.", ex.Message);

			_queryExecutor.Verify(x => x.ExecuteNonQueryAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>()), Times.Exactly(iterations * 2));

			VerifyLog(LogLevel.Information, "Applied script", Times.Exactly(iterations));
		}

		[Fact]
		public async Task UpdateDatabase_AlreadyUpToDate_Logs()
		{
			SetupSqlDirectoryContent(new Dictionary<string, string>
			{
				["0000_test.sql"] = "script 0",
				["0001_test.sql"] = "script 1",
				["0002_test.sql"] = "script 2"
			});
			_queryExecutor.Setup(x => x.ExecuteScalarAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);

			await _target.UpdateDatabase();
			_queryExecutor.Verify(x => x.ExecuteNonQueryAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>()), Times.Never);

			VerifyLog(LogLevel.Information, "Applied script", Times.Never());
			VerifyLog(LogLevel.Information, "Database is up to date.");
		}

		[Theory]
		[InlineData(-1, 3)]
		[InlineData(0, 2)]
		public async Task UpdateDatabase_Success(int startVersion, int iterations)
		{
			SetupSqlDirectoryContent(new Dictionary<string, string>
			{
				["0000_test.sql"] = "script 0",
				["0001_test.sql"] = "script 1",
				["0002_test.sql"] = "script 2"
			});
			_queryExecutor.Setup(x => x.ExecuteScalarAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>())).ReturnsAsync(startVersion);

			await _target.UpdateDatabase();

			_queryExecutor.Verify(x => x.ExecuteNonQueryAsync(It.IsAny<Query>(), It.IsAny<CancellationToken>()), Times.Exactly(iterations * 2));

			VerifyLog(LogLevel.Information, "Applied script", Times.Exactly(iterations));
			VerifyLog(LogLevel.Information, "Database is up to date.");
		}

		#endregion

		private void VerifyLog(LogLevel level, string message, Times times) => _logger.Verify(l => l.Log(
			level,
			It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((v, _) => v.ToString().Contains(message)),
			null,
			(Func<object, Exception, string>)It.IsAny<object>()), times);

		private void VerifyLog(LogLevel level, string message) => VerifyLog(level, message, Times.Once());


		private void SetupSqlDirectoryContent(IReadOnlyList<string> files)
		{
			SetupSqlDirectoryContent(files.ToDictionary(f => f, _ => It.IsAny<string>()));
		}

		private void SetupSqlDirectoryContent(IReadOnlyDictionary<string, string> files)
		{
			var sqlDirectoryContents = new Mock<IDirectoryContents>();
			var fileEnumerator = files.Select(f => SetupFileInfo(f.Key, f.Value).Object).GetEnumerator();

			sqlDirectoryContents.Setup(x => x.GetEnumerator()).Returns(fileEnumerator);

			_sqlFileProvider
				.Setup(x => x.GetDirectoryContents(It.IsAny<string>()))
				.Returns(sqlDirectoryContents.Object);
		}

		private static Mock<IFileInfo> SetupFileInfo(string name, string content)
		{
			var fileInfo = new Mock<IFileInfo>();

			fileInfo.SetupGet(f => f.Name).Returns(name);

			if (content != null)
				fileInfo.Setup(f => f.CreateReadStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes(content)));
			
			return fileInfo;
		}
	}
}
