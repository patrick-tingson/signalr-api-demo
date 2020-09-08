using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using SignalR_API_Demo.Extention;
using SignalR_API_Demo.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR_API_Demo.Hubs
{
    //[Authorize("NotificationSubscriber")]
    //[AuthorizeUser(Method = "signalr", Actions = new[] { Actions.get })]
    [Authorize]
    public class StronglyTypeNotificationHub : Hub<INotificationClient>
    {
        //private static HashSet<string> ConnectedIds = new HashSet<string>();
        private static List<Subscriber> Subscribers = new List<Subscriber>();

        public override async Task OnConnectedAsync()
        {
            if (!ClaimsValidation())
            {
                throw new HubException("403 (Forbidden)");
            }

            //ConnectedIds.Add(Context.UserIdentifier);

            var subscriberData = Subscribers.FirstOrDefault(r => r.GlobalId == Context.UserIdentifier);

            if (subscriberData != null)
            {
                subscriberData.ConnectionId.Add(Context.ConnectionId);
            }
            else
            {
                Subscribers.Add(new Subscriber
                {
                    ConnectionId = new List<string>() { Context.ConnectionId },
                    GlobalId = Context.UserIdentifier
                });
            }

            Console.WriteLine($"{Context.UserIdentifier} is now connected");
            Console.WriteLine($"Total connected users {Subscribers.Count}");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            //if (ConnectedIds.Contains(Context.UserIdentifier))
            //{
            //    ConnectedIds.Remove(Context.UserIdentifier);
            //}

            var subscriberData = Subscribers.FirstOrDefault(r => r.GlobalId == Context.UserIdentifier);

            if (subscriberData != null)
            {
                if (subscriberData.ConnectionId.Count > 1)
                {
                    subscriberData.ConnectionId.Remove(Context.ConnectionId);
                }
                else
                {
                    Subscribers.Remove(subscriberData);
                }
            }

            Console.WriteLine($"{Context.UserIdentifier} is now disconnected");
            Console.WriteLine($"Total connected users {Subscribers.Count}");

            await base.OnDisconnectedAsync(exception);
        }

        [HubMethodName("ClientMessageToAll")]
        public Task ClientMessage(string message)
        {
            var x = Context;

            return Clients.All.ReceiveMessage(message);
        }

        [HubMethodName("ClientToClientMessage")]
        public Task ClientMessageToAnotherClient(string user, string message)
        {
            return Clients.User(user).ReceiveMessage(message);
        }

        public IEnumerable<Subscriber> GetAllSubscribers()
        {
            return Subscribers;
        }

        private bool ClaimsValidation()
        {
            var userRights = Context.User.Claims.Where(r => r.Type.Equals("rights"));

            if (userRights.Any(r => r.Value.ToLower().Contains("signalr") && r.Value.ToLower().Contains("get")))
            {
                return true;
            }

            return false;
        }
    }
}
