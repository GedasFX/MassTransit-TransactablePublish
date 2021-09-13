using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace transactablepublish
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureServices);

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddMassTransit(c =>
            {
                c.AddTransactionalBus();
                
                c.AddConsumer<LaunchMessageConsumer>();
                c.AddConsumer<CompletedMessageConsumer>();
                c.UsingRabbitMq((context, configurator) =>
                {
                    configurator.Host("amqp://username:password@localhost");

                    configurator.UseInMemoryOutbox();
                    configurator.ReceiveEndpoint(new TemporaryEndpointDefinition(), rc =>
                    {
                        rc.Consumer<LaunchMessageConsumer>(context);
                        rc.Consumer<CompletedMessageConsumer>(context);
                    });
                });
            });
            services.AddMassTransitHostedService();
            services.AddHostedService<Worker>();
        }
    }
}
