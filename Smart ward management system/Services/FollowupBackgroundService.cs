namespace Smart_ward_management_system.Services
{
    public class FollowUpBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FollowUpBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromHours(1); // Check every hour

        public FollowUpBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<FollowUpBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_period);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Starting follow-up check at: {time}", DateTime.UtcNow);

                    using var scope = _serviceProvider.CreateScope();
                    var followUpService = scope.ServiceProvider.GetRequiredService<IFollowUpService>();

                    await followUpService.CheckAndProcessOverdueItems();

                    _logger.LogInformation("Completed follow-up check at: {time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing follow-up checks");
                }
            }
        }
    }
}
