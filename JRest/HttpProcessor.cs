using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Web;
using System.Diagnostics;

namespace JRest
{
	public class HttpProcessor
	{
		private static string[] VALID_REQUESTS = new string[] { "GET", "POST" };

		private static char[] QUESTION_MARK = new char[] { '?' };

		private TcpClient m_client;

		private HttpServer m_server;
		public Dictionary<string, string> headers = new Dictionary<string, string> ();

		public string path;

		public string request_type;

		public Dictionary<string, string> url_params = new Dictionary<string, string> ();

		public HttpResponse Response = new HttpResponse ();

		private bool m_responseSent = false;

		private StringBuilder log = new StringBuilder ();

		private int m_order = -1;

		private List<Tuple<string, long>> m_perf = new List<Tuple<string, long>>();

		private long m_perfStart = -1;

		public int ID
		{
			get { return m_order; }
		}

		internal HttpProcessor ( TcpClient client, HttpServer server, int order )
		{
			m_client = client;
			m_server = server;
			m_order = order;
			Log ( "ID: {0}", m_order );
			PerfNote ( "Constructor" );
			m_client.GetStream ();
		}


		public void SendResponse ()
		{
			if ( !m_responseSent )
			{
				PerfNote ( "Start-Response" );
				Response.Write ( m_client.GetStream () );
				PerfNote ( "Finish-Response" );
				m_client.Close ();
				m_responseSent = true;
				Log ( "Succesful response {0}", Response.Code.ToString () );
				FlushLogs ();
			}
		}

		[Conditional("PERF_ENABLED")]
		private void PerfNote ( string name )
		{
			if ( m_perfStart == -1 )
			{
				m_perfStart = Stopwatch.GetTimestamp ();
			}
			m_perf.Add ( new Tuple<string, long> ( name, (Stopwatch.GetTimestamp() - m_perfStart) / 10000  ));
		}

		internal void Process ()
		{
			PerfNote ( "Start" );
			Log ( "Processing {0}", m_client.Client.RemoteEndPoint );
			StreamReader reader = new StreamReader ( m_client.GetStream () );
			string line1 = null;
			try
			{
				line1 = reader.ReadLine ();
			}
			catch ( IOException ioe )
			{
				// Just don't care if we don't get a good first line.
				m_client.Close ();
				this.log = null;

				return;
			}

			try {
				PerfNote ( "First line" );
				Log ( "Request {0} ", line1 );
				int space = line1.IndexOf(' ');
				int end = line1.LastIndexOf(" HTTP/");
				if(space > -1 && end > -1) {
					var request_type = line1.Substring(0, space);
					if(VALID_REQUESTS.Contains(request_type))
					{
						this.request_type = request_type;
						var resource = line1.Substring(space + 1, end - (space + 1));
						var split_resource = resource.Split(QUESTION_MARK, 2);
						this.path = HttpUtility.UrlDecode(split_resource[0]);
						if (split_resource.Length > 1)
						{
							ReadUrlParameters(split_resource[1]);	
						}
						PerfNote ( "Read-Headers" );
						ReadHeaders(reader);
						PerfNote ( "Finish-Headers" );
					}
				}
			} catch(Exception e) {
				try
				{
					Response.Code = HttpResponse.ResponseCode.ERROR;
					Response.Body = "";
					SendResponse ();
				}
				catch ( Exception moreExceptions )
				{
					Console.WriteLine ( "{0}", moreExceptions );
				}
				m_client.Close ();
				m_server.OnError ( e, this );
				if ( this.log != null )
				{
					FlushLogs();
				}
			}
			m_server.Success(this);
		}

		private void Log ( string format, params object[] objects )
		{
			log.AppendLine ( String.Format ( format, objects ) );
		}

		private void FlushLogs ()
		{
			Log("Perf:");
			foreach ( var tuple in m_perf )
			{
				Log ( "  {0,15}: {1}", tuple.Item1, tuple.Item2 );
			}
			m_server.FlushLog ( log );
			log = null;
		}

		private void ReadHeaders ( StreamReader reader )
		{
			string line = null;
			int lines = 0;
			while ( line != "" && lines < 100 )
			{
				line = reader.ReadLine ();
				var index = line.IndexOf ( ':' );
				if ( index > -1 )
				{
					string key = line.Substring ( 0, index );
					string value = line.Substring ( index + 1 );
					this.headers[key] = value;
				}
			}
		}

		private void ReadUrlParameters ( string sourceString )
		{
			var parameters = sourceString.Split ( '&' );
			foreach ( var pair in parameters )
			{
				var parameter = pair.Split ( '=' );
				string key = parameter[0];
				string value = null;
				if ( parameter.Length > 0 )
				{
					value = HttpUtility.UrlDecode ( parameter[1] );
				}
				this.url_params[key] = value;
			}
		}

		public int Order { get { return m_order; } }
	}
}
