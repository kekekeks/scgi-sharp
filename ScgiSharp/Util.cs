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

		public static Task ReadDataAsync (ISocket socket, Stream to, long size, int bufferSize = 8192)
		{
			var buffer = new byte[bufferSize];
			Func<Task> readNext = null;
			readNext = () =>
			{
				if (size <= 0)
					return TaskFromResult (0);
				return socket.RecieveAsync (buffer, 0, (int)Math.Min (bufferSize, size)).ContinueWith (t =>
				{
					t.PropagateExceptions ();
					var read = t.Result;
					if (read <= 0)
						throw new IOException ("Unexpected end of stream");
					size -= read;
					to.Write (buffer, 0, read);

					return readNext ();
				}).Unwrap ();


			};
			return readNext ();
		}

		public static Task<T> TaskFromResult<T> (T value)
		{
			var tcs = new TaskCompletionSource<T> ();
			tcs.SetResult (value);
			return tcs.Task;
		}

		public static Task<byte[]> ReadExactly (ISocket stream, int size)
		{
			var ms = new MemoryStream (size);
			return ReadDataAsync (stream, ms, size, size).ContinueWith (t =>
			{
				t.PropagateExceptions (); 
				return TaskFromResult (ms.ToArray ());
			}).Unwrap ();
		}

		public static Task WriteDataAsync (ISocket socket, Stream from, long size, int bufferSize = 8192)
		{
			var buffer = new byte[bufferSize];
			Func<Task> writeNext = null;
			writeNext = () =>
			{
				if (size <= 0)
					return TaskFromResult (0);
				var currentChunkLength = from.Read (buffer, 0, bufferSize);
				if (currentChunkLength <= 0) //Finished
					return TaskFromResult (0);
				return WriteDataAsync (socket, buffer, currentChunkLength)
					.ContinueWith (t =>
					{
						t.PropagateExceptions ();
						return writeNext ();
					}).Unwrap ();

			};
			return writeNext ();
		}

		public static Task WriteDataAsync (ISocket socket, byte[] buffer, int size = -1)
		{
			if (size == -1)
				size = buffer.Length;
			int offset = 0;

			Func<Task> writeNext = null;
			writeNext = () =>
				{
					if (size <= 0)
						return TaskFromResult (0);
					return socket.SendAsync (buffer, offset, size).ContinueWith (t =>
						{
							t.PropagateExceptions ();
							var written = t.Result;
							size -= written;
							offset += written;
							return writeNext ();
						}).Unwrap ();
				};
			return writeNext ();
		}

		public static void PropagateExceptions (this Task task)
		{
			if (task == null)
				throw new ArgumentNullException ("task");
			if (!task.IsCompleted)
				throw new InvalidOperationException ("The task has not completed yet.");

			if (task.IsFaulted)
				task.Wait ();
		}


	}
}
