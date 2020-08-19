using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using dotnet_framework_server.Services;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;

namespace dotnet_framework_server
{
	public class ChatHub : Hub
	{
		private readonly WebSocketManager webSocketManager = WebSocketManager.Instance;

		public void Hello()
		{
			Clients.All.hello();
		}

		public void Send(string name, string message)
		{
			webSocketManager.BroadcastMessage(name, message);
		}

		public override Task OnDisconnected(bool stopCalled)
		{
			string connectionId = Context.ConnectionId;
			webSocketManager.BroadcastMessage(connectionId, "discounted!");
			return base.OnDisconnected(stopCalled);
		}
	}
}