using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Text.Json;
using System.Drawing;

namespace dotnet_framework_server.Services
{
	public class WebSocketClient
	{
		public string name;
		public WebSocket webSocket;
	}

	public class WebSocketItem
	{
		public WebSocketCommand Command { get; set; }
		public string Name { get; set; }
		public string Message { get; set; }
	}

	public enum WebSocketCommand
	{
		Send
	}

	public sealed class WebSocketManager
	{
		// singleton
		private static readonly Lazy<WebSocketManager> _instance = new Lazy<WebSocketManager>(() => new WebSocketManager(GlobalHost.ConnectionManager.GetHubContext<ChatHub>().Clients));

		private static IHubConnectionContext<dynamic> Clients { get; set; }

		public static WebSocketManager Instance
		{
			get { return _instance.Value; }
		}

		private WebSocketManager(IHubConnectionContext<dynamic> clients)
		{
			Clients = clients;

			//HeartBeat();
		}

		public ConcurrentDictionary<string, WebSocketClient> WebSocketClients { get; } = new ConcurrentDictionary<string, WebSocketClient>();

		private void HeartBeat()
		{
			Task.Run(async () =>
			{
				for (; ; )
				{
					foreach (var client in WebSocketClients)
					{
						var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(client.Key));
						await client.Value.webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
					}

					await Task.Delay(1000);
				}
			});

		}

		public string AddClient(WebSocket webSocket)
		{
			string connectionID = Guid.NewGuid().ToString();

			WebSocketClient webSocketClient = new WebSocketClient
			{
				webSocket = webSocket,
				name = connectionID
			};

			WebSocketClients.TryAdd(connectionID, webSocketClient);

			Task.Run(async () =>
			{
				await SendClientAsync(webSocket, "ID:" + connectionID);
			});

			return connectionID;
		}

		public void RemoveClient(string connectionID)
		{
			WebSocketClients.TryRemove(connectionID, out _);
		}

		public async Task SendClientAsync(WebSocket webSocket, string message)
		{
			var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
			await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
		}

		public Task BroadcastMessage(string name, string message)
		{
			WebSocketItem webSocketItem = new WebSocketItem();
			webSocketItem.Command = WebSocketCommand.Send;
			webSocketItem.Name = name;
			webSocketItem.Message = message;

			string jsonMessage = JsonSerializer.Serialize<WebSocketItem>(webSocketItem);
			return BroadcastJsonMessage(jsonMessage);
		}

		public async Task BroadcastJsonMessage(string jsonMessage)
		{
			try
			{
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, };
				WebSocketItem webSocketItem = JsonSerializer.Deserialize<WebSocketItem>(jsonMessage, options);
				Clients.All.broadcastMessage(webSocketItem.Name, webSocketItem.Message);

				//var hello = WebSocketClients.Select(async x =>
				//{
				//	await x.Value.webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
				//});

				foreach (var client in WebSocketClients)
				{
					await client.Value.webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonMessage)), WebSocketMessageType.Text, true, CancellationToken.None);
				}
			}
			catch (Exception)
			{
			}
		}
	}
}