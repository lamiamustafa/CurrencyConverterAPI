using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CurrencyConverter.Infrastructure.CurrencyProvider
{
    public class FrankfurterProvider : ICurrencyProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ICurrencyProvider> _logger;
        public FrankfurterProvider(HttpClient httpClient, ILogger<ICurrencyProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency)
        {
            _logger.LogInformation("Fetching latest rates from Frankfurter API for base currency: {BaseCurrency}", baseCurrency);
            var response = await _httpClient.GetAsync($"latest?base={baseCurrency}");
            var content = await response.Content.ReadAsStringAsync();

            if(!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch latest rates from Frankfurter API. Status code: {StatusCode}, Content: {Content}", response.StatusCode, content);
                throw new Exception($"Failed to fetch latest rates. Status code: {response.StatusCode}");
            }
            var searchResult = JsonConvert.DeserializeObject<ExchangeRateResponse>(content);

            return searchResult;
        }

        public async Task<HistoryExchangeRateResponse> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate)
        {
            var response = await _httpClient.GetAsync($"{startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}?base={baseCurrency}");
            var content = await response.Content.ReadAsStringAsync();
            if(!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch historical rates from Frankfurter API. Status code: {StatusCode}, Content: {Content}", response.StatusCode, content);
                throw new Exception($"Failed to fetch historical rates. Status code: {response.StatusCode}");
            }

            var searchResult = JsonConvert.DeserializeObject<HistoryExchangeRateResponse>(content);
            return searchResult;
        }
    } 
}
