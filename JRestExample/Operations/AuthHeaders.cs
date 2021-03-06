﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JRest.Operations;
using JRest.Util;
using JRest;

namespace JRestExample.Operations
{
	public class AuthHeaders : IOperationPlugin
	{
		public string user_name { get; private set; }

		public bool verified { get; private set; }

		public string auth_type { get; private set; }

		public void init_request (HttpProcessor processor)
		{
			string header_val;
			if ( processor.headers.TryGetValue ( "Authorization", out header_val ) )
			{
				var split = header_val.Trim().Split ( ' ' );
				var type = split[0];
				var val = split[1];

				val = Base64StringEncoding.decode ( val, Encoding.UTF8);

				int seperator_index = val.IndexOf ( ':' );
				if ( seperator_index != -1 )
				{
					var user = val.Substring ( 0, seperator_index );
					var pasword = val.Substring ( seperator_index + 1 );
					Console.WriteLine ( "Found user: {0} password: {1}", user, pasword );
				}
			}
		}
	}
}
