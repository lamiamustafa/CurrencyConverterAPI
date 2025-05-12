using CurrencyConverter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.API.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ExchangeRatesController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;

        public ExchangeRatesController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;   
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestRates(string baseCurrency)
        {
             return Ok(await _currencyService.GetLatestRates(baseCurrency));
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistoricalRates(string baseCurrency, DateTime startDate, DateTime endDate, int pageNo, int pageSize)
        {
            var result = await _currencyService.GetHistoricalRatesAsync(baseCurrency, startDate, endDate);
            result.Rates = result.Rates.Skip((pageNo - 1) * pageSize).Take(pageSize).ToDictionary<string, Dictionary<string, decimal>>();
            return Ok(result);
        }
    }
}
