using LegacyRenewalApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyRenewalApp.Helper
{
    public class TaxCalculator : ITaxCalculator
    {
        private readonly Dictionary<string, decimal> _taxRates = new()
        {
            { "Poland", 0.23m },
            { "Germany", 0.19m },
            { "Czech Republic", 0.21m },
            { "Norway", 0.25m }
        };

        public decimal GetTaxRate(string country)
        {
            
            return _taxRates.ContainsKey(country) ? _taxRates[country] : 0.20m;
        }
    }
}
