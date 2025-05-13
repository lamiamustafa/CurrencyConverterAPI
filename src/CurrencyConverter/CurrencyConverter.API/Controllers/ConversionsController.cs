using CurrencyConverter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CurrencyConverter.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ConversionsController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;

        public ConversionsController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;   
        }

        /// <summary>
        /// Convert an amount from one currency to another.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ConvertCurrencyAsync(string from, string to, decimal amount)
        {
            decimal toAmount = await _currencyService.ConvertCurrencyAsync(from, to, amount);
            if(toAmount == -1)
            {
                return BadRequest(new { message = "Conversion failed. The currencies TRY, PLN, THB, and MXN are not supported for conversion." });
            }
            else if(toAmount == 0)
            {
                return NotFound(new { message = "Not found" });
            }

            return Ok(new
            {
                fromCurrency = from,
                fromAmount = amount,
                toCurrency = to,
                toAmount = toAmount
            });
        }
    }
}
