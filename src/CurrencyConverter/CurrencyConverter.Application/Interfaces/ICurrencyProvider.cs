using CurrencyConverter.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Application.Interfaces
{
    public interface ICurrencyProvider
    {
        Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency);
        Task<HistoryExchangeRateResponse> GetHistoricalRatesAsync(string baseCurrency, DateTime start, DateTime end);
    }
}
