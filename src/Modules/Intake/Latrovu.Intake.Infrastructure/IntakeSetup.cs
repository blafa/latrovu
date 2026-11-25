using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Latrovu.Intake.Infrastructure;

public static class IntakeSetup
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
        services.AddScoped<ISubmissionForwarder, SubmissionForwarder>();
        services.AddScoped<ISearchRequestForwarder, SearchRequestForwarder>();
        services.AddScoped<IAgentReplyForwarder, AgentReplyForwarder>();
        services.AddHostedService<MailPollingService>();
        return services;
    }
}
