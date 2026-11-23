using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Latrovu.Intake.Infrastructure;

public static class IntakeModuleServiceCollectionExtensions
{
    public static IServiceCollection AddIntakeModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<MailOptions>()
            .Bind(configuration.GetSection(MailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IMailInbox, ImapInbox>();
        services.AddSingleton<IMailRouter, MailRouter>();
        services.AddScoped<IMailDispatcher, MailDispatcher>();
        services.AddHostedService<MailPollingService>();
        return services;
    }
}
