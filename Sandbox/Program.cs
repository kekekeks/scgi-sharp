using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Linq;
using Nancy;
using ScgiSharp;
using ScgiSharp.Nancy;
using HttpStatusCode = System.Net.HttpStatusCode;
using ScgiSharp.IO;

namespace Sandbox
{
	class Program
	{
		static void Main (string[] args)
		{
			if (args.Contains ("--echo-server"))
				EchoServer ();
			if (args.Contains ("--http-listener-server"))
				HttpListenerEchoServer ();
			else
				NancyServer ();
			
		}

		static void HttpListenerEchoServer ()
		{
			new Nancy.Hosting.Self.NancyHost (new Uri ("http://localhost:10081/")).Start ();
			Thread.Sleep (-1);
		}

		static void NancyServer ()
		{
			new ScgiNancyHost (new DefaultNancyBootstrapper (), CreateListener ()).Start (IPAddress.Any, 10081);
			Thread.Sleep (-1);
		}

		static ScgiSharp.IO.ISocketListener CreateListener ()
		{
			return Environment.GetCommandLineArgs ().Contains ("--libevent") ?
			        (ISocketListener)new ScgiSharp.OarsIo.OarsSocketListener () :
					new DefaultSocketListener ();
		}

		static void EchoServer ()
		{
			var server = new ScgiSharp.ScgiServer (CreateListener ());
			server.Listen (IPAddress.Any, 10081, 64);
			while (true)
			{
				var task = server.AcceptConnectionAsync ();
				task.Wait ();
				Process (task.Result);
			}
		}

		private static async void Process (ScgiConnection conn)
		{
			using (conn)
			{
				var req = await conn.ReadRequest ();
				var echoResponse = new StringWriter ();
				echoResponse.WriteLine ("Method: {0}\nPath: {1}\nQueryString: {2}\nLocalPort: {3}\nRemoteAddress: {4}\nRemotePort: {5}\nScheme: {6}", req.Method, req.Path, req.QueryString, req.LocalPort, req.RemoteAddress, req.RemotePort, req.Scheme);

				echoResponse.WriteLine ("\nHeaders");
				foreach (var header in req.Headers)
				{
					echoResponse.Write ("{0}:", header.Key);
					foreach (var v in header.Value)
						echoResponse.WriteLine ("\t{0}", v);
				}



				await conn.SendResponse (HttpStatusCode.OK, new Dictionary<string, string> (), Encoding.UTF8.GetBytes (echoResponse.ToString ()));
			}
		}
	}}
