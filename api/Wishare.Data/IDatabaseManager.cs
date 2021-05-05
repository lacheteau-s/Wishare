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
	}
}
