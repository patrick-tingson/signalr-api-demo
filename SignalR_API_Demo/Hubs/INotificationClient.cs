using SignalR_API_Demo.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR_API_Demo.Hubs
{
    public interface INotificationClient
    {
        Task ReceiveMessage(string message);
        Task ReceiveMessageObject(ServerMessage message);
    }
}
