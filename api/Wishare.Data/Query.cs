using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Wishare.Data
{
	public class Query : IEnumerable<KeyValuePair<string, object>>
	{
		private readonly IDictionary<string, object> _parameters = new Dictionary<string, object>();

		public string Text { get; }
		public CommandType Type { get; }

		public Query(string query, CommandType type = CommandType.Text)
		{
			Text = query ?? throw new ArgumentNullException(nameof(query));
			Type = type;
		}

		public Query Add(string name, object value)
		{
			_parameters.Add(name, value);
			return this;
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _parameters.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
