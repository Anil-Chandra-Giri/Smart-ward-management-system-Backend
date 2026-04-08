using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Model.Polls;
using Smart_ward_management_system.Services;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PollController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoggingService _logger;

        public PollController(ApplicationDbContext context, ILoggingService logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching poll categories",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId });

                var categories = await _context.PollCategories
                    .Where(c => c.IsActive)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {categories.Count} poll categories",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, Count = categories.Count });

                return Ok(categories);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching poll categories",
                    ex,
                    LogCategory.Polls,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "An error occurred while fetching categories." });
            }
        }

        // GET: api/<PollController>/active
        [HttpGet("active")]
        public async Task<IActionResult> GetActivePolls()
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching active polls",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId });

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
                        })
                    })
                    .ToListAsync();
            //var polls = await _context.Polls
            //    .Include(p => p.Options)
            //    .Where(p => p.IsActive &&
            //                p.StartDate <= now &&
            //                (p.EndDate == null || p.EndDate >= now))
            //    .Select(p => new
            //    {
            //        p.Id,
            //        p.Title,
            //        p.Description,
            //        Options = p.Options!.Select(o => new
            //        {
            //            o.Id,
            //            o.OptionText
            //        }),
            //        p.StartDate,
            //        p.EndDate
            //    })
            //    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {polls.Count} active polls",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, Count = polls.Count });

                return Ok(polls);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching active polls",
                    ex,
                    LogCategory.Polls,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "An error occurred while fetching active polls." });
            }
        }

        // GET api/<PollController>/poll/{id}
        [HttpGet("poll/{id}")]
        public async Task<IActionResult> GetPollById(Guid id)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching poll by ID: {id}",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = id });

                var poll = await _context.Polls
                    .Include(p => p.Options)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (poll == null)
                {
                    await _logger.LogWarningAsync($"Poll not found with ID: {id}",
                        LogCategory.Polls,
                        new { CorrelationId = correlationId, PollId = id });
                    return NotFound(new { message = "Poll not found" });
                }

                await _logger.LogInfoAsync($"Retrieved poll {id}: {poll.Title}",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = id, Title = poll.Title });

                return Ok(poll);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching poll by ID: {id}",
                    ex,
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = id });
                return StatusCode(500, new { message = "An error occurred while fetching the poll." });
            }
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] PollCategory category)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Creating new poll category: {category.Name}",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, CategoryName = category.Name });

                category.Id = Guid.NewGuid();
                category.CreatedDate = DateTime.UtcNow;
                category.IsActive = true;

                _context.PollCategories.Add(category);
                await _context.SaveChangesAsync();

                await _logger.LogInfoAsync($"Poll category created successfully with ID: {category.Id}",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, CategoryId = category.Id, CategoryName = category.Name });

                return Ok(category);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error creating poll category: {category?.Name}",
                    ex,
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, CategoryName = category?.Name });
                return StatusCode(500, new { message = "An error occurred while creating the category." });
            }
        }

        // POST api/<PollController>/create
        [HttpPost("create")]
        public async Task<IActionResult> CreatePoll(CreatePollDto dto)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                var userId = User.FindFirst("UserId")?.Value ?? "WardOfficer";

                await _logger.LogInfoAsync($"Creating new poll: {dto.Title}",
                    LogCategory.Polls,
                    new
                    {
                        CorrelationId = correlationId,
                        Title = dto.Title,
                        CategoryId = dto.CategoryId,
                        StartDate = dto.StartDate,
                        EndDate = dto.EndDate,
                        OptionsCount = dto.Options?.Count ?? 0,
                        CreatedBy = userId
                    });

                var poll = new Poll
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title,
                    Description = dto.Description,
                    CategoryId = dto.CategoryId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = userId,
                    IsActive = true
                };

                _context.Polls.Add(poll);
                await _context.SaveChangesAsync();

                var options = dto.Options.Select(o => new PollOption
                {
                    Id = Guid.NewGuid(),
                    PollId = poll.Id,
                    OptionText = o
                }).ToList();

                _context.PollOptions.AddRange(options);
                await _context.SaveChangesAsync();

                // Log the poll creation
                await _logger.LogPollActionAsync(poll.Id, "created");

                await _logger.LogInfoAsync($"Poll created successfully with ID: {poll.Id}, Options: {options.Count}",
                    LogCategory.Polls,
                    new
                    {
                        CorrelationId = correlationId,
                        PollId = poll.Id,
                        Title = poll.Title,
                        OptionsCount = options.Count,
                        CreatedBy = userId
                    });

                return Ok(new { Message = "Poll Created Successfully", PollId = poll.Id });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error creating poll: {dto?.Title}",
                    ex,
                    LogCategory.Polls,
                    new
                    {
                        CorrelationId = correlationId,
                        Title = dto?.Title,
                        ErrorMessage = ex.Message
                    });
                return StatusCode(500, new { message = "An error occurred while creating the poll." });
            }
        }

        [HttpPost("vote")]
        public async Task<IActionResult> Vote(VoteDto dto)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Vote attempt for poll: {dto.PollId}, Citizen: {dto.CitizenId}",
                    LogCategory.Polls,
                    new
                    {
                        CorrelationId = correlationId,
                        PollId = dto.PollId,
                        CitizenId = dto.CitizenId,
                        OptionId = dto.OptionId
                    });

                // Check if poll exists and is active
                var poll = await _context.Polls.FindAsync(dto.PollId);
                if (poll == null)
                {
                    await _logger.LogWarningAsync($"Poll not found for voting: {dto.PollId}",
                        LogCategory.Polls,
                        new { CorrelationId = correlationId, PollId = dto.PollId });
                    return BadRequest(new { Message = "Poll not found." });
                }

                if (!poll.IsActive)
                {
                    await _logger.LogWarningAsync($"Inactive poll voting attempt: {dto.PollId}",
                        LogCategory.Polls,
                        new { CorrelationId = correlationId, PollId = dto.PollId });
                    return BadRequest(new { Message = "This poll is no longer active." });
                }

                var now = DateTime.UtcNow;
                if (poll.StartDate > now)
                {
                    await _logger.LogWarningAsync($"Poll not started yet: {dto.PollId}",
                        LogCategory.Polls,
                        new { CorrelationId = correlationId, PollId = dto.PollId, StartDate = poll.StartDate });
                    return BadRequest(new { Message = "This poll has not started yet." });
                }

                if (poll.EndDate.HasValue && poll.EndDate < now)
                {
                    await _logger.LogWarningAsync($"Poll has ended: {dto.PollId}",
                        LogCategory.Polls,
                        new { CorrelationId = correlationId, PollId = dto.PollId, EndDate = poll.EndDate });
                    return BadRequest(new { Message = "This poll has ended." });
                }

                bool alreadyVoted = await _context.PollVotes
                    .AnyAsync(v => v.PollId == dto.PollId && v.CitizenId == dto.CitizenId);

                if (alreadyVoted)
                {
                    await _logger.LogWarningAsync($"Citizen already voted in poll: {dto.PollId}, Citizen: {dto.CitizenId}",
                        LogCategory.Polls,
                        new { CorrelationId = correlationId, PollId = dto.PollId, CitizenId = dto.CitizenId });
                    return BadRequest(new { Message = "You already voted." });
                }

                var vote = new PollVote
                {
                    Id = Guid.NewGuid(),
                    PollId = dto.PollId,
                    OptionId = dto.OptionId,
                    CitizenId = dto.CitizenId,
                    VotedOn = DateTime.UtcNow
                };

                _context.PollVotes.Add(vote);
                await _context.SaveChangesAsync();

                await _logger.LogInfoAsync($"Vote submitted successfully for poll: {dto.PollId}, Citizen: {dto.CitizenId}",
                    LogCategory.Polls,
                    new
                    {
                        CorrelationId = correlationId,
                        PollId = dto.PollId,
                        CitizenId = dto.CitizenId,
                        OptionId = dto.OptionId,
                        VoteId = vote.Id
                    });

                // Log citizen action
                await _logger.LogCitizenActionAsync(
                    dto.CitizenId.ToString(),
                    $"Voted in poll: {poll.Title}",
                    "Poll Voting"
                );

                return Ok(new { Message = "Vote Submitted", VoteId = vote.Id });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error submitting vote for poll: {dto?.PollId}, Citizen: {dto?.CitizenId}",
                    ex,
                    LogCategory.Polls,
                    new
                    {
                        CorrelationId = correlationId,
                        PollId = dto?.PollId,
                        CitizenId = dto?.CitizenId,
                        ErrorMessage = ex.Message
                    });
                return StatusCode(500, new { message = "An error occurred while submitting your vote." });
            }
        }

        [HttpGet("{pollId}/results")]
        public async Task<IActionResult> GetResults(Guid pollId)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching results for poll: {pollId}",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = pollId });

                var poll = await _context.Polls.FindAsync(pollId);
                if (poll == null)
                {
                    await _logger.LogWarningAsync($"Poll not found for results: {pollId}",
                        LogCategory.Polls,
                        new { CorrelationId = correlationId, PollId = pollId });
                    return NotFound(new { message = "Poll not found" });
                }

                var results = await _context.PollOptions
                    .Where(o => o.PollId == pollId)
                    .Select(o => new
                    {
                        Option = o.OptionText,
                        Votes = _context.PollVotes.Count(v => v.OptionId == o.Id),
                        Percentage = 0.0 // Will calculate after getting total
                    })
                    .ToListAsync();

                var totalVotes = results.Sum(r => r.Votes);

                // Calculate percentages
                var resultsWithPercentage = results.Select(r => new
                {
                    r.Option,
                    r.Votes,
                    Percentage = totalVotes > 0 ? Math.Round((double)r.Votes / totalVotes * 100, 2) : 0
                }).ToList();

                var response = new
                {
                    PollId = pollId,
                    PollTitle = poll.Title,
                    TotalVotes = totalVotes,
                    Results = resultsWithPercentage
                };

                await _logger.LogInfoAsync($"Retrieved results for poll {pollId}: {totalVotes} total votes",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = pollId, TotalVotes = totalVotes });

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching results for poll: {pollId}",
                    ex,
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = pollId });
                return StatusCode(500, new { message = "An error occurred while fetching poll results." });
            }
        }

        [HttpGet("my-votes/{citizenId}")]
        public async Task<IActionResult> GetCitizenVotes(string citizenId)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching votes for citizen: {citizenId}",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, CitizenId = citizenId });

                var votes = await _context.PollVotes
                    .Where(v => v.CitizenId == citizenId)
                    .Include(v => v.Poll)
                    .Include(v => v.Option)
                    .Select(v => new
                    {
                        v.PollId,
                        PollTitle = v.Poll!.Title,
                        v.OptionId,
                        OptionText = v.Option!.OptionText,
                        v.VotedOn
                    })
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {votes.Count} votes for citizen {citizenId}",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, CitizenId = citizenId, Count = votes.Count });

                return Ok(votes);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching votes for citizen: {citizenId}",
                    ex,
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, CitizenId = citizenId });
                return StatusCode(500, new { message = "An error occurred while fetching citizen votes." });
            }
        }

        // PUT api/<PollController>/deactivate/{pollId}
        [HttpPut("deactivate/{pollId}")]
        public async Task<IActionResult> DeactivatePoll(Guid pollId)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogWarningAsync($"Attempting to deactivate poll: {pollId}",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = pollId });

                var poll = await _context.Polls.FindAsync(pollId);

                if (poll == null)
                {
                    await _logger.LogWarningAsync($"Poll not found for deactivation: {pollId}",
                        LogCategory.Polls,
                        new { CorrelationId = correlationId, PollId = pollId });
                    return NotFound(new { message = "Poll not found" });
                }

                poll.IsActive = false;
                await _context.SaveChangesAsync();

                await _logger.LogPollActionAsync(pollId, "deactivated");

                await _logger.LogWarningAsync($"Poll deactivated: {pollId} - Title: {poll.Title}",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = pollId, Title = poll.Title });

                return Ok(new { Message = "Poll Deactivated", PollId = pollId });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error deactivating poll: {pollId}",
                    ex,
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = pollId });
                return StatusCode(500, new { message = "An error occurred while deactivating the poll." });
            }
        }

        // PUT api/<PollController>/activate/{pollId}
        [HttpPut("activate/{pollId}")]
        public async Task<IActionResult> ActivatePoll(Guid pollId)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Attempting to activate poll: {pollId}",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = pollId });

                var poll = await _context.Polls.FindAsync(pollId);

                if (poll == null)
                {
                    await _logger.LogWarningAsync($"Poll not found for activation: {pollId}",
                        LogCategory.Polls,
                        new { CorrelationId = correlationId, PollId = pollId });
                    return NotFound(new { message = "Poll not found" });
                }

                poll.IsActive = true;
                await _context.SaveChangesAsync();

                await _logger.LogPollActionAsync(pollId, "activated");

                await _logger.LogInfoAsync($"Poll activated: {pollId} - Title: {poll.Title}",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = pollId, Title = poll.Title });

                return Ok(new { Message = "Poll Activated", PollId = pollId });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error activating poll: {pollId}",
                    ex,
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = pollId });
                return StatusCode(500, new { message = "An error occurred while activating the poll." });
            }
        }

        // GET: api/<PollController>/statistics
        [HttpGet("statistics")]
        public async Task<IActionResult> GetPollStatistics()
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching poll statistics",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId });

                var totalPolls = await _context.Polls.CountAsync();
                var activePolls = await _context.Polls.CountAsync(p => p.IsActive);
                var totalVotes = await _context.PollVotes.CountAsync();
                var totalCategories = await _context.PollCategories.CountAsync(c => c.IsActive);

                var pollsByCategory = await _context.Polls
                    .GroupBy(p => p.CategoryId)
                    .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var statistics = new
                {
                    TotalPolls = totalPolls,
                    ActivePolls = activePolls,
                    TotalVotes = totalVotes,
                    TotalCategories = totalCategories,
                    PollsByCategory = pollsByCategory,
                    AverageVotesPerPoll = totalPolls > 0 ? Math.Round((double)totalVotes / totalPolls, 2) : 0
                };

                await _logger.LogInfoAsync($"Poll statistics retrieved: {totalPolls} polls, {totalVotes} votes",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, TotalPolls = totalPolls, TotalVotes = totalVotes });

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching poll statistics",
                    ex,
                    LogCategory.Polls,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "An error occurred while fetching poll statistics." });
            }
        }

        // DELETE api/<PollController>/delete/{id}
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeletePoll(Guid id)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogWarningAsync($"Attempting to delete poll: {id}",
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = id });

                var poll = await _context.Polls
                    .Include(p => p.Options)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (poll == null)
                {
                    await _logger.LogWarningAsync($"Poll not found for deletion: {id}",
                        LogCategory.Polls,
                        new { CorrelationId = correlationId, PollId = id });
                    return NotFound(new { message = "Poll not found" });
                }

                // Delete associated votes first
                var votes = await _context.PollVotes.Where(v => v.PollId == id).ToListAsync();
                _context.PollVotes.RemoveRange(votes);

                // Delete options
                if (poll.Options != null)
                    _context.PollOptions.RemoveRange(poll.Options);

                // Delete poll
                _context.Polls.Remove(poll);

                await _context.SaveChangesAsync();

                await _logger.LogWarningAsync($"Poll deleted: {id} - Title: {poll.Title}, Votes deleted: {votes.Count}",
                    LogCategory.Polls,
                    new
                    {
                        CorrelationId = correlationId,
                        PollId = id,
                        Title = poll.Title,
                        OptionsDeleted = poll.Options?.Count ?? 0,
                        VotesDeleted = votes.Count
                    });

                return Ok(new { message = "Poll deleted successfully" });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error deleting poll: {id}",
                    ex,
                    LogCategory.Polls,
                    new { CorrelationId = correlationId, PollId = id });
                return StatusCode(500, new { message = "An error occurred while deleting the poll." });
            }
        }
    }
}