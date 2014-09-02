using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JRest.Operations;

namespace JRestExample.Operations
{
	public class Test : BaseOperation
	{

		
		public void example ( int i, string s )
		{
			JSONReponse(new { integer_plus_2 = i + 2, string_= s, hohoho = "Yo"});
		}
	}
}
