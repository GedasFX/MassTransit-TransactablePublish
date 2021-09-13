using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using MassTransit;
using MassTransit.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace transactablepublish
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _provider;

        public Worker(ILogger<Worker> logger, IServiceProvider provider)
        {
            _logger = logger;
            _provider = provider;
        }

        // IPublishEndpoint - No transaction - Publish right away
        // IPublishEndpoint - Committed transaction - Publish after commit
        // IPublishEndpoint - Uncommitted transaction - no not publish
        // IPublishEndpoint - throw during transaction - no not publish
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 1. ASP.NET (filters) -> ASP.NET (actions) -> MediatR (pipeline behavior) -> MediatR (command handlers)
            // 2. MassTransit consumer (filters) -> MassTransit consumer (consumers) -> MediatR (pipeline behavior) -> MediatR (command handlers)
            // 3. Hangfire -> ???? -> MediatR (pipeline behavior) -> MediatR (command handlers)

            _logger.LogInformation("Enter worker");

            await using var scope = _provider.CreateAsyncScope();
            var transactionalBus = scope.ServiceProvider.GetRequiredService<ITransactionalBus>();
            // await transactionalBus.Publish(new LaunchMessage { Content = "test" }, stoppingToken);
            // await transactionalBus.Release(); // Immediately after CommitAsync
            //
            
            var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            _logger.LogInformation("enter other transaction");
            await publisher.Publish(new LaunchMessage { Content = "test" }, stoppingToken);
            _logger.LogInformation("publish message");
            await transactionalBus.Release();
            
            // using(var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            // {
            //     await publisher.Publish(new LaunchMessage { Content = "test" }, stoppingToken);
            //     _logger.LogInformation("publish message");
            //     
            //     transaction.Complete();
            //     _logger.LogInformation("completed transaction");
            //
            //     await Task.Delay(TimeSpan.FromMilliseconds(1000), stoppingToken);
            //     _logger.LogInformation("dispose started");
            //
            // }
            // _logger.LogInformation("disposed");

            await Task.Delay(TimeSpan.FromMilliseconds(1000), stoppingToken);
            _logger.LogInformation("all done");
        }
    }
}
