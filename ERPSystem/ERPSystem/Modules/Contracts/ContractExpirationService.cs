namespace ERPSystem.Modules.Contracts
{
    public class ContractExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ContractExpirationService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();

                var contractService = scope.ServiceProvider
                    .GetRequiredService<ContractsService>();

                await contractService.ExpireContractsAsync();

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
