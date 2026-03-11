using Microsoft.AspNetCore.SignalR;

namespace Smart_ward_management_system.Common
{
    public class RealTimeHub : Hub
    {
        // Method to send token update to all connected clients
        public async Task SendTokenUpdate(string tokenNumber, string status)
        {
            await Clients.All.SendAsync("ReceiveTokenUpdate", tokenNumber, status);
        }
    }
}
