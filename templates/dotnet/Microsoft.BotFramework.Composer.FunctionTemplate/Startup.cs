using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.BotFramework.Composer.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

[assembly: FunctionsStartup(typeof(Microsoft.BotFramework.Composer.FunctionTemplate.Startup))]

namespace Microsoft.BotFramework.Composer.FunctionTemplate
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var rootDirectory = Directory.GetParent(binDirectory).FullName;

            var config = new ConfigurationBuilder();
                //.SetBasePath(context.FunctionAppDirectory)
                //.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                //.AddEnvironmentVariables()
                //.Build();

            config
                .SetBasePath(rootDirectory)
                .AddJsonFile($"ComposerDialogs/settings/appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsetting.json", optional: true, reloadOnChange: true)
                .UseLuisConfigAdapter()
                .UseLuisSettings();


            if (Debugger.IsAttached)
            {
                // Local Debug
                config.AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true);
            }
            else
            {
                //Azure Deploy
                config.AddJsonFile("appsettings.deployment.json", optional: true, reloadOnChange: true);
                config.AddUserSecrets<Startup>();
            }

            config.AddEnvironmentVariables();

            var rootConfiguration = config.Build();
            var services = builder.Services;

            services.AddLogging();

            // Create the credential provider to be used with the Bot Framework Adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            services.AddSingleton<BotAdapter>(sp => (BotFrameworkHttpAdapter)sp.GetService<IBotFrameworkHttpAdapter>());

            // Register AuthConfiguration to enable custom claim validation.
            services.AddSingleton<AuthenticationConfiguration>();

            // Register the skills client and skills request handler.
            services.AddSingleton<SkillConversationIdFactoryBase, SkillConversationIdFactory>();
            services.AddHttpClient<BotFrameworkClient, SkillHttpClient>();
            services.AddSingleton<ChannelServiceHandler, SkillHandler>();

            // Storage
            services.AddSingleton<IStorage>(serviceProvider =>
            {
                //IConfiguration configuration = serviceProvider.GetService<IConfiguration>();

                var settings = new BotSettings();
                rootConfiguration.Bind(settings);

                IStorage storage = null;

                // Configure storage for deployment
                if (!string.IsNullOrEmpty(settings.CosmosDb.AuthKey))
                {
                    storage = new CosmosDbStorage(settings.CosmosDb);
                }
                else
                {
                    Console.WriteLine("The settings of CosmosDbStorage is incomplete, please check following settings: settings.CosmosDb");
                    storage = new MemoryStorage();
                }

                return storage;
            });

            // State
            services.AddSingleton<UserState>(serviceProvider => new UserState(serviceProvider.GetService<IStorage>()));
            services.AddSingleton<ConversationState>(serviceProvider => new ConversationState(serviceProvider.GetService<IStorage>()));

            // Resource explorer to track declarative assets
            var resourceExplorer = new ResourceExplorer().AddFolder(Path.Combine(rootDirectory, "ComposerDialogs"));
            services.AddSingleton(resourceExplorer);

            // Adapter
            services.AddSingleton<IBotFrameworkHttpAdapter, BotFrameworkHttpAdapter>(s =>
            {
                // Retrieve required dependencies
                //IConfiguration configuration = s.GetService<IConfiguration>();
                IStorage storage = s.GetService<IStorage>();
                UserState userState = s.GetService<UserState>();
                ConversationState conversationState = s.GetService<ConversationState>();

                // Register host configuration
                HostContext.Current.Set<IConfiguration>(rootConfiguration);

                // Bind settings
                var settings = new BotSettings();
                rootConfiguration.Bind(settings);

                var adapter = new BotFrameworkHttpAdapter(new ConfigurationCredentialProvider(rootConfiguration));

                adapter
                  .UseStorage(storage)
                  .UseState(userState, conversationState);

                if (!string.IsNullOrEmpty(settings.BlobStorage.ConnectionString) && !string.IsNullOrEmpty(settings.BlobStorage.Container))
                {
                    adapter.Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
                }
                else
                {
                    Console.WriteLine("The settings of TranscriptLoggerMiddleware is incomplete, please check following settings: settings.BlobStorage.ConnectionString, settings.BlobStorage.Container");
                }

                adapter.OnTurnError = async (turnContext, exception) =>
                {
                    await turnContext.SendActivityAsync(exception.Message).ConfigureAwait(false);
                    await conversationState.ClearStateAsync(turnContext).ConfigureAwait(false);
                    await conversationState.SaveChangesAsync(turnContext).ConfigureAwait(false);
                };

                return adapter;
            });

            // Bot
            services.AddSingleton<IBot, ComposerBot>();
        }
    }
}
