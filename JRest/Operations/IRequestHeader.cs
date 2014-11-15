using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JRest.Operations
{
	public interface IRequestHeader
	{
		void parse_headers (IDictionary<string, string> headers);
	}
}
