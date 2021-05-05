using System.Threading;
﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wishare.Data
{
	public interface IQueryExecutor
	{
		public Task<object?> ExecuteScalarAsync(string query, IReadOnlyDictionary<string, object> parameters = default, CancellationToken cancellationToken = default);
		public Task<int> ExecuteNonQueryAsync(string query, IReadOnlyDictionary<string, object> parameters = default, CancellationToken cancellationToken = default);
	}
}
