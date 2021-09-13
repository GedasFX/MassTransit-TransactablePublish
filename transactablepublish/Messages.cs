using System;
using System.Threading.Tasks;
using System.Transactions;
using GreenPipes;
using MassTransit;
using MassTransit.Transactions;
using Microsoft.Extensions.Logging;

namespace transactablepublish
{
    public class LaunchMessage
    {
        public string Content { get; set; }
    }

    public class CompletedMessage
    {
        public string Content { get; set; }
    }
    
    public class LaunchMessageConsumer : IConsumer<LaunchMessage>
    {
        private readonly ILogger<LaunchMessageConsumer> _logger;
        private readonly IPublishEndpoint _publisher;
        private readonly ITransactionalBus _transactionalBus;

        public LaunchMessageConsumer(ILogger<LaunchMessageConsumer> logger, IPublishEndpoint publisher, ITransactionalBus transactionalBus)
        {
            _logger = logger;
            _publisher = publisher;
            _transactionalBus = transactionalBus;
        }

        // IPublishEndpoint - No transaction - Publish right away
        // IPublishEndpoint - Committed transaction - Publish after commit
        // IPublishEndpoint - Uncommitted transaction - no not publish
        // IPublishEndpoint - throw during transaction - no not publish
        
        public async Task Consume(ConsumeContext<LaunchMessage> context)
        {
            _logger.LogInformation("Consumed: {Message}", context.Message.Content);
            
            await context.Publish(new CompletedMessage { Content = context.Message.Content });
            _logger.LogInformation("published");

            // await _transactionalBus.Release();
            _logger.LogInformation("released");
        }
        
        
        
        // ConsumeContext.Publish - do nothing - Publish after receive
        // ConsumeContext.Publish - rollback - do not publish
        
        // public async Task Consume(ConsumeContext<LaunchMessage> context)
        // {
        //     var transactionContext = context.GetPayload<TransactionContext>();
        //     
        //     _logger.LogInformation("Consumed: {Message}", context.Message.Content);
        //     await context.Publish(new CompletedMessage { Content = context.Message.Content });
        //     
        //     _logger.LogInformation("published");
        //
        //     await transactionContext.Commit();
        //     _logger.LogInformation("committed");
        //
        //     if (transactionContext.Transaction.TransactionInformation.Status == TransactionStatus.Aborted)
        //         throw new TransactionAbortedException();
        // }
    }
    
    public class CompletedMessageConsumer : IConsumer<CompletedMessage>
    {
        private readonly ILogger<CompletedMessageConsumer> _logger;

        public CompletedMessageConsumer(ILogger<CompletedMessageConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<CompletedMessage> context)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Completed!");
            Console.ResetColor();
            _logger.LogError("Completed: {Message}", context.Message.Content);
            
            return Task.CompletedTask;
        }
    }

}