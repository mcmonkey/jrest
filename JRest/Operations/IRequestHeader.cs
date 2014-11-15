using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JRest.Operations
{
	public interface IOperationPlugin
	{
		void init_request (HttpProcessor request);
	}
}
