using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CurrencyConverter.Application.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly ICurrencyProviderFactory _currencyProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CurrencyService> _logger;
        private readonly string _latestRatesCacheKey = "rates_latest";
        private readonly string _historicalRatesCacheKey = "rates_hostorical";
        public static readonly HashSet<string> blockedConvertCurrencies = new(){ "TRY", "PLN", "THB", "MXN" };//TODO: move to appsettings

        public CurrencyService(ICurrencyProviderFactory currencyProvider, IMemoryCache memoryCache, ILogger<CurrencyService> logger)
        {
            _currencyProvider = currencyProvider;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<ExchangeRateResponse> GetLatestRates(string baseCurrency)
        {
            var cacheKey = $"{_latestRatesCacheKey}_{baseCurrency}";
            var result = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpiration = DateTime.UtcNow.Date.AddDays(1); //expire at the midnight UTC.
                var response = await GetLatestRatesFromProvider(baseCurrency);

                return response;
            });
            return result;
        }

        public async Task<HistoryExchangeRateResponse> GetHistoricalRatesAsync(string baseCurrency, DateTime start, DateTime end)
        {
            var cacheKey = $"{_historicalRatesCacheKey}_{baseCurrency}_{start:yyyy-MM-dd}_{end:yyyy-MM-dd}";
            var result = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30);//long term cache
                var response = await GetHistoricalRatesFromProvider(baseCurrency, start, end);
                return response;
            });
            return result;
        }

        public async Task<decimal> ConvertCurrencyAsync(string from, string to, decimal amount)
        {
            var blockedCurrency = new[] { from, to }
                .FirstOrDefault(currency => blockedConvertCurrencies.Contains(currency));

            if (blockedCurrency != null)
            {
                _logger.LogError($"Currency conversion involving '{blockedCurrency}' is not allowed.");
                return -1;
            }

            var latest = await GetLatestRates(from);
            decimal fromToRate = latest.Rates[to];
            decimal convertedAmount = Math.Round(amount * fromToRate);
            return convertedAmount;
        }

        private async Task<ExchangeRateResponse> GetLatestRatesFromProvider(string baseCurrency)
        {
            var frankfurterProvider = _currencyProvider.CreateCurrencyProvider("Frankfurter");

            var res = await frankfurterProvider.GetLatestRatesAsync(baseCurrency);
            return res;
        }
        private async Task<HistoryExchangeRateResponse> GetHistoricalRatesFromProvider(string baseCurrency, DateTime start, DateTime end)
        {
            var frankfurterProvider = _currencyProvider.CreateCurrencyProvider("Frankfurter");

            var res = await frankfurterProvider.GetHistoricalRatesAsync(baseCurrency, start, end);
            return res;
        }
    }
}
