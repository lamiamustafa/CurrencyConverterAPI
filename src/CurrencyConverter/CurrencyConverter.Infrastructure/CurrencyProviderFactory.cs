using CurrencyConverter.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using CurrencyConverter.Infrastructure.CurrencyProvider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Infrastructure
{
    public class CurrencyProviderFactory : ICurrencyProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ICurrencyProvider> _logger;

        public CurrencyProviderFactory(IServiceProvider serviceProvider, ILogger<ICurrencyProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public ICurrencyProvider CreateCurrencyProvider(string provider)
        {
            switch (provider.ToLower())
            {
                case "frankfurter":
                    return _serviceProvider.GetRequiredService<FrankfurterProvider>();
                default:
                throw new ArgumentException($"Unknown provider: {provider}");
            }
        }
    }
}
