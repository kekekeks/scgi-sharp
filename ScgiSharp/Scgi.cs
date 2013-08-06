using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScgiSharp
{
	static class Scgi
	{
		public const string ContentLength = "CONTENT_LENGTH";
		public const string Method = "REQUEST_METHOD";
		public const string Uri = "REQUEST_URI";
		public const string QueryString = "QUERY_STRING";
		public const string ContentType = "CONTENT_TYPE";

		public const string DocumentUri = "DOCUMENT_URI";
		public const string DocumentRoot = "DOCUMENT_ROOT";
		public const string ServerProtocol = "SERVER_PROTOCOL";

		public const string RemoteAddress = "REMOTE_ADDR";
		public const string RemotePort = "REMOTE_PORT";
		public const string ServerPort = "SERVER_PORT";
		public const string ServerName = "SERVER_NAME";

		public const string CustomRoot = "REQUEST_ROOT";
		public const string CustomScheme = "REQUEST_SCHEME";
	}
}
