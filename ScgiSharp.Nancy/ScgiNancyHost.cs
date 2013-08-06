using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.IO;
using ScgiSharp.IO;
using HttpStatusCode = System.Net.HttpStatusCode;


namespace ScgiSharp.Nancy
{
    public class ScgiNancyHost
    {
		readonly ScgiServer _server;
	    readonly INancyEngine _engine;

	    public ScgiNancyHost (INancyBootstrapper bootstrapper, ISocketListener socketListener = null)
		{
			if (socketListener == null)
				socketListener = new DefaultSocketListener ();
			_server = new ScgiServer (socketListener);

			bootstrapper.Initialise ();
			_engine = bootstrapper.GetEngine ();
		}



		public void Start (IPAddress address, int port, int backlog = 64)
		{
			_server.Listen (address, port, backlog);
			_server.AcceptConnectionAsync ().ContinueWith (HandleConnectionAccepted);
		}

	    void HandleConnectionAccepted (Task<ScgiConnection> task)
	    {
			_server.AcceptConnectionAsync ().ContinueWith (HandleConnectionAccepted);
			if (task.IsFaulted)
			{
				LogException (task.Exception);
				return;
			}
			ProcessConnection (task.Result);

	    }

	    async void ProcessConnection (ScgiConnection connection)
	    {
		    using (connection)
		    {
				ScgiRequest request = null;

				try
				{
					request = await connection.ReadRequest ();
				}
				catch (Exception e)
				{
					LogException (e);
					SendResponseFailSafeNoWait (connection, HttpStatusCode.BadRequest);
					return;
				}

				NancyContext context = null;
				try
			    {
					await Task.Run (() =>
					{
						context = _engine.HandleRequest (ConvertScgiRequestToNancyRequest (request));
						PostProcessNancyResponse (context.Response);
					});
					
			    }
			    catch (Exception e)
			    {
				    LogException (e);
					SendResponseFailSafeNoWait (connection, HttpStatusCode.InternalServerError, null, new MemoryStream (Encoding.UTF8.GetBytes (e.ToString ())));
				    throw;
			    }



				await SendNancyResponse (connection, context.Response);


			}
	    }

	    async void SendResponseFailSafeNoWait (ScgiConnection connection, HttpStatusCode statusCode, IEnumerable<KeyValuePair<string, string>> headers = null, MemoryStream response = null) 
		{
			if (headers == null)
				headers = new Dictionary<string, string> ();
			if (response == null)
				response = new MemoryStream ();
			try
			{
				await connection.SendResponse (statusCode, headers, response);
			}
			catch (Exception ee)
			{
				LogException (ee);
			}
		}

	    protected virtual Request ConvertScgiRequestToNancyRequest (ScgiRequest request)
	    {
			return new Request (request.Method, request.Path, request.Headers.ToDictionary (h => h.Key, h => (IEnumerable<string>)h.Value), RequestStream.FromStream (request.Body, request.ContentLength, long.MaxValue), request.Scheme, request.QueryString, request.RemoteAddress);
	    }
		
		protected virtual void PostProcessNancyResponse (Response response)
		{
			response.Headers["Content-Type"] = response.ContentType;
		}

		async Task SendNancyResponse (ScgiConnection conn, Response response)
		{
			try
			{
				var body = new MemoryStream ();
				response.Contents (body);
				body.Seek (0, SeekOrigin.Begin);
				await conn.SendResponse ((HttpStatusCode)(int)response.StatusCode, response.Headers, body);
				conn.Close ();
			}
			catch (IOException e)
			{
				if (!(e.InnerException is SocketException)) //Ignore closed socket exceptions
					LogException (e);
			}
			catch (SocketException) { }
			catch (Exception e)
			{
				LogException (e);
			}
		
			
		}

	    protected virtual void LogException (Exception e)
		{
			Console.WriteLine ("Exception: {0}", e);
		}

    }
}
