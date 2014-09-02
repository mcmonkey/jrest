using System;
using System.Collections.Generic;
using System.Text;
using Http;
using System.Reflection;
using System.Diagnostics;

namespace Heritage.Transactions
{
	public class HttpTransactionManager
	{
		private static Type[] s_constructor_parameters = new Type[0];

		private static object[] s_constructor_arguments = new object[0];

		private static Dictionary<string, Type> m_operation_cache = new Dictionary<string, Type> ();

		private string m_operation_package_root = "";

		private Assembly m_operation_assembly;

		public HttpTransactionManager ( Assembly operation_assembly, string package_root )
		{
			this.m_operation_assembly = operation_assembly;
			this.m_operation_package_root = package_root;
		}

		private void OnGet ( HttpProcessor processor )
		{

			//processor.Response.Body = "<html><head><title>lololol</title></head><body>Hi newbs " + processor.ID + "</body></html>";
			//processor.Response.SetHeader ( "Content", "text/html" );
			//processor.SendResponse ();
			//return;
			string path = processor.path;
			int method_location = path.LastIndexOf ( '.' );
			string _method = null;
			Type t = null;
			try
			{

				if ( method_location > 0 )
				{
					_method = path.Substring ( method_location + 1 );

					path = path.Substring ( 0, method_location );
					path = path.Replace ( '/', '.' );

					if(!m_operation_cache.TryGetValue(path, out t))
					{
						t = m_operation_assembly.GetType ( m_operation_package_root + path, false, true );
						if ( t != null )
						{
							m_operation_cache.Add ( path, t );
						}
					}

				}
				if ( t == null )
				{
					processor.Response.Code = HttpResponse.ResponseCode.NOT_FOUND;
					processor.Response.Body = "Could not find Operation for location: " + processor.path;
					processor.SendResponse ();
				}
				else
				{

					BaseOperation operation = t.GetConstructor ( s_constructor_parameters ).Invoke ( s_constructor_arguments ) as BaseOperation;

					operation.request = processor;

					MethodInfo mi = t.GetMethod ( _method, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public );
					ParameterInfo[] pi = mi.GetParameters ();
					object[] arguments = new object[pi.Length];
					int i = 0;
					foreach ( var param in pi )
					{
						string url_value;
						Type param_type = param.ParameterType;
						object argument = null;
						if ( processor.url_params.TryGetValue ( param.Name, out url_value ) )
						{
							if ( param_type == typeof ( string ) )
							{
								argument = url_value;
							}
							else if ( param_type == typeof ( int ) )
							{
								argument = int.Parse ( url_value );
							}
							else if ( param_type == typeof ( float ) )
							{
								argument = float.Parse ( url_value );
							}
							else
							{
								argument = null;
							}
						}
						arguments[i] = argument;
						i++;
					}

					Exception rethrow = null;
					try
					{
						operation.Before ();
						mi.Invoke ( operation, arguments );
						operation.After ();
						processor.Response.Code = operation.ResponseCode ();
						processor.Response.Body = operation.GetResponse ();
						foreach ( var header in operation.Headers )
						{
							processor.Response.SetHeader ( header.Key, header.Value );
						}
					}
					catch ( Exception e )
					{
						rethrow = e;
					}

					if ( rethrow != null ) throw rethrow;


				}
			}
			catch ( Exception e )
			{
				processor.Response.Code = HttpResponse.ResponseCode.ERROR;
				processor.Response.Body = e.ToString ();
			}
			processor.SendResponse ();

		}

		internal void Start ( int port )
		{
			HttpServer s = new HttpServer ( 8181 );
			s.GET = OnGet;
			s.Start ();
		}
	}
}
