using hotel_and_resort.Models;
using Hotel_and_resort.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Web;


namespace hotel_and_resort.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        [HttpGet("payfast")]
        public IActionResult PayFast()
        {
            var merchantId = "\t10038419"; //  Insert our MERCHANT ID 
            var merchantKey = "\to6q83fm4xl05l"; // Insert our MERCHANT KEY
            var passphrase = "HOTELRESORTPAYFAST"; // optional
            var returnUrl = "https://yourapp.com/payment-success";
            var cancelUrl = "https://yourapp.com/payment-cancel";
            var notifyUrl = "https://yourapi.com/api/payment/notify";

            var data = new Dictionary<string, string>
    {
        { "merchant_id", merchantId },
        { "merchant_key", merchantKey },
        { "return_url", returnUrl },
        { "cancel_url", cancelUrl },
        { "notify_url", notifyUrl },
        { "amount", "100.00" },
        { "item_name", "Premium Plan" },
        { "email_address", "user@example.com" }
    };

            var signature = GenerateSignature(data, passphrase);
            data.Add("signature", signature);

            var query = string.Join("&", data.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
            var redirectUrl = $"https://sandbox.payfast.co.za/eng/process?{query}";

            return Redirect(redirectUrl);
        }

        private string GenerateSignature(Dictionary<string, string> data, string passphrase)
        {
            var sorted = data.OrderBy(x => x.Key).ToList();
            var sb = new StringBuilder();

            foreach (var kvp in sorted)
            {
                sb.Append($"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}&");
            }

            if (!string.IsNullOrEmpty(passphrase))
                sb.Append("passphrase=" + HttpUtility.UrlEncode(passphrase));
            else
                sb.Length--;

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

    }

}