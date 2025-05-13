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
        public FrankfurterProvider(HttpClient httpClient, ILogger<ICurrencyProvider> logger, IConfiguration config)
        {
            _httpClient = httpClient;
            _logger = logger;
            if(config != null && config["ExternalApis:Frankfurter:BaseURL"] != null)
            {
                _httpClient.BaseAddress = new Uri(config["ExternalApis:Frankfurter:BaseURL"]);
            }
        }

        /// <summary>
        /// Fetches the latest exchange rates for a given base currency from the Frankfurter API.
        /// </summary>
        /// <param name="baseCurrency"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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

        /// <summary>
        /// Fetches historical exchange rates for a given base currency and date range from the Frankfurter API.
        /// </summary>
        /// <param name="baseCurrency"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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
