using Microsoft.AspNetCore.SignalR;

namespace GroupPlanner.Application.Hubs
{
    public class AlgorithmHub : Hub
    {
        public async System.Threading.Tasks.Task SendProgress(int percent)
        {
            await Clients.All.SendAsync("ReceiveProgress", percent);
        }
    }
}
