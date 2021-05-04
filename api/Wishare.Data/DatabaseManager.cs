using Microsoft.Extensions.Logging;
using System;

namespace Wishare.Data
{
	public class DatabaseManager : IDatabaseManager
	{
		private readonly ILogger<DatabaseManager> _logger;
		public DatabaseManager(ILogger<DatabaseManager> logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}
	}
}
