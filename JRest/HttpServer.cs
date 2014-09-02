using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;
using System.Collections.Concurrent;

namespace JRest
{
	public delegate void ProcessHandler ( HttpProcessor processor );

	public class HttpServer
	{
		private int m_port;

		public List<Thread> operationThreads = new List<Thread>();

		internal List<ProcessQueue> m_processingQueues = new List<ProcessQueue> ();


		public ProcessHandler GET;

		public ProcessHandler POST;

		private StreamWriter m_accessLog;

		private StreamWriter m_errorLog;

		private ConcurrentQueue<StringBuilder> m_accesses = new ConcurrentQueue<StringBuilder> ();

		private int m_order = 0;


		public HttpServer ( int port = 8080 )
		{
			m_port = port;
		}

		public void Start ()
		{
			Console.WriteLine ( "Starting HTTP server on port {0}", m_port );
			m_accessLog = File.AppendText ( @"./access.log" );
			m_errorLog = File.AppendText ( @"./error.log" );
			m_accessLog.WriteLine ( "Restart " + DateTime.Now );
			m_errorLog.WriteLine ( "Restart " + DateTime.Now );

			Thread thread;
			thread = new Thread ( AccessLogging );
			thread.Start ();

			operationThreads.Add ( thread );


			for ( int i = 0; i < 5; i++ )
			{
				var pq = new ProcessQueue ();
				pq.thread = thread;

				m_processingQueues.Add(new ProcessQueue());

				thread = new Thread (  ProcessThread );
				thread.Start ( m_processingQueues[i] );
			}

			thread = new Thread ( Run );
			thread.Start ();

			operationThreads.Add ( thread );

		}

		private void ProcessThread ( object param )
		{
			ProcessQueue pq = param as ProcessQueue;
			ConcurrentQueue<HttpProcessor> queue = pq.queue;
			HttpProcessor processor;
			while ( true )
			{
				pq.wait.WaitOne ();

				bool success = false;
				lock ( pq )
				{
					success = queue.TryDequeue ( out processor );
					if(!success) pq.wait.Reset();
				}
				if (success)
				{
					try
					{
						try
						{
							processor.Process ();
						}
						catch ( Exception e )
						{
							OnError ( e, processor );
						}
					}
					catch ( Exception e )
					{

						// Give up, move on.
					}

				}

			}
		}

		private void Run ()
		{

			TcpListener listener = new TcpListener ( IPAddress.Any, m_port );

			listener.Start ();
			while ( true )
			{
				TcpClient client = listener.AcceptTcpClient ();
				client.ReceiveTimeout = 500;
				Console.WriteLine ( "Accepted client from {0}", client.Client.RemoteEndPoint.ToString());
				HttpProcessor http = new HttpProcessor ( client, this, m_order++ );
				Enqueue ( http );
			}
		}

		private void Enqueue ( HttpProcessor http )
		{
			int min = int.MaxValue;
			int count = 0;
			ProcessQueue pq = null;
			ProcessQueue best = null;
			for ( int i = m_processingQueues.Count - 1; i > -1; --i )
			{
				pq = m_processingQueues[i];
				count = pq.queue.Count;
				if ( count == 0 )
				{
					best = pq;
					break;
				}
				else if ( count < min )
				{
					min = count;
					best = pq;
				}
			}
			lock ( best )
			{
				best.queue.Enqueue ( http );
				best.wait.Set ();
			}
		}

		private void AccessLogging ()
		{
			while ( true )
			{
				Thread.Sleep ( 100 );
				int i = 0;
				StringBuilder sb;
				while ((i < 5 || m_accesses.Count > 100) && m_accesses.Count > 0)
				{
					if ( m_accesses.TryDequeue ( out sb ) )
					{
						m_accessLog.WriteLine ( sb.ToString () );
					}
					else
					{
						break;
					}
					i++;
				}
				m_accessLog.Flush ();
			}
		}

		internal void FlushLog ( StringBuilder log )
		{
			Console.WriteLine ( log.ToString () );
			m_accesses.Enqueue ( log );
		}

		internal void Success ( HttpProcessor processor )
		{
			try
			{

				switch ( processor.request_type )
				{
					case "GET":
						if ( GET != null )
							GET ( processor );
						break;
					case "POST":
						if ( POST != null )
							POST ( processor );
						break;
					default:
						processor.Response.Code = HttpResponse.ResponseCode.ERROR;
						processor.SendResponse ();
						break;
				}
			}
			catch ( Exception e )
			{
				processor.Response.Code = HttpResponse.ResponseCode.ERROR;
				processor.SendResponse ();
				OnError ( e, processor );

			}
		}

		internal void OnError ( Exception e, HttpProcessor p )
		{
			string id = string.Format ( "ID: {0}", p.Order );
			m_errorLog.WriteLine ( id );
			m_errorLog.WriteLine ( e );
			Console.WriteLine ( id );
			Console.WriteLine ( e );
		}
		
		internal class ProcessQueue
		{
			internal Thread thread;

			internal ConcurrentQueue<HttpProcessor> queue = new ConcurrentQueue<HttpProcessor>();

			internal EventWaitHandle wait = new EventWaitHandle(true, EventResetMode.AutoReset);
		}
	}
}
