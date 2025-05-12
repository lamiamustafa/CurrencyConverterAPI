using CurrencyConverter.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Tests.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Mock<ICurrencyService> CurrencyServiceMock { get; } = new Mock<ICurrencyService>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove original ICurrencyService registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ICurrencyService));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Add mocked ICurrencyService
                services.AddScoped(_ => CurrencyServiceMock.Object);
            });
        }
    }
}