using LegacyRenewalApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyRenewalApp.Helper
{
    public class BillingGatewayAdapter : IBillingGateway
    {
        public void SaveInvoice(RenewalInvoice invoice)
        {
            
            LegacyBillingGateway.SaveInvoice(invoice);
        }

        public void SendEmail(string email, string subject, string body)
        {
            LegacyBillingGateway.SendEmail(email, subject, body);
        }
    }
}
