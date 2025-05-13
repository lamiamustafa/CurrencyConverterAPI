using CurrencyConverter.API.Models;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Tests.IntegrationTests;
using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace CurrencyConverter.IntegrationTests
{
    public class ExchangeRatesControllerTests : AuthenticatedTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public ExchangeRatesControllerTests(CustomWebApplicationFactory factory) : base(factory.CreateClient())
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetLatestRates_ShouldReturnOkWithData()
        {
            // Arrange

            var token = await GetJwtTokenAsync("admin@example.com", "Admin123!");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var res = new ExchangeRateResponse
            {
                Amount = 1,
                Base = "USD",
                Date = DateTime.UtcNow.Date,
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.85m },
                    { "GBP", 0.75m }
                }
            };

            _factory.CurrencyServiceMock
                .Setup(service => service.GetLatestRates("USD"))
                .ReturnsAsync(res);

            // Act

            var response = await _client.GetAsync("/api/v1/ExchangeRates/latest?baseCurrency=USD");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();
            content.Should().BeEquivalentTo(res);
        }
    }
}
