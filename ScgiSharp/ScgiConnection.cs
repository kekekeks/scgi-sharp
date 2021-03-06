﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ScgiSharp.IO;

namespace ScgiSharp
{
	public class ScgiConnection : IDisposable
	{

		readonly ISocket _socket;

		bool _requestIsAlreadyRead;
		bool _responseIsAlreadySent;
		bool _closed;
		bool _disposed;

		public ScgiConnection (ISocket cl)
		{
			_socket = cl;
		}

		public Task<ScgiRequest> ReadRequestAsync ()
		{
			CheckClosed ();
			if (_requestIsAlreadyRead)
				throw new InvalidOperationException ("SCGI protocol doesn't allow multiple requests per connection");
			_requestIsAlreadyRead = true;

			return ScgiParser.GetHeaders (_socket).ContinueWith (htask =>
			{
				htask.PropagateExceptions ();
				CheckClosed ();
				var headers = htask.Result.ToList ();

				var httpHeaders = headers.Where (h => h.Key.StartsWith ("HTTP_")).ToList ();
				var scgiHeaders = headers.Where (h => !h.Key.StartsWith ("HTTP_")).ToList ();

				var contentLength = long.Parse (scgiHeaders.First (h => h.Key == "CONTENT_LENGTH").Value);

				var ms = new MemoryStream ();
				return Util.ReadDataAsync (_socket, ms, contentLength).ContinueWith (btask =>
				{
					btask.PropagateExceptions ();
					CheckClosed ();
					ms.Seek (0, SeekOrigin.Begin);
					return Util.TaskFromResult (new ScgiRequest (scgiHeaders, httpHeaders, ms));
				}).Unwrap ();
			}).Unwrap ();
		}



		public Task SendResponse (HttpStatusCode statusCode, IEnumerable<KeyValuePair<string, string>> headers, byte[] responseData)
		{
			return SendResponse (statusCode, headers, new MemoryStream (responseData));
		}

		public Task SendResponse (HttpStatusCode statusCode, IEnumerable<KeyValuePair<string, string>> headers, Stream responseDataStream)
		{
			CheckClosed ();

			if (_responseIsAlreadySent)
				throw new InvalidOperationException ("SCGI protocol doesn't allow multiple requests per connection");
			_responseIsAlreadySent = true;

			return SendHeaders (statusCode, headers).ContinueWith (t =>
			{
				t.PropagateExceptions ();
				CheckClosed ();
				return Util.WriteDataAsync (_socket, responseDataStream, responseDataStream.Length).ContinueWith (t2 =>
					{
						t2.PropagateExceptions ();
						CheckClosed ();
						Close ();
						return Util.TaskFromResult (1);
					}).Unwrap ();
			}).Unwrap ();
		}

		void CheckClosed ()
		{
			if (_disposed)
				throw new ObjectDisposedException ("ScgiConnection");
			if (_closed)
				throw new InvalidOperationException ("Socket was closed by user code");
		}
		
		Task SendHeaders (HttpStatusCode statusCode, IEnumerable<KeyValuePair<string, string>> headers)
		{
			var writer = new StringWriter {NewLine = "\r\n"};
			writer.WriteLine ("Status: {0} {1}", (int)statusCode, statusCode);

			foreach (var header in headers)
				writer.WriteLine ("{0}: {1}", header.Key, header.Value);
			writer.WriteLine ();

			return Util.WriteDataAsync (_socket, Encoding.UTF8.GetBytes (writer.ToString ()));
		}


		public void Close ()
		{
			if (_closed)
				return;
			_socket.Close ();
			_closed = true;
		}
		
		public void Dispose ()
		{
			_closed = true;
			_disposed = true;
			_socket.Dispose ();
		}
	}
}
