using Microsoft.Owin;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CruiseControl.Core.Triggers
{
    public class BodyDigestMiddleware
    {
        private GithubHookTrigger trigger;

        public BodyDigestMiddleware(GithubHookTrigger trigger)
        {
            this.trigger = trigger;
        }

        public async Task Invoke(IOwinContext context, Func<Task> next)
        {
            if (context.Request.Headers.ContainsKey(Constants.XHubSignature) && !string.IsNullOrEmpty(this.trigger.Secret))
            {
                string body = new StreamReader(context.Request.Body).ReadToEnd();
                context.Environment["HMAC"] = HMAC(body, this.trigger.Secret);
                byte[] requestData = Encoding.UTF8.GetBytes(body);
                context.Request.Body = new MemoryStream(requestData);
            }
            await next();
        }

        private static string HMAC(string input, string key)
        {
            HMACSHA1 hmac = new HMACSHA1(Encoding.ASCII.GetBytes(key));
            byte[] inputArray = Encoding.ASCII.GetBytes(input);
            return hmac.ComputeHash(inputArray).Aggregate("", (s, e) => s + String.Format("{0:x2}", e), s => s);
        }
    }
}