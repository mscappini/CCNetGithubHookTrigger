using Microsoft.Owin;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;

namespace CruiseControl.Core.Triggers
{
    public class DefaultController : ApiController
    {
        private static readonly string[] RequiredHeaders = new string[]
        {
            Constants.XGithubEvent,
            Constants.XGithubDelivery
        };
        private readonly Dictionary<string, string> payloadArgs = new Dictionary<string, string>();

        private GithubHookTrigger trigger;

        private string Event { get { return this.payloadArgs[RequiredHeaders[0]]; } }
        private string Delivery { get { return this.payloadArgs[RequiredHeaders[1]]; } }
        private string Signature { get { return this.payloadArgs[Constants.XHubSignature]; } }

        public DefaultController(GithubHookTrigger trigger)
        {
            this.trigger = trigger;
            foreach (string key in RequiredHeaders)
            {
                payloadArgs[key] = null;
            }
            payloadArgs[Constants.XHubSignature] = null;
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post(JObject payload)
        {
            // Determine if any required parameters are missing.
            ParseHeaders(Request.Headers);

            // Authorize, if necessary.
            Authorize();

            // Log the received event.
            Log(string.Join(
                Environment.NewLine,
                "Payload received:", $"\tEvent: {Event}", $"\tUnique ID: {Delivery}", $"\tSignature: {Signature}", payload.ToString()));

            try
            {
                // Process events.
                if (string.Compare("push", this.Event, true) == 0)
                    return await Push(payload);
                else if (string.Compare("ping", this.Event, true) == 0)
                    return await Ping(payload);
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw;
            }

            // An event was sent from Github that is not supported.
            string eventNotSupported = $"Event not supported: {this.Event}";
            Log(eventNotSupported);
            throw new HttpResponseException(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(eventNotSupported)
            });
        }

        private void ParseHeaders(HttpRequestHeaders headers)
        {
            IEnumerable<string> vals;
            Func<string, string> parseHeader = (k) =>
            {
                if (headers.TryGetValues(k, out vals))
                    return vals.First();
                return null;
            };

            foreach (string key in RequiredHeaders)
            {
                payloadArgs[key] = parseHeader(key);
            }
            payloadArgs[Constants.XHubSignature] = parseHeader(Constants.XHubSignature);

            var missingRequiredArgs = payloadArgs.Where(kvp => RequiredHeaders.Contains(kvp.Key) && string.IsNullOrWhiteSpace(kvp.Value));
            if (missingRequiredArgs.Any())
                ThrowMissingParameter(missingRequiredArgs.Select(ma => ma.Key));
        }

        private void Authorize()
        {
            if (this.Signature != null)
            {
                if (string.IsNullOrEmpty(this.trigger.Secret))
                {
                    Log("Signature provided, but no secret defined");
                    ThrowUnauthorized();
                }

                string[] parts = this.Signature.Split('=');
                if (parts.Length < 2)
                {
                    Log("Unsupported signature format: " + this.Signature);
                    ThrowUnauthorized();
                }
                else if (parts[0] != "sha1")
                {
                    Log("Unsupported signature digest method: " + parts[0]);
                    ThrowUnauthorized();
                }
                else
                {
                    IOwinContext owinContext = this.Request.GetOwinContext();
                    if (!owinContext.Environment.ContainsKey("HMAC"))
                    {
                        Log("HMAC SHA1 not stored; middleware may be broken.");
                        ThrowUnauthorized();
                    }

                    string digest = owinContext.Environment["HMAC"] as string;
                    if (string.Compare(parts[1], digest, true) != 0)
                    {
                        Log($"Body digest from Github {parts[1]} does not match calculated body digest {digest}");
                        ThrowUnauthorized();
                    }
                }
            }
        }

        private async Task<HttpResponseMessage> Push(JObject payload)
        {
            dynamic content = payload;

            string pusher = content.pusher.name.ToString();
            string[] refParts = content.@ref.ToString().Split('/');
            string branch = refParts[refParts.Length - 1];

            Log($"Received push event to {branch} from {pusher}");

            foreach (string branchPattern in this.trigger.Branches)
            {
                MatchCollection matches = Regex.Matches(branch, branchPattern);
                if (matches.Count > 0 && matches[0].Value == branch)
                {
                    this.trigger.PushData = new PushData()
                    {
                        PushedBy = pusher,
                        Branch = branch
                    };
                }
            }

            return await Task.FromResult(Request.CreateResponse(System.Net.HttpStatusCode.OK));
        }

        private async Task<HttpResponseMessage> Ping(JObject model)
        {
            string log = "Ping received!";
            if (model != null)
            {
                dynamic content = model;
                log += $" Here's some knowledge: {content.zen}";
            }
            Log(log);

            return await Task.FromResult(Request.CreateResponse(HttpStatusCode.OK));
        }

        private void ThrowMissingParameter(IEnumerable<string> args)
        {
            throw new HttpResponseException(new HttpResponseMessage()
            {
                StatusCode = (HttpStatusCode)422,
                Content = new StringContent("Missing required arguments: " + string.Join(", ", args))
            });
        }

        private void ThrowUnauthorized()
        {
            throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }

        private void Log(string message)
        {
            this.trigger.Log(message);
        }
    }
}