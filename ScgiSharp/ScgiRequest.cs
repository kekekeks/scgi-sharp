using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScgiSharp
{
	public class ScgiRequest
	{
		public Stream Body { get; set; }
		public string Method { get; set; }
		public string Path { get; set; }
		public string QueryString { get; set; }

		public string LocalPort { get; set; }
		public string RemoteAddress { get; set; }
		public string RemotePort { get; set; }
		public string Scheme { get; set; }


		public Dictionary<string, List<string>> Headers { get; set; }

		public long ContentLength { get; set; }


		public ScgiRequest (IEnumerable<KeyValuePair<string, string>> serviceHeaders, IEnumerable<KeyValuePair<string, string>> httpHeaders, MemoryStream body)
		{
			Body = body;
			Headers = TransformHeaders (httpHeaders);

			var args = serviceHeaders.ToDictionary (p => p.Key, p => p.Value);
			
			Method = args[Scgi.Method];

			Path = args[Scgi.DocumentUri];
			QueryString = args[Scgi.QueryString];
			if(args.ContainsKey (Scgi.CustomRoot))
			{
				var customRoot = args[Scgi.CustomRoot];
				Path = Path.Substring (customRoot.Length);
				if (!Path.StartsWith ("/"))
					Path = "/" + Path;
			}

			Scheme = "http";
			if (args.ContainsKey (Scgi.CustomScheme))
				Scheme = args[Scgi.CustomScheme];


			LocalPort = args[Scgi.ServerPort];
			RemoteAddress = args[Scgi.RemoteAddress];
			RemotePort = args[Scgi.RemotePort];


			ContentLength = long.Parse (args[Scgi.ContentLength]);
			SetHttpHeader ("Content-Length", args[Scgi.ContentLength]);
			if (args.ContainsKey (Scgi.ContentType))
			{
				var contentType = args[Scgi.ContentType];
				if (!string.IsNullOrWhiteSpace (contentType))
					SetHttpHeader ("Content-Type", args[Scgi.ContentType]);
			}
		}
		
		void SetHttpHeader (string header, string value)
		{
			Headers[header] = new List<string> { value };
		}

		Dictionary<string, List<string>> TransformHeaders (IEnumerable<KeyValuePair<string, string>> headers)
		{
			var rv = new Dictionary<string, List<string>> ();
			foreach (var header in headers)
			{
				var headerName = Util.NormalizeHeaderName (header.Key.Substring ("HTTP_".Length));
				List<string> lst;
				if (!rv.TryGetValue (headerName, out lst))
					rv[headerName] = lst = new List<string> ();
				lst.Add (header.Value);
			}
			return rv;
		}

	}
}