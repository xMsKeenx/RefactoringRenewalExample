using LegacyRenewalApp.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyRenewalApp.Interfaces
{
    public interface IDiscountCalculator
    {
        (decimal DiscountAmount, string Notes) CalculateDiscount(
            Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints);
    }
}
