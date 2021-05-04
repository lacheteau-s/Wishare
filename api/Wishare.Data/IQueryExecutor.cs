using System.Threading;
using System.Threading.Tasks;

namespace Wishare.Data
{
	public interface IQueryExecutor
	{
		public Task<object?> ExecuteScalarAsync(string query, CancellationToken cancellationToken = default);
	}
}
