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

		private List<IOperationPlugin> m_plugins = new List<IOperationPlugin> ();

		protected PluginType add_plugin<PluginType>(PluginType operation_plugin ) where PluginType  : IOperationPlugin
		{
			m_plugins.Add ( operation_plugin );
			return operation_plugin;
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
			
		}

		internal void init (  )
		{
			foreach ( var plugin in m_plugins )
			{
				plugin.init_request ( this.request );
			}
		}
	}
}
