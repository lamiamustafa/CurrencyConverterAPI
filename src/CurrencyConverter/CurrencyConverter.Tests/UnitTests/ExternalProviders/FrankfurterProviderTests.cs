using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Infrastructure.CurrencyProvider;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CurrencyConverter.Tests.UnitTests.ExternalProviders
{
    public class FrankfurterProviderTests
    {
        private readonly Mock<ILogger<ICurrencyProvider>> _loggerMock;

        public FrankfurterProviderTests()
        {
            _loggerMock = new Mock<ILogger<ICurrencyProvider>>();
        }

        private HttpClient CreateMockHttpClient(string expectedJsonResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = statusCode,
                   Content = new StringContent(expectedJsonResponse),
               })
               .Verifiable();

            return new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://api.frankfurter.app/")
            };
        }

        [Fact]
        public async Task GetLatestRatesAsync_ReturnsExpectedResult()
        {
            // Arrange
            var json = JsonConvert.SerializeObject(new ExchangeRateResponse
            {
                Base = "USD",
                Rates = new Dictionary<string, decimal> { { "EUR", 0.85M } }
            });

            var httpClient = CreateMockHttpClient(json);
            var provider = new FrankfurterProvider(httpClient, _loggerMock.Object);

            // Act
            var result = await provider.GetLatestRatesAsync("USD");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("USD", result.Base);
            Assert.True(result.Rates.ContainsKey("EUR"));
            Assert.Equal(0.85M, result.Rates["EUR"]);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_ReturnsExpectedResult()
        {
            // Arrange
            var json = JsonConvert.SerializeObject(new HistoryExchangeRateResponse
            {
                BaseCurrency = "USD",
                StartDate = "2024-05-01",
                EndDate = "2024-05-03",
                Rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    { "2024-05-01", new Dictionary<string, decimal> { { "EUR", 0.85M } } },
                    { "2024-05-02", new Dictionary<string, decimal> { { "EUR", 0.86M } } }
                }
            });

            var httpClient = CreateMockHttpClient(json);
            var provider = new FrankfurterProvider(httpClient, _loggerMock.Object);

            var startDate = new DateTime(2024, 5, 1);
            var endDate = new DateTime(2024, 5, 3);

            // Act
            var result = await provider.GetHistoricalRatesAsync("USD", startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("USD", result.BaseCurrency);
            Assert.Equal("2024-05-01", result.StartDate);
            Assert.True(result.Rates.ContainsKey("2024-05-02"));
            Assert.Equal(0.86M, result.Rates["2024-05-02"]["EUR"]);
        }
    }
}
