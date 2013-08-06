using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;

namespace Sandbox
{
	public class EchoModule : NancyModule
	{
		public EchoModule ()
		{
			Get["/"] = r => "Hello world from NancyFx!";
			Get["/echo"] = Echo;
			Post["/echo"] = Echo;
		}

		private dynamic Echo (dynamic args)
		{
			var echoResponse = new StringWriter ();
			echoResponse.WriteLine ("Method: {0}\nPath: {1}\nQueryString: {2}\nLocalPort: {3}\nRemoteAddress: {4}\nScheme: {5}", Request.Method, Request.Path, Request.Url.Query, Request.Url.Port, Request.UserHostAddress, Request.Url.Scheme);

			echoResponse.WriteLine ("\nHeaders");
			foreach (var header in Request.Headers)
			{
				echoResponse.Write ("{0}:", header.Key);
				foreach (var v in header.Value)
					echoResponse.WriteLine ("\t{0}", v);
			}

			return new Nancy.Responses.TextResponse (echoResponse.ToString ());

		}
	}
}
