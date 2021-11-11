using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using PdNetwork.Sockets.Server;
using PdNetwork.Sockets.Server.Objects;
using UnityEngine;

namespace PdNetwork.Sockets.Impl.Server
{
	public class TcpSocketServerLayer : ISocketServerLayer, IDisposable
	{
		private Thread _thrServer;
		private TcpListener _tcpListener;
		private IPEndPoint _sendEndPoint;
		private Thread _thrSocketReader;
		private bool _active;
		private int _port;
		private byte _peerIdCount = 1;

		public Action OnStarted { get; set; }
		public Action<byte> OnPeerConnect { get; set; }
		public Action<byte, string> OnPeerDisconnect { get; set; }
		public Action<string> OnDisconnect { get; set; }
		public Action<string, SocketError> OnError { get; set; }

		private readonly Dictionary<byte, ClientPeerObject> _clients = new Dictionary<byte, ClientPeerObject>();
		private readonly Dictionary<byte, Thread> _clientsThreads = new Dictionary<byte, Thread>();

		public Action<byte[]> OnData { get; set; }

		public void Dispose()
		{
			if (_active)
			{
				_active = false;
				_tcpListener.Stop();
			}

			_thrServer?.Abort();
			_thrSocketReader?.Abort();
		}

		public void Start(int port)
		{
			_port = port;
			
			if (_active)
				return;
			
			try
			{
				_tcpListener = new TcpListener(IPAddress.Any, _port);

				var localPort = ((IPEndPoint) _tcpListener.LocalEndpoint).Port;
				
				_thrSocketReader = new Thread(Read)
				{
					Name = "SocketThreadv4(" + localPort + ")",
					IsBackground = true
				};
				_thrSocketReader.Start(_tcpListener);
				_active = true;
				OnStarted?.Invoke();
			}
			catch (SocketException ex)
			{
				HandleError("Connection error: " + ex.Message, ex.StackTrace, ex.SocketErrorCode);
			}
			catch (Exception ex)
			{
				HandleError("General exception on connection: " + ex.Message + " " + ex.StackTrace);
			}
		}

		private void Read(object state)
		{
			var listener = (TcpListener) state;
			EndPoint bufferEndPoint = new IPEndPoint(IPAddress.Any, 0);
			while (_active)
			{
				try
				{
					var acceptTcpClient = listener.AcceptTcpClient();

					var peerId = _peerIdCount++;
					var clientPeerObject = new ClientPeerObject(acceptTcpClient, peerId);
					PeerConnected(peerId, clientPeerObject);
					Thread clientThread = new Thread(() =>
					{
						clientPeerObject.Stream = acceptTcpClient.GetStream();
						var receiveBuffer = new byte[1500];
						while (true)
						{
							try
							{
								var bytesLength = clientPeerObject.Stream.Read(receiveBuffer, 0, receiveBuffer.Length);
								HandleBinaryData(receiveBuffer, bytesLength);
							}
							catch
							{
								PeerDisconnected(clientPeerObject.PeerId, "Disconnected by peer");
							}
						}

//						clientPeerObject.Process(HandleBinaryData(receiveBuffer, receiveBuffer.Length));
					});
					_clientsThreads.Add(peerId, clientThread);
					clientThread.Start();
				}
				catch (SocketException ex)
				{
					if (!string.IsNullOrEmpty(ex.Message))
					{
						HandleError("General error reading data from TcpListener: " + ex.Message, ex.StackTrace);
					}

					break;
				}
			}
		}

		private void PeerConnected(byte peerId, ClientPeerObject peer)
		{
			_clients.Add(peerId, peer);
			OnPeerConnect?.Invoke(peerId);
		}

		private void PeerDisconnected(byte peerId, string reason)
		{
			var clientPeerObject = _clients[peerId];
			clientPeerObject?.Close();
			_clients.Remove(peerId);
			
			_clientsThreads[peerId].Abort();
			_clientsThreads.Remove(peerId);
			
			OnPeerDisconnect?.Invoke(peerId, reason);
		}

		private void HandleBinaryData(byte[] buf, int size)
		{
			var data = new byte[size];
			Buffer.BlockCopy(buf, 0, data, 0, size);
			OnData?.Invoke(data);
		}

		public void Disconnect() => Disconnect("Server disconnected");

		public void Disconnect(string reason)
		{
			try
			{
				_tcpListener.Stop();
				_active = false;

				foreach (var client in _clients.Values) 
					client.Close();
				_clients.Clear();

				foreach (var thread in _clientsThreads.Values) 
					thread.Abort();
				_clientsThreads.Clear();

				_thrServer.Abort();
				_thrSocketReader.Abort();
				UnityEngine.Debug.Log($"[{nameof(TcpSocketServerLayer)}] Server disconnected, reason: {reason}");
			}
			catch (Exception ex)
			{
				LogError(ex.Message, ex.StackTrace);
			}
		}

		public void DisconnectPeer(byte peer, string reason)
		{
			_clients[peer].Close();
			_clients.Remove(peer);
			
			_clientsThreads[peer].Abort();
			_clientsThreads.Remove(peer);
			OnPeerDisconnect?.Invoke(peer, reason);
		}

		public void Write(byte[] data) => WriteToClients(_clients.Keys, data);
		public void Write(byte[] recipients, byte[] data) => WriteToClients(recipients, data);

		private void WriteToClients(IEnumerable<byte> recipients, byte[] data)
		{
			if (!_active)
				return;

			try
			{
				foreach (var recipient in recipients) 
					_clients[recipient].Stream.Write(data, 0, data.Length);
			}
			catch (SocketException ex)
			{
				HandleError("Error writing to socket: " + ex.Message, ex.StackTrace, ex.SocketErrorCode);
			}
			catch (Exception ex)
			{
				HandleError("General error writing to socket: " + ex.Message, ex.StackTrace);
			}
		}

		private void HandleError(string err, string stacktrace = null, SocketError se = SocketError.NotSocket)
		{
			LogError(err, stacktrace);
			OnError?.Invoke(err, se);
		}

		private void LogWarn(string msg)
		{
			Debug.LogWarning("[UdpSocketLayer] " + msg);
		}

		private void LogError(string msg, string stacktrace = null)
		{
			var error = stacktrace != null ? msg + ", \n" + stacktrace : msg;
			UnityEngine.Debug.LogError($"[{nameof(TcpSocketServerLayer)}] {error}");
		}
	}
}