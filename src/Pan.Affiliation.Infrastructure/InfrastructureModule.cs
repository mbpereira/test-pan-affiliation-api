﻿using System.Security.Authentication;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pan.Affiliation.Domain.Shared.Settings;
using Pan.Affiliation.Infrastructure.Caching;
using Pan.Affiliation.Infrastructure.Persistence;
using Pan.Affiliation.Infrastructure.Settings;
using Pan.Affiliation.Infrastructure.Settings.Sections;
using Serilog;
using Serilog.Enrichers.Span;
using StackExchange.Redis;

namespace Pan.Affiliation.Infrastructure
{
    public class InfrastructureModule : Module
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IServiceCollection _services;

        public InfrastructureModule(IConfiguration configuration)
        {
            _settingsProvider = new SettingsProvider(configuration);
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            _services.AddDbContext<PanAffiliationDbContext>(contextBuilder =>
                contextBuilder.UseNpgsql(GetConnectionString(),
                    b => b.MigrationsAssembly(GetMigrationsAssembly())));

            AddHttpClient(Gateways.Ibge.Constants.IbgeHttpClient);
            AddHttpClient(Gateways.ViaCep.Constants.ViaCepHttpClient);
            AddSerilog();

            builder.Populate(_services);

            builder
                .RegisterInstance(_settingsProvider)
                .SingleInstance();

            RegisterRedis(builder);

            builder.RegisterGeneric(typeof(Logging.Logger<>))
                .As(typeof(Domain.Shared.Logging.ILogger<>))
                .InstancePerLifetimeScope();

            builder.RegisterAssemblyTypes(typeof(InfrastructureModule).Assembly)
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }

        private void RegisterRedis(ContainerBuilder builder)
        {
            var settings = _settingsProvider.GetSection<RedisSettings>(RedisCacheProvider.Constants.SettingsKey);
            builder.Register(_ =>
                    ConnectionMultiplexer.Connect(new ConfigurationOptions()
                    {
                        AbortOnConnectFail = false,
                        EndPoints = { { settings.Host!, settings.Port } },
                        DefaultDatabase = settings.DefaultDatabase,
                        SslProtocols = SslProtocols.Tls12,
                        AsyncTimeout = 5000,
                        ConnectTimeout = 5000,
                        SyncTimeout = 5000,
                        
                    }))
                .As<IConnectionMultiplexer>()
                .SingleInstance();
        }

        private void AddSerilog()
        {
            _services.AddLogging(loggingBuilder =>
            {
                var settings = _settingsProvider.GetSection<LogSettings>(Logging.Constants.LoggingSettingsKey);

                var configuration = new LoggerConfiguration();

                var newRelicSettings = settings.NewRelicSettings;

                if (newRelicSettings?.ApplicationName is not null &&
                    newRelicSettings?.LicenseKey is not null)
                {
                    configuration = configuration
                        .WriteTo
                        .NewRelicLogs(
                            licenseKey: newRelicSettings?.LicenseKey,
                            applicationName: newRelicSettings?.ApplicationName);
                }

                if (settings.LogFile is not null)
                {
                    configuration = configuration
                        .WriteTo
                        .File(settings.LogFile);
                }

                configuration = configuration
                    .Enrich.FromLogContext()
                    .Enrich.WithEnvironmentName()
                    .Enrich.WithMachineName()
                    .Enrich.WithClientAgent()
                    .Enrich.WithClientIp()
                    .Enrich.WithCorrelationId()
                    .Enrich.WithTraceIdentifier()
                    .Enrich.WithSpan()
                    .WriteTo.Console();

                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(configuration.CreateLogger());
            });
        }

        private void AddHttpClient(string httpClientIdentifier)
        {
            _services.AddHttpClient(httpClientIdentifier)
                .SetHandlerLifetime(TimeSpan.FromHours(1));
        }

        private static string? GetMigrationsAssembly()
            => typeof(PanAffiliationDbContext).Assembly.FullName;

        private string GetConnectionString()
        {
            var settings = _settingsProvider.GetSection<DbSettings>(Constants.PanAffiliationDbSettingsKey);

            return string.Format(Constants.PgConnectionString,
                settings.Host,
                settings.Username,
                settings.Password,
                settings.Database,
                settings.Port);
        }
    }
}