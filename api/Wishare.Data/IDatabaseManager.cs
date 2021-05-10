using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wishare.Data
{
	public interface IDatabaseManager
	{
		int ExpectedSchemaVersion { get; }
		Task<int?> TryGetCurrentSchemaVersion();
		Task<bool> CheckDatabaseVersion();
		Task UpdateDatabase();
	}
}
