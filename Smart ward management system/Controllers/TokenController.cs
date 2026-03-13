using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IHubContext<QueueHub> _hubContext;
        private readonly ApplicationDbContext _context;
        public TokenController(IHubContext<QueueHub> hubContext,ApplicationDbContext context)
        {
            _hubContext = hubContext;
            _context = context;
        }
        [HttpPost("next/{wardId}")]
        public async Task<IActionResult> CallNext()
        {
            var nextToken = await _context.Tokens
                .Where(t=>t.Status == "Pending")
                .OrderBy(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (nextToken != null)
            {
                nextToken.Status = "Calling";
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("ReceiveQueueUpdate", nextToken.TokenNumber);
            }
            return Ok(nextToken);
        }
    }
}
