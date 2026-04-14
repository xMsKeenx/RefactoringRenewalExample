using LegacyRenewalApp.Helper;
using LegacyRenewalApp.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly ICostumerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IRenewalServiceValidator _validator;
        private readonly IBillingGateway _billingGateway;
        private readonly IDiscountCalculator _discountCalculator;
        private readonly ITaxCalculator _taxCalculator;

        public SubscriptionRenewalService()
          : this(new CustomerRepository(),
                 new SubscriptionPlanRepository(),
                 new RenewalServiceValidator(),
                 new BillingGatewayAdapter(),
                 new DiscountCalculator(),
                 new TaxCalculator())
        { }

        public SubscriptionRenewalService(
            ICostumerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            IRenewalServiceValidator validator,
            IBillingGateway billingGateway,
            IDiscountCalculator discountCalculator,
            ITaxCalculator taxCalculator)
        {
            _customerRepository = customerRepository;
            _planRepository = planRepository;
            _validator = validator;
            _billingGateway = billingGateway;
            _discountCalculator = discountCalculator;
            _taxCalculator = taxCalculator;
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId, string planCode, int seatCount,
            string paymentMethod, bool includePremiumSupport, bool useLoyaltyPoints)
        {
            _validator.Validate(customerId, planCode, seatCount, paymentMethod);

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

            var customer = _customerRepository.GetById(customerId);
            var plan = _planRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");

            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
            string notes = string.Empty;

            var discountResult = _discountCalculator.CalculateDiscount(customer, plan, seatCount, baseAmount, useLoyaltyPoints);
            decimal discountAmount = discountResult.DiscountAmount;
            notes += discountResult.Notes;

            decimal subtotalAfterDiscount = baseAmount - discountAmount;
            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notes += "minimum discounted subtotal applied; ";
            }

            decimal supportFee = 0m;
            if (includePremiumSupport)
            {
                supportFee = normalizedPlanCode switch
                {
                    "START" => 250m,
                    "PRO" => 400m,
                    "ENTERPRISE" => 700m,
                    _ => 0m
                };
                notes += "premium support included; ";
            }

            decimal paymentFee = normalizedPaymentMethod switch
            {
                "CARD" => (subtotalAfterDiscount + supportFee) * 0.02m,
                "BANK_TRANSFER" => (subtotalAfterDiscount + supportFee) * 0.01m,
                "PAYPAL" => (subtotalAfterDiscount + supportFee) * 0.035m,
                "INVOICE" => 0m,
                _ => throw new ArgumentException("Unsupported payment method")
            };

            if (normalizedPaymentMethod != "INVOICE") notes += $"{normalizedPaymentMethod.ToLower()} fee; ";
            else notes += "invoice payment; ";

            decimal taxRate = _taxCalculator.GetTaxRate(customer.Country);
            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            decimal taxAmount = taxBase * taxRate;

            decimal finalAmount = taxBase + taxAmount;
            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "minimum invoice amount applied; ";
            }

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            _billingGateway.SaveInvoice(invoice);

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                string subject = "Subscription renewal invoice";
                string body = $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} has been prepared. Final amount: {invoice.FinalAmount:F2}.";
                _billingGateway.SendEmail(customer.Email, subject, body);
            }

            return invoice;
        }
    }
}