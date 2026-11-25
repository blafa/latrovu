using Latrovu.BuildingBlocks.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Retry.Simple;

namespace Latrovu.BuildingBlocks.Infrastructure.Messaging;

public static class MessagingSetup
{
    public static IServiceCollection AddMessagePublishing(this IServiceCollection services, IConfiguration configuration)
    {
        MessagingOptions options = BindOptions(services, configuration);
        services.AddOutbox();
        services.AddRebus(configure => configure
            .Transport(transport => transport.UseRabbitMqAsOneWayClient(options.RabbitMqConnectionString)));
        services.AddHostedService<OutboxForwarder>();
        return services;
    }

    public static IServiceCollection AddMessageConsuming(
        this IServiceCollection services,
        IConfiguration configuration,
        string inputQueue,
        IReadOnlyList<Type> subscriptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputQueue);
        ArgumentNullException.ThrowIfNull(subscriptions);

        MessagingOptions options = BindOptions(services, configuration);
        services.AddOutbox();
        services.AddRebus(
            configure => configure
                .Transport(transport => transport.UseRabbitMq(options.RabbitMqConnectionString, inputQueue))
                .Options(rebus => rebus.RetryStrategy(errorQueueName: options.ErrorQueue, maxDeliveryAttempts: options.MaxDeliveryAttempts)),
            onCreated: bus => SubscribeAsync(bus, subscriptions));
        services.AddHostedService<OutboxForwarder>();
        return services;
    }

    public static IServiceCollection AddMessageHandler<TMessage, THandler>(this IServiceCollection services)
        where THandler : class, IMessageHandler<TMessage>
    {
        services.AddScoped<IMessageHandler<TMessage>, THandler>();
        services.AddRebusHandler<RebusMessageHandler<TMessage>>();
        return services;
    }

    public static IServiceCollection AddOutboxCleanup(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<OutboxCleanupWorker>();
        return services;
    }

    private static IServiceCollection AddOutbox(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IOutbox, PostgresOutbox>();
        services.AddHostedService<OutboxSchema>();
        return services;
    }

    private static MessagingOptions BindOptions(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection section = configuration.GetSection(MessagingOptions.SectionName);
        services.AddOptions<MessagingOptions>().Bind(section).ValidateDataAnnotations().ValidateOnStart();
        return section.Get<MessagingOptions>() ?? throw new InvalidOperationException($"{MessagingOptions.SectionName} is missing");
    }

    private static async Task SubscribeAsync(IBus bus, IReadOnlyList<Type> subscriptions)
    {
        foreach (Type messageType in subscriptions)
        {
            await bus.Subscribe(messageType);
        }
    }
}
