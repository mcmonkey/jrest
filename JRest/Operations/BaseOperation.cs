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
	}
}
