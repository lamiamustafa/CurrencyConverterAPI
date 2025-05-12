using CurrencyConverter.API.Controllers;
using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.DTOs; 
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CurrencyConverter.Tests.UnitTests.Controllers
{
    public class CurrencyConversionControllerTests
    {
        private readonly Mock<ICurrencyService> _mockCurrencyService;
        private readonly CurrencyConversionController _controller;

        public CurrencyConversionControllerTests()
        {
            _mockCurrencyService = new Mock<ICurrencyService>();
            _controller = new CurrencyConversionController(_mockCurrencyService.Object);
        }

        [Fact]
        public async Task GetLatestRates_ReturnsOkResult_WithRates()
        {
            // Arrange
            string from = "EUR";
            string to = "USD";
            decimal amount = 1.2m, res = 1.2m;

            _mockCurrencyService.Setup(s => s.ConvertCurrencyAsync(from, to, amount)).ReturnsAsync(res);

            // Act
            var result = await _controller.ConvertCurrencyAsync(from, to, amount);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(res, okResult.Value);
        }
    }

}
