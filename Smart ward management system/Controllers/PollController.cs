using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model.Polls;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PollController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public PollController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.PollCategories
                .Where(c => c.IsActive)
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/<PollController>
        [HttpGet("active")]
        public async Task<IActionResult> GetActivePolls()
        {
            var now = DateTime.UtcNow;

            var polls = await _context.Polls
                .Include(p => p.Options)
                .Where(p => p.IsActive &&
                            p.StartDate <= now &&
                            (p.EndDate == null || p.EndDate >= now))
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Description,
                    Options = p.Options!.Select(o => new
                    {
                        o.Id,
                        o.OptionText
                    }),
                    p.StartDate,
                    p.EndDate
                })
                .ToListAsync();

            return Ok(polls);
        }

        // GET api/<PollController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] PollCategory category)
        {
            _context.PollCategories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(category);
        }

        // POST api/<PollController>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePoll(CreatePollDto dto)
        {
            var poll = new Poll
            {
                Title = dto.Title,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "WardOfficer"
            };

            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var options = dto.Options.Select(o => new PollOption
            {
                PollId = poll.Id,
                OptionText = o
            }).ToList();

            _context.PollOptions.AddRange(options);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Poll Created Successfully" });
        }

        [HttpPost("vote")]
        public async Task<IActionResult> Vote(VoteDto dto)
        {
            bool alreadyVoted = await _context.PollVotes
                .AnyAsync(v => v.PollId == dto.PollId && v.CitizenId == dto.CitizenId);

            if (alreadyVoted)
                return BadRequest(new { Message = "You already voted." });

            var vote = new PollVote
            {
                PollId = dto.PollId,
                OptionId = dto.OptionId,
                CitizenId = dto.CitizenId,
                VotedOn = DateTime.UtcNow
            };

            _context.PollVotes.Add(vote);

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Vote Submitted" });
        }

        [HttpGet("{pollId}/results")]
        public async Task<IActionResult> GetResults(Guid pollId)
        {
            var results = await _context.PollOptions
                .Where(o => o.PollId == pollId)
                .Select(o => new
                {
                    Option = o.OptionText,
                    Votes = _context.PollVotes.Count(v => v.OptionId == o.Id)
                })
                .ToListAsync();

            return Ok(results);
        }



        // PUT api/<PollController>/5
        [HttpPut("deactivate/{pollId}")]
        public async Task<IActionResult> DeactivatePoll(int pollId)
        {
            var poll = await _context.Polls.FindAsync(pollId);

            if (poll == null)
                return NotFound();

            poll.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Poll Deactivated" });
        }

        // DELETE api/<PollController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
