using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JRest.Operations
{
	public abstract class BaseOperation
	{

		private JRest.HttpResponse.ResponseCode m_response = JRest.HttpResponse.ResponseCode.OK;

		private object m_jsonPayload = null;

		public  Dictionary<string, string> Headers = new Dictionary<string, string> ();

		public HttpProcessor request;

		private List<IRequestHeader> request_header_handlers = new List<IRequestHeader> ();

		protected IDictionary<string, string> request_headers = null;

		protected RequestHeaderType add_request_header<RequestHeaderType>(RequestHeaderType request_header ) where RequestHeaderType  : IRequestHeader
		{
			request_header_handlers.Add ( request_header );
			return request_header;
		}

		internal protected virtual void Before ()
		{
		}

		internal protected virtual void After ()
		{
			
		}

		internal JRest.HttpResponse.ResponseCode ResponseCode ()
		{
			return this.m_response;
		}

		protected void Error ()
		{
			m_response = JRest.HttpResponse.ResponseCode.ERROR;
		}

		protected void JSONReponse ( object payload )
		{

			this.m_jsonPayload = payload;
			Headers.Add ( "Content-Type", "application/json" );
		}

		internal string GetResponse ()
		{
			var s = new System.Web.Script.Serialization.JavaScriptSerializer ();
			return s.Serialize ( this.m_jsonPayload );
		}

		internal void parse_headers ()
		{
			foreach ( var request_header in request_header_handlers )
			{
				request_header.parse_headers ( request_headers );
			}
		}

		internal void init ( IDictionary<string, string> headers )
		{
			request_headers = headers;
		}
	}
}
