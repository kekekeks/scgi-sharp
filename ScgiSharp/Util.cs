using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ScgiSharp.IO;

namespace ScgiSharp
{
	static class Util
	{

		public static string CapitalizeWord (string s)
		{
			if (string.IsNullOrEmpty (s))
				return s;
			return char.ToUpperInvariant (s[0]) + s.Substring (1).ToLowerInvariant ();
		}

		public static string NormalizeHeaderName (string header)
		{
			return string.Join ("-", header.Split ('_').Select (CapitalizeWord));
		}

		public static async Task ReadDataAsync (ISocket socket, Stream to, long size, int bufferSize = 8192)
		{
			var buffer = new byte[bufferSize];
			while (size > 0)
			{
				var read = await socket.RecieveAsync (buffer, 0, bufferSize);
				if (read <= 0)
					throw new IOException ("Unexpected end of stream");
				size -= read;
				to.Write (buffer, 0, read);
			}
		}

		public static async Task<byte[]> ReadExactly (ISocket stream, int size)
		{
			var ms = new MemoryStream (size);
			await ReadDataAsync (stream, ms, size, size);
			return ms.ToArray ();
		}

		public static async Task WriteDataAsync (ISocket socket, Stream from, long size, int bufferSize = 8192)
		{
			var buffer = new byte[bufferSize];
			while (size > 0)
			{
				var currentChunkLength = from.Read (buffer, 0, bufferSize);
				if (currentChunkLength <= 0) //Finished
					return;
				size -= currentChunkLength;
				await WriteDataAsync (socket, buffer, currentChunkLength);

			}
		}

		public static async Task WriteDataAsync (ISocket socket, byte[] buffer, int size = -1)
		{
			if (size == -1)
				size = buffer.Length;
			int offset = 0;
			while (size > 0)
			{
				var written = await socket.SendAsync (buffer, offset, size);
				size -= written;
				offset += written;
			}
		}
		


	}
}
