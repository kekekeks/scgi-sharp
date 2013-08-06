using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ScgiSharp
{
	public class ScgiConnection : IDisposable
	{

		readonly Socket _socket;

		bool _requestIsAlreadyRead;
		bool _responseIsAlreadySent;
		bool _closed;

		public ScgiConnection (Socket cl)
		{
			_socket = cl;
		}

		public async Task<ScgiRequest> ReadRequest ()
		{
			if (_requestIsAlreadyRead)
				throw new InvalidOperationException ("SCGI protocol doesn't allow multiple requests per connection");
			_requestIsAlreadyRead = true;

			var headers = (await ScgiParser.GetHeaders (_socket)).ToList ();

			var httpHeaders = headers.Where (h => h.Key.StartsWith ("HTTP_")).ToList ();
			var scgiHeaders = headers.Where (h => !h.Key.StartsWith ("HTTP_")).ToList ();

			var contentLength = long.Parse (scgiHeaders.First (h => h.Key == "CONTENT_LENGTH").Value);
			
			var ms = new MemoryStream ();
			await Util.ReadDataAsync (_socket, ms, contentLength);
			ms.Seek (0, SeekOrigin.Begin);

			return new ScgiRequest (scgiHeaders, httpHeaders, ms);
		}



		public async Task SendResponse (HttpStatusCode statusCode, IEnumerable<KeyValuePair<string, string>> headers, byte[] responseData)
		{
			await SendResponse (statusCode, headers, new MemoryStream (responseData));

		}

		public async Task SendResponse (HttpStatusCode statusCode, IEnumerable<KeyValuePair<string, string>> headers, Stream responseDataStream)
		{
			if (_responseIsAlreadySent)
				throw new InvalidOperationException ("SCGI protocol doesn't allow multiple requests per connection");
			_responseIsAlreadySent = true;

			await SendHeaders (statusCode, headers);

			await Util.WriteDataAsync (_socket, responseDataStream, responseDataStream.Length);
			Close ();
		}
		
		
		async Task SendHeaders (HttpStatusCode statusCode, IEnumerable<KeyValuePair<string, string>> headers)
		{
			var writer = new StringWriter {NewLine = "\r\n"};
			writer.WriteLine ("Status: {0} {1}", (int)statusCode, statusCode);

			foreach (var header in headers)
				writer.WriteLine ("{0}: {1}", header.Key, header.Value);
			writer.WriteLine ();

			await Util.WriteDataAsync (_socket, Encoding.UTF8.GetBytes (writer.ToString ()));
		}


		public void Close ()
		{
			if (_closed)
				return;
			_socket.Shutdown (SocketShutdown.Send);
			_socket.Close ();
			_closed = true;
		}
		
		public void Dispose ()
		{
			_socket.Dispose ();
		}
	}
}
