using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Smart_ward_management_system.Controllers
{
    public class QueueHub : Hub
    {
        public async Task UpdateQueue( string currentToken)
        {
            await Clients.All.SendAsync("ReceiveQueueUpdate", currentToken);
        }
    }
}
