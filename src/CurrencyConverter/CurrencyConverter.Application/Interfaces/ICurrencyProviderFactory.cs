using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Application.Interfaces
{
    public interface ICurrencyProviderFactory
    {
        ICurrencyProvider CreateCurrencyProvider(string provider);
    }
}
