using LegacyRenewalApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyRenewalApp.Helper
{
    public class DiscountCalculator : IDiscountCalculator
    {
        public (decimal DiscountAmount, string Notes) CalculateDiscount(
            Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool useLoyaltyPoints)
        {
            decimal discountAmount = 0m;
            string notes = string.Empty;

            // Segment discounts
            if (customer.Segment == "Silver") { discountAmount += baseAmount * 0.05m; notes += "silver discount; "; }
            else if (customer.Segment == "Gold") { discountAmount += baseAmount * 0.10m; notes += "gold discount; "; }
            else if (customer.Segment == "Platinum") { discountAmount += baseAmount * 0.15m; notes += "platinum discount; "; }
            else if (customer.Segment == "Education" && plan.IsEducationEligible) { discountAmount += baseAmount * 0.20m; notes += "education discount; "; }

            // Loyalty discounts
            if (customer.YearsWithCompany >= 5) { discountAmount += baseAmount * 0.07m; notes += "long-term loyalty discount; "; }
            else if (customer.YearsWithCompany >= 2) { discountAmount += baseAmount * 0.03m; notes += "basic loyalty discount; "; }

            // Seat discounts
            if (seatCount >= 50) { discountAmount += baseAmount * 0.12m; notes += "large team discount; "; }
            else if (seatCount >= 20) { discountAmount += baseAmount * 0.08m; notes += "medium team discount; "; }
            else if (seatCount >= 10) { discountAmount += baseAmount * 0.04m; notes += "small team discount; "; }

            // Points
            if (useLoyaltyPoints && customer.LoyaltyPoints > 0)
            {
                int pointsToUse = customer.LoyaltyPoints > 200 ? 200 : customer.LoyaltyPoints;
                discountAmount += pointsToUse;
                notes += $"loyalty points used: {pointsToUse}; ";
            }

            return (discountAmount, notes);
        }
    }
}
