using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace JRest {
	public class HttpResponse
	{
		public enum ResponseCode
		{
			OK = 200,
			NOT_FOUND = 404,
			ERROR = 500
		}

		public static Dictionary<ResponseCode, string> code_values = new Dictionary<ResponseCode, string>();
		static HttpResponse() {
			code_values[ResponseCode.OK] = "OK";
			code_values[ResponseCode.NOT_FOUND] = "Not Found";
			code_values[ResponseCode.ERROR] = "Error";
		}

		public ResponseCode Code = ResponseCode.NOT_FOUND;

		public string Body = "";

		private Dictionary<string, string> headers = new Dictionary<string, string> ();
		public void Write(Stream s)
		{
			var writer = new StreamWriter(s);
			writer.Write("HTTP/1.1 ");
			writer.Write((int)Code);
			writer.Write(" ");
			writer.Write(code_values[Code]);
			writer.Write("\r\n");

			foreach ( var kvp in headers )
			{
				writer.Write ( "{0}: {1}\r\n", kvp.Key, kvp.Value );
			}

			writer.Write ( "\r\n" );

			if (Body != null)
			{
				writer.Write(Body);
			}
			writer.Flush();
		}


		public void SetHeader ( string header_key, string header_value)
		{
			headers[header_key] = header_value;
		}
	}
}
