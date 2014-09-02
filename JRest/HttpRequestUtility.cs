using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Http
{
	public static class HttpRequestUtility
	{
		public static string Request(string host, string request, int port = 80)
		{
			TcpClient client = new TcpClient();
			client.Connect(host, port);

			var writer = new StreamWriter(client.GetStream());
			var reader = new StreamReader(client.GetStream());
			writer.Write(request);
			writer.Write("\r\n\r\n");

			client.ReceiveTimeout = 1000;

			StringBuilder message = new StringBuilder();
			int total = 1000;
			while (client.Connected && total > 0)
			{
				message.Append(reader.ReadToEnd());
				Thread.Sleep(5);
				total--;
			}
			return message.ToString();
		}
	}
}
