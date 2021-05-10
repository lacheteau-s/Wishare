using System;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Wishare.Data.Tests
{
	internal class SqlExceptionFactory
	{
		private const BindingFlags _flags = BindingFlags.NonPublic | BindingFlags.Instance;

		private static T Instantiate<T>(params object[] parameters) =>
			(T)typeof(T).GetConstructors(_flags)
			.First(c => c.GetParameters().Length == parameters.Length)
			.Invoke(parameters);

		internal static SqlException Create(int number)
		{
			var error = Instantiate<SqlError>(number, new byte(), new byte(), "", "", "", new int(), null);
			var errorCollection = Instantiate<SqlErrorCollection>();

			typeof(SqlErrorCollection).GetMethod("Add", _flags).Invoke(errorCollection, new[] { error });

			return Instantiate<SqlException>(new object[] { "", errorCollection, null, Guid.NewGuid() });
		}
	}
}
