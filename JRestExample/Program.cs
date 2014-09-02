using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JRest.Transactions;
using System.Reflection;

namespace JRestExample
{
	class Program
	{
		static void Main ( string[] args )
		{
			var manager = new HttpTransactionManager ( Assembly.GetExecutingAssembly (), "JRestExample" );
			manager.Start ( 8181 );
		}
	}
}
