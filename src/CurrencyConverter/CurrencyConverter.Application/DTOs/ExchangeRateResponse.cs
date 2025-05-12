using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CurrencyConverter.Application.DTOs
{
    public class ExchangeRateResponse
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
                
        [JsonPropertyName("base")]
        public string Base { get; set; }
                
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }


        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; }
    }
}
