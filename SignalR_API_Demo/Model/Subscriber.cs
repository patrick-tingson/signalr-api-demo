using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR_API_Demo.Model
{
    public class Subscriber
    {
        public string GlobalId { get; set; }
        public List<string> ConnectionId { get; set; }
    }
}
