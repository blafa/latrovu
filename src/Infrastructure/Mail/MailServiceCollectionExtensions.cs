using Latrovu.Core.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Latrovu.Infrastructure.Mail;

public static class MailServiceCollectionExtensions
{
    public static IServiceCollection AddMailInbox(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<MailOptions>()
            .Bind(configuration.GetSection(MailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IMailInbox, ImapInbox>();
        return services;
    }
}
