using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
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
		private const string _insertNewVersionQuery = "INSERT INTO schema_version VALUES (@version, @file_name, @update_date);";

		public DatabaseManager(IFileProvider sqlFileProvider, IQueryExecutor queryExecutor, ILogger<DatabaseManager> logger)
		{
			_sqlFileProvider = sqlFileProvider ?? throw new ArgumentNullException(nameof(sqlFileProvider));
			_queryExecutor = queryExecutor?? throw new ArgumentNullException(nameof(queryExecutor));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public int ExpectedSchemaVersion => GetScripts().Last().Version;

		public async Task<int?> TryGetCurrentSchemaVersion()
		{
			try
			{
				var version = await _queryExecutor.ExecuteScalarAsync(new Query(_getCurrentVersionQuery));
				return version == DBNull.Value
					? throw new DatabaseException("Failed to retrieve version from database: table 'schema_version' is empty.")
					: (int)version;
			}
			catch (SqlException ex) when (ex.Number == 208)
			{
				return null;
			}
		}

		public async Task<bool> CheckDatabaseVersion()
		{
			var currentVersion = await TryGetCurrentSchemaVersion();

			if (!currentVersion.HasValue)
			{
				_logger.LogWarning("Database unitialized.");
				return false;
			}

			var expectedVersion = ExpectedSchemaVersion;

			if (currentVersion < expectedVersion)
			{
				_logger.LogWarning($"Database is out of date. Expected version: {expectedVersion}. Current version: {currentVersion}");
				return false;
			}

			if (currentVersion > expectedVersion)
				throw new DatabaseException($"Database version ({currentVersion}) is ahead of target ({expectedVersion}). The application was likely downgraded.");

			_logger.LogInformation($"Database is up to date.");
			return true;
		}

		public async Task UpdateDatabase()
		{
			_logger.LogInformation("Updating database...");

			var currentVersion = await TryGetCurrentSchemaVersion() ?? -1;

			foreach (var script in GetScripts().Where(s => s.Version > currentVersion))
			{
				if (script.Version - currentVersion > 1)
					throw new InvalidOperationException($"Missing script for version {currentVersion + 1}.");

				await ExecuteScript(script);
				
				currentVersion = script.Version;
			}

			_logger.LogInformation("Database is up to date.");
		}

		private async Task ExecuteScript((int Version, IFileInfo File) script)
		{
			var query = "";

			using (var stream = script.File.CreateReadStream())
			{
				using var reader = new StreamReader(stream, Encoding.UTF8);
				query = await reader.ReadToEndAsync();
			}

			await _queryExecutor.ExecuteNonQueryAsync(new Query(query));
			await _queryExecutor.ExecuteNonQueryAsync(new Query(_insertNewVersionQuery)
			{
				{ "@version", script.Version },
				{ "@file_name", script.File.Name },
				{ "@update_date", DateTime.UtcNow }
			});

			_logger.LogInformation($"Applied script {script.File.PhysicalPath}. Database now at version {script.Version}");
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
