using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Infrastructure.CurrencyProvider;
using CurrencyConverter.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Tests.UnitTests.ExternalProviders
{
    public class CurrencyProviderFactoryTests
    {
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<ILogger<ICurrencyProvider>> _loggerMock;
        private readonly CurrencyProviderFactory _factory;

        public CurrencyProviderFactoryTests()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _loggerMock = new Mock<ILogger<ICurrencyProvider>>();
            _factory = new CurrencyProviderFactory(
                _serviceProviderMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void CreateCurrencyProvider_WithFrankfurter_ReturnsFrankfurterProvider()
        {
            // Arrange
            var frankfurterMock = new Mock<FrankfurterProvider>(MockBehavior.Loose, new object[] { null!, null! });

            _serviceProviderMock
                .Setup(sp => sp.GetService(typeof(FrankfurterProvider)))
                .Returns(frankfurterMock.Object);
            // Act
            var result = _factory.CreateCurrencyProvider("Frankfurter");

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<FrankfurterProvider>(result);
        }

        [Fact]
        public void CreateCurrencyProvider_WithUnknownProvider_ThrowsArgumentException()
        {
            // Arrange
            var unknownProvider = "UnknownProvider";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _factory.CreateCurrencyProvider(unknownProvider));
            Assert.Equal($"Unknown provider: {unknownProvider}", ex.Message);
        }
    }
}
