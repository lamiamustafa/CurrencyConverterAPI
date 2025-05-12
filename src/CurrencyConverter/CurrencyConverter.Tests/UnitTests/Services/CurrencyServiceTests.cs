using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Tests.UnitTests.Services
{
    public class CurrencyServiceTests
    {
        private readonly Mock<ICurrencyProviderFactory> _providerFactoryMock;
        private readonly Mock<ICurrencyProvider> _providerMock;
        private readonly Mock<IMemoryCache> _memoryCacheMock;
        private readonly CurrencyService _currencyService;

        public CurrencyServiceTests()
        {
            _providerFactoryMock = new Mock<ICurrencyProviderFactory>();
            _providerMock = new Mock<ICurrencyProvider>();
            _memoryCacheMock = new Mock<IMemoryCache>();
            _currencyService = new CurrencyService(_providerFactoryMock.Object, _memoryCacheMock.Object);
        }

        [Fact]
        public async Task GetLatestRates_ReturnsRates_FromCache()
        {
            // Arrange
            var baseCurrency = "USD";
            var expectedResponse = new ExchangeRateResponse
            {
                Base = baseCurrency,
                Rates = new Dictionary<string, decimal> { { "EUR", 0.85M } }
            };

            object cached = expectedResponse;

            _memoryCacheMock
                .Setup(mc => mc.TryGetValue(It.IsAny<object>(), out cached))
                .Returns(true);

            // Act
            var result = await _currencyService.GetLatestRates(baseCurrency);

            // Assert
            Assert.Equal(expectedResponse.Base, result.Base);
            Assert.True(result.Rates.ContainsKey("EUR"));
        }

        [Fact]
        public async Task GetLatestRates_ReturnsRates_FromProvider()
        {
            // Arrange
            var baseCurrency = "USD";
            var expectedResponse = new ExchangeRateResponse
            {
                Base = baseCurrency,
                Rates = new Dictionary<string, decimal> { { "EUR", 0.85M } }
            };

            var cacheEntryMock = new Mock<ICacheEntry>();
            cacheEntryMock.SetupAllProperties();

            object cacheKeyUsed = null;

            _memoryCacheMock
                .Setup(mc => mc.CreateEntry(It.IsAny<object>()))
                .Callback<object>(key => cacheKeyUsed = key)
                .Returns(cacheEntryMock.Object);

            // Simulate no value in cache
            _memoryCacheMock
                .Setup(mc => mc.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
                .Returns(false);

            _providerFactoryMock
                .Setup(pf => pf.CreateCurrencyProvider("Frankfurter"))
                .Returns(_providerMock.Object);

            _providerMock
                .Setup(p => p.GetLatestRatesAsync(baseCurrency))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _currencyService.GetLatestRates(baseCurrency);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("USD", result.Base);
            Assert.Equal(0.85M, result.Rates["EUR"]);
        }


        [Fact]
        public async Task GetHistoricalRatesAsync_ReturnsRates_FromCache()
        {
            // Arrange
            var baseCurrency = "USD";
            var start = new DateTime(2024, 05, 01);
            var end = new DateTime(2024, 05, 05);

            var expectedResponse = new HistoryExchangeRateResponse
            {
                BaseCurrency = baseCurrency,
                StartDate = start.ToString("yyyy-MM-dd"),
                EndDate = end.ToString("yyyy-MM-dd"),
                Rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    { "2024-05-01", new Dictionary<string, decimal> { { "EUR", 0.85M } } },
                    { "2024-05-02", new Dictionary<string, decimal> { { "EUR", 0.86M } } }
                }
            };

            object cached = expectedResponse;

            _memoryCacheMock
                .Setup(mc => mc.TryGetValue(It.IsAny<object>(), out cached))
                .Returns(true);

            // Act
            var result = await _currencyService.GetHistoricalRatesAsync(baseCurrency, start, end);

            // Assert
            Assert.Equal(baseCurrency, result.BaseCurrency);
            Assert.Equal("2024-05-01", result.StartDate);
            Assert.Equal("2024-05-05", result.EndDate);
            Assert.True(result.Rates.ContainsKey("2024-05-01"));
            Assert.True(result.Rates["2024-05-01"].ContainsKey("EUR"));
            Assert.Equal(0.85M, result.Rates["2024-05-01"]["EUR"]);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_ReturnsRates_FromProvider()
        {
            // Arrange
            var baseCurrency = "USD";
            var start = new DateTime(2024, 05, 01);
            var end = new DateTime(2024, 05, 05);

            var expectedResponse = new HistoryExchangeRateResponse
            {
                BaseCurrency = baseCurrency,
                StartDate = start.ToString("yyyy-MM-dd"),
                EndDate = end.ToString("yyyy-MM-dd"),
                Rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    { "2024-05-01", new Dictionary<string, decimal> { { "EUR", 0.85M } } },
                    { "2024-05-02", new Dictionary<string, decimal> { { "EUR", 0.86M } } }
                }
            };

            var cacheEntryMock = new Mock<ICacheEntry>();
            cacheEntryMock.SetupAllProperties();

            object cacheKeyUsed = null;

            _memoryCacheMock
                .Setup(mc => mc.CreateEntry(It.IsAny<object>()))
                .Callback<object>(key => cacheKeyUsed = key)
                .Returns(cacheEntryMock.Object);

            // Simulate no value in cache
            _memoryCacheMock
                .Setup(mc => mc.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
                .Returns(false);

            _providerFactoryMock
                .Setup(pf => pf.CreateCurrencyProvider("Frankfurter"))
                .Returns(_providerMock.Object);

            _providerMock
                .Setup(p => p.GetHistoricalRatesAsync(baseCurrency, start, end))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _currencyService.GetHistoricalRatesAsync(baseCurrency, start, end);

            // Assert
            Assert.Equal(baseCurrency, result.BaseCurrency);
            Assert.Equal("2024-05-01", result.StartDate);
            Assert.Equal("2024-05-05", result.EndDate);
            Assert.True(result.Rates.ContainsKey("2024-05-01"));
            Assert.True(result.Rates["2024-05-01"].ContainsKey("EUR"));
            Assert.Equal(0.85M, result.Rates["2024-05-01"]["EUR"]);
        }

        [Theory]
        [InlineData("TRY", "USD", 100)]
        [InlineData("USD", "TRY", 100)]
        public async Task ConvertCurrencyAsync_ThrowsException_ForBlockedCurrency(string from, string to, decimal amount)
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _currencyService.ConvertCurrencyAsync(from, to, amount));
        }

        [Fact]
        public async Task ConvertCurrencyAsync_ReturnsConvertedAmount()
        {
            // Arrange
            string from = "USD", to = "EUR";
            decimal amount = 100;
            var rate = 0.85M;

            var expectedResponse = new ExchangeRateResponse
            {
                Base = from,
                Rates = new Dictionary<string, decimal> { { to, rate } }
            };

            object cached = expectedResponse;

            _memoryCacheMock
                .Setup(mc => mc.TryGetValue(It.IsAny<object>(), out cached))
                .Returns(true);

            // Act
            var result = await _currencyService.ConvertCurrencyAsync(from, to, amount);

            // Assert
            Assert.Equal(Math.Round(amount * rate), result);
        }
    }
}
