using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace PhonePeGateway.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> GeneratePaymentLink([FromBody] VerifyRequestModel phonePePayment)
        {
            try
            {
                // ON LIVE URL YOU MAY GET CORS ISSUE, ADD Below LINE TO RESOLVE
                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var PhonePeGatewayURL = "https://api-preprod.phonepe.com/apis/pg-sandbox";

                var httpClient = new HttpClient();
                var uri = new Uri($"{PhonePeGatewayURL}/pg/v1/pay");

                // Add headers
                httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("X-VERIFY", phonePePayment.X_VERIFY);

                // Create JSON request body
                var jsonBody = $"{{\"request\":\"{phonePePayment.base64}\"}}";
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Send POST request
                var response = await httpClient.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();

                // Read and deserialize the response content
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent);
                // Return a response
                return Json(new { Success = true, Message = "Verification successful", phonepeResponse = responseContent });
            }
            catch (Exception ex)
            {
                // Handle errors and return an error response
                return Json(new { Success = false, Message = "Verification failed", Error = ex.Message });
            }
        }
        // POST: /Home/GeneratePaymentLink
        [HttpPost]
        public async Task<IActionResult> GeneratePaymentLinkWithDelay([FromBody] VerifyRequestModel phonePePayment)
        {
            var PhonePeGatewayURL = "https://api-preprod.phonepe.com/apis/pg-sandbox";

            var httpClient = new HttpClient();
            var uri = new Uri($"{PhonePeGatewayURL}/pg/v1/pay");

            // Add headers
            httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("X-VERIFY", phonePePayment.X_VERIFY);

            // Create JSON request body
            var jsonBody = $"{{\"request\":\"{phonePePayment.base64}\"}}";
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            await PostRequestWithRetry(uri, content, httpClient);
            // Return the responseContent
            return Json(new { Success = true, Message = "Verification successful", phonepeResponse = "" });
        }

        static async Task PostRequestWithRetry(Uri uri, HttpContent content, HttpClient httpClient)
        {
            int retryCount = 0;
            int maxRetryAttempts = 5;
            int initialDelay = 2000; // Start with 2 seconds
            int maxDelay = 60000; // Cap the delay to 1 minute

            while (retryCount < maxRetryAttempts)
            {
                try
                {
                    HttpResponseMessage response = await httpClient.PostAsync(uri, content);

                    if (response.StatusCode == HttpStatusCode.TooManyRequests) // 429 Too Many Requests
                    {
                        retryCount++;

                        // Check if `Retry-After` header is present
                        if (response.Headers.TryGetValues("Retry-After", out var values))
                        {
                            var retryAfter = values.FirstOrDefault();
                            if (int.TryParse(retryAfter, out int seconds))
                            {
                                Console.WriteLine($"Rate limited. Retrying after {seconds} seconds...");
                                await Task.Delay(TimeSpan.FromSeconds(seconds));
                            }
                        }
                        else
                        {
                            // Calculate delay with exponential backoff and some randomness
                            var delay = Math.Min(initialDelay * (int)Math.Pow(2, retryCount), maxDelay);
                            delay = new Random().Next(delay, delay + 1000); // Add randomness

                            Console.WriteLine($"Rate limited. Retrying after {delay} milliseconds...");
                            await Task.Delay(delay);
                        }
                    }
                    else
                    {
                        // Handle successful response
                        var responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Response: {responseBody}");
                        break; // Exit the loop if the request was successful
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occurred: {ex.Message}");
                    // Optionally: Log the exception and handle it accordingly
                    break;
                }
            }

            if (retryCount == maxRetryAttempts)
            {
                Console.WriteLine("Maximum retry attempts reached. Please try again later.");
            }
        }


        // POST: /Home/CheckPaymentStatus
        [HttpPost]
        public async Task<JsonResult> CheckPaymentStatus(VerifyRequestModel phonePePayment)
        {
            try
            {
                // ON LIVE URL YOU MAY GET CORS ISSUE, ADD Below LINE TO RESOLVE
                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var PhonePeGatewayURL = "https://api-preprod.phonepe.com/apis/pg-sandbox";

                var httpClient = new HttpClient();
                var uri = new Uri($"{PhonePeGatewayURL}/pg/v1/status/{phonePePayment.MERCHANTID}/{phonePePayment.TransactionId}");

                // Add headers
                httpClient.DefaultRequestHeaders.Add("accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("X-VERIFY", phonePePayment.X_VERIFY);
                httpClient.DefaultRequestHeaders.Add("X-MERCHANT-ID", phonePePayment.MERCHANTID);

                // Create JSON request body

                // Send POST request
                var response = await httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                // Read and deserialize the response content
                var responseContent = await response.Content.ReadAsStringAsync();

                // Return a response
                return Json(new { Success = true, Message = "Verification successful", phonepeResponse = responseContent });
            }
            catch (Exception ex)
            {
                // Handle errors and return an error response
                return Json(new { Success = false, Message = "Verification failed", Error = ex.Message });
            }
        }

        public IActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }

    public class VerifyRequestModel
    {
        public string X_VERIFY { get; set; }
        public string base64 { get; set; }
        public string TransactionId { get; set; }
        public string MERCHANTID { get; set; }
        // Add other properties from the request if needed
    }
}