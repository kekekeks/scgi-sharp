using System;
using Oars;
using System.Collections.Generic;
using ScgiSharp.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ScgiSharp.OarsIo
{
	public class OarsSocketListener : ISocketListener
	{
		internal Oars.EventBase _eventBase;
		Oars.ConnectionListener _listener;
		public OarsSocketListener ()
		{
			_eventBase = new EventBase ();
		}



		Queue<Action> _syncQueue = new Queue<Action> ();

		void EventLoop ()
		{
			while (true)
			{
				lock (_syncQueue)
					while (_syncQueue.Count!=0)
						_syncQueue.Dequeue () ();
				_eventBase.Loop (LoopOptions.NonBlock | LoopOptions.Once);
			}
		}

		public void Sync (Action cb)
		{
			lock (_syncQueue)
				_syncQueue.Enqueue (cb);
		}


		Queue<TaskCompletionSource<ISocket>> _waitingForSockets = new Queue<TaskCompletionSource<ISocket>> ();
		Queue<ISocket> _acceptedSockets = new Queue<ISocket> ();
		bool _listening = false;

		public System.Threading.Tasks.Task<ISocket> AcceptSocket ()
		{
			var tcs = new TaskCompletionSource <ISocket> ();
			Sync (() =>
			{
				if (_acceptedSockets.Count != 0)
				{
					tcs.SetResult (_acceptedSockets.Dequeue ());
					return;
				}
				_waitingForSockets.Enqueue (tcs);
				if (!_listening)
				{
					_listener.Enable ();
					_listening = true;
				}

			});
			return tcs.Task;
		}

		void SocketAccepted (IntPtr socket, System.Net.IPEndPoint arg2)
		{
			var osocket = new OarsSocket (socket, this);
			if (_waitingForSockets.Count == 0)
				_acceptedSockets.Enqueue (osocket);
			else
				_waitingForSockets.Dequeue ().SetResult (osocket);
			if (_waitingForSockets.Count != 0)
				_listener.Disable ();
		}

		public void Listen (System.Net.IPAddress address, int port, int backlog)
		{
			_listener = new ConnectionListener (_eventBase, new System.Net.IPEndPoint (address, port), (short)backlog);
			_listener.ConnectionAccepted += SocketAccepted;
			_listener.Disable ();
			new Thread (EventLoop).Start ();
		}


	}
}

