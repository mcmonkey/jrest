using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using JRest.Operations;
using System.Security.Cryptography.X509Certificates;

namespace JRest.Transactions
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
					if ( mi == null )
					{
						processor.Response.Code = HttpResponse.ResponseCode.NOT_FOUND;
						processor.Response.Body = "Could not find method : " + _method + " on " + path;
						processor.SendResponse ();
					}
					else
					{
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
							else
							{
								// TODO: Come up with error code for missing parameters. 
								// Reflection appears to be ok with coercing null to 0, but I'm not.

							}
							arguments[i] = argument;
							i++;
						}

						Exception rethrow = null;
						try
						{
							operation.init ( processor.headers );
							operation.parse_headers ();
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
			}
			catch ( Exception e )
			{
				processor.Response.Code = HttpResponse.ResponseCode.ERROR;
				processor.Response.Body = e.ToString ();
			}
			processor.SendResponse ();

		}

		public void Start ( int port, X509Certificate ssl = null )
		{
			HttpServer s = new HttpServer ( 8181, ssl );
			s.GET = OnGet;
			s.Start ();
		}
	}
}
