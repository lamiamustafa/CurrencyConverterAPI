using CurrencyConverter.API.Controllers;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.DTOs; 
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CurrencyConverter.Tests.UnitTests.Controllers
{
    public class ConversionsControllerTests
    {
        private readonly Mock<ICurrencyService> _mockCurrencyService;
        private readonly ConversionsController _controller;

        public ConversionsControllerTests()
        {
            _mockCurrencyService = new Mock<ICurrencyService>();
            _controller = new ConversionsController(_mockCurrencyService.Object);
        }

        [Fact]
        public async Task GetLatestRates_ReturnsOkResult_WithRates()
        {
            // Arrange
            string from = "EUR";
            string to = "USD";
            decimal amount = 1.2m, res = 1.2m;
            var expectedRes = new
            {
                fromCurrency = from,
                fromAmount = amount,
                toCurrency = to,
                toAmount = res
            };
            _mockCurrencyService.Setup(s => s.ConvertCurrencyAsync(from, to, amount)).ReturnsAsync(res);

            // Act
            var result = await _controller.ConvertCurrencyAsync(from, to, amount);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var actual = okResult.Value;
            Assert.NotNull(actual);
            Assert.Equal(from, actual.GetType().GetProperty("fromCurrency")?.GetValue(actual));
            Assert.Equal(amount, actual.GetType().GetProperty("fromAmount")?.GetValue(actual));
            Assert.Equal(to, actual.GetType().GetProperty("toCurrency")?.GetValue(actual));
            Assert.Equal(res, actual.GetType().GetProperty("toAmount")?.GetValue(actual));
        }
    }

}
