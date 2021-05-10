using System.Threading;
﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wishare.Data
{
	public interface IQueryExecutor
	{
		public Task<object?> ExecuteScalarAsync(Query query, CancellationToken cancellationToken = default);
		public Task<int> ExecuteNonQueryAsync(Query query, CancellationToken cancellationToken = default);
	}
}
