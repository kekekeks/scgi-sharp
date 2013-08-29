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

		Task<NancyContext> HandleRequestAsync (ScgiRequest req)
		{
			var tcs = new TaskCompletionSource<NancyContext> ();
			_engine.HandleRequest (ConvertScgiRequestToNancyRequest (req), tcs.SetResult, tcs.SetException);
			return tcs.Task;
		}

		void ProcessConnection (ScgiConnection connection)
		{
			connection.ReadRequestAsync ().ContinueWith (readReqTask =>
			{
				if (readReqTask.IsFaulted)
				{
					LogException (readReqTask.Exception);
					SendResponseFailSafeNoWait (connection, HttpStatusCode.BadRequest);
					return TaskFromResult (0);
				}
				var request = readReqTask.Result;

				return HandleRequestAsync(request).ContinueWith (handlerTask =>
				{
					if (handlerTask.IsFaulted)
					{
						LogException (handlerTask.Exception);
						SendResponseFailSafeNoWait (connection, HttpStatusCode.InternalServerError, null, new MemoryStream (Encoding.UTF8.GetBytes (handlerTask.Exception.ToString ())));
						return TaskFromResult (0);
					}
					return SendNancyResponse (connection, handlerTask.Result.Response);
				}).Unwrap ();

			}).Unwrap ().ContinueWith (finalTask =>
			{
				if (finalTask.IsFaulted)
					LogException (finalTask.Exception);
				connection.Dispose ();
			});





		}

	    void SendResponseFailSafeNoWait (ScgiConnection connection, HttpStatusCode statusCode, IEnumerable<KeyValuePair<string, string>> headers = null, MemoryStream response = null) 
		{
			if (headers == null)
				headers = new Dictionary<string, string> ();
			if (response == null)
				response = new MemoryStream ();
			connection.SendResponse (statusCode, headers, response).ContinueWith (t =>
			{
				if (t.IsFaulted)
					LogException (t.Exception);
			});
		}

	    protected virtual Request ConvertScgiRequestToNancyRequest (ScgiRequest request)
	    {
			return new Request (request.Method, request.Path, request.Headers.ToDictionary (h => h.Key, h => (IEnumerable<string>)h.Value), RequestStream.FromStream (request.Body, request.ContentLength, long.MaxValue), request.Scheme, request.QueryString, request.RemoteAddress);
	    }
		
		protected virtual void PostProcessNancyResponse (Response response)
		{
			response.Headers["Content-Type"] = response.ContentType;
		}

		Task SendNancyResponse (ScgiConnection conn, Response response)
		{
			var body = new MemoryStream ();
			try
			{
				response.Contents (body);
				body.Seek (0, SeekOrigin.Begin);
			}
			catch (Exception e)
			{
				return TaskFromException<int> (e);
			}

			return conn.SendResponse ((HttpStatusCode)(int)response.StatusCode, response.Headers, body).ContinueWith (t =>
			{
				if (t.IsFaulted)
				{
					var e = UnwrapException(t.Exception);
					if (!(e is SocketException || e is IOException))
						LogException (t.Exception);
				}
				conn.Close ();
				return TaskFromResult (1);
			}).Unwrap ();
			
		}

		Exception UnwrapException (Exception e)
		{
			while (true)
			{
				var ae = e as AggregateException;
				if (ae == null || ae.InnerExceptions.Count > 1)
					return e;
				e = ae.InnerException;
			}
		}

	    protected virtual void LogException (Exception e)
		{
			Console.WriteLine ("Exception: {0}", e);
		}

		static Task<T> TaskFromResult<T> (T value)
		{
			var tcs = new TaskCompletionSource<T> ();
			tcs.SetResult (value);
			return tcs.Task;
		}

		static Task<T> TaskFromException<T> (Exception e)
		{
			var tcs = new TaskCompletionSource<T> ();
			tcs.SetException (e);
			return tcs.Task;
		}
    }
}
