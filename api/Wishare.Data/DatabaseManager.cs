using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Wishare.Data
{
	public class DatabaseManager : IDatabaseManager
	{
		private readonly IFileProvider _sqlFileProvider;
		private readonly IQueryExecutor _queryExecutor;
		private readonly ILogger<DatabaseManager> _logger;

		private static readonly Regex _sqlFileRegex = new("^[0-9]{4}_(?!_)[a-zA-Z_]+.sql$", RegexOptions.Compiled);

		private const string _getCurrentVersionQuery = "SELECT MAX(version) FROM schema_version";

		public DatabaseManager(IFileProvider sqlFileProvider, IQueryExecutor queryExecutor, ILogger<DatabaseManager> logger)
		{
			_sqlFileProvider = sqlFileProvider ?? throw new ArgumentNullException(nameof(sqlFileProvider));
			_queryExecutor = queryExecutor?? throw new ArgumentNullException(nameof(queryExecutor));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public int ExpectedSchemaVersion => GetScripts().Last().Version;

		public async Task<int> GetCurrentSchemaVersion()
		{
			var version = await _queryExecutor.ExecuteScalarAsync(_getCurrentVersionQuery);
			return version == DBNull.Value
				? throw new DatabaseException("Failed to retrieve version from database: table 'schema_version' is empty.")
				: (int)version;
		}

		public async Task<bool> CheckDatabaseVersion()
		{
			var expectedVersion = ExpectedSchemaVersion;
			var actualVersion = await GetCurrentSchemaVersion();

			if (expectedVersion != actualVersion)
			{
				_logger.LogWarning($"Database is out of date. Expected version: {expectedVersion}. Actual version: {actualVersion}");
				return false;
			}
			
			_logger.LogInformation($"Database is up to date.");
			return true;
		}

		private IEnumerable<(int Version, IFileInfo File)> GetScripts()
		{
			return _sqlFileProvider.GetDirectoryContents("")
				.Where(f => _sqlFileRegex.IsMatch(f.Name))
				.Select(f => (Version: ParseVersion(f.Name), File: f))
				.OrderBy(f => f.Version);

			static int ParseVersion(string name) => int.Parse(name.Substring(0, name.IndexOf('_')));
		}
	}
}
