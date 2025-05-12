using CurrencyConverter.API.Controllers;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.DTOs; 
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CurrencyConverter.Tests.UnitTests.Controllers
{
    public class ExchangeRatesControllerTests
    {
        private readonly Mock<ICurrencyService> _mockCurrencyService;
        private readonly ExchangeRatesController _controller;

        public ExchangeRatesControllerTests()
        {
            _mockCurrencyService = new Mock<ICurrencyService>();
            _controller = new ExchangeRatesController(_mockCurrencyService.Object);
        }

        [Fact]
        public async Task GetLatestRates_ReturnsOkResult_WithRates()
        {
            // Arrange
            string baseCurrency = "USD";
            var res = new ExchangeRateResponse {
                Amount = 1,
                Base = baseCurrency,
                Date = DateTime.UtcNow.Date,
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.85m },
                    { "GBP", 0.75m }
                }
            };
            _mockCurrencyService.Setup(s => s.GetLatestRates(baseCurrency)).ReturnsAsync(res);

            // Act
            var result = await _controller.GetLatestRates(baseCurrency);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(res, okResult.Value);
        }

        [Fact]
        public async Task GetHistoricalRates_ReturnsPaginatedResult()
        {
            // Arrange
            string baseCurrency = "USD";
            DateTime startDate = new DateTime(2024, 01, 01);
            DateTime endDate = new DateTime(2024, 01, 03);
            int pageNo = 1, pageSize = 2;

            var fullRates = new Dictionary<string, Dictionary<string, decimal>>
            {
                { "2024-01-01", new Dictionary<string, decimal> { { "EUR", 0.85m } } },
                { "2024-01-02", new Dictionary<string, decimal> { { "EUR", 0.86m } } },
                { "2024-01-03", new Dictionary<string, decimal> { { "EUR", 0.87m } } },
            };

            var resultModel = new HistoryExchangeRateResponse
            {
                BaseCurrency = baseCurrency,
                EndDate = endDate.ToShortDateString(),
                StartDate = startDate.ToShortDateString(),
                Rates = fullRates
            };

            _mockCurrencyService
                .Setup(s => s.GetHistoricalRatesAsync(baseCurrency, startDate, endDate))
                .ReturnsAsync(resultModel);

            // Act
            var result = await _controller.GetHistoricalRates(baseCurrency, startDate, endDate, pageNo, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedModel = Assert.IsType<HistoryExchangeRateResponse>(okResult.Value);

            Assert.Equal(baseCurrency, returnedModel.BaseCurrency);
            Assert.Equal(pageSize, returnedModel.Rates.Count);
        }
    }

}
