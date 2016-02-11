using Exortech.NetReflector;
using System;
using System.IO;
using ThoughtWorks.CruiseControl.Core;
using ThoughtWorks.CruiseControl.Core.Config;
using ThoughtWorks.CruiseControl.Remote;

namespace CruiseControl.Core.Triggers
{
    [ReflectorType("githubHookTrigger", Description = "A trigger that accepts Github hook requests to trigger builds.")]
    public class GithubHookTrigger : ITrigger, IConfigurationValidation
    {
        public const string DefaultEndPoint = "http://*:31574/";

        [ThreadStatic]
        private bool started;

        private IDisposable webApp;

        public GithubHookTrigger()
        {
            this.BuildCondition = BuildCondition.IfModificationExists;
        }

        public IntegrationRequest Fire()
        {
            if (!started)
            {
                started = true;

                if (string.IsNullOrEmpty(this.EndPoint))
                    this.EndPoint = DefaultEndPoint;
                if (this.Branches == null || this.Branches.Length == 0)
                    this.Branches = new string[] { "master" };

                Log("Starting OWIN on " + this.EndPoint);

                webApp = OwinStartup.Start(this);

                Log("Started OWIN on " + this.EndPoint);
            }

            if (this.PushData != null)
            {
                IntegrationRequest req = new IntegrationRequest(this.BuildCondition, this.GetType().Name, this.PushData.PushedBy);
                req.BuildValues["Branch"] = this.PushData.Branch;
                this.PushData = null;
                return req;
            }
            return null;
        }

        public void IntegrationCompleted()
        {
        }

        public void Validate(IConfiguration configuration, ConfigurationTrace parent, IConfigurationErrorProcesser errorProcesser)
        {
            if (!string.IsNullOrWhiteSpace(this.LogFile))
            {
                try
                {
                    string dir = Path.GetDirectoryName(this.LogFile);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    Log("Validating permissions");
                }
                catch (Exception ex)
                {
                    errorProcesser.ProcessWarning("Could not create log file: ", ex.ToString());
                    this.LogFile = null;
                }
            }
        }

        public void Log(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }

        public void Log(string message)
        {
            if (!string.IsNullOrWhiteSpace(this.LogFile))
            {
                File.AppendAllText(this.LogFile, message + Environment.NewLine);
            }
        }

        public DateTime NextBuild { get { return DateTime.MaxValue; } }

        [ReflectorProperty("endpoint",
            Description = "The service endpoint this trigger should bind to. (E.g. " + DefaultEndPoint + ")",
            Required = false)]
        public string EndPoint { get; set; }

        [ReflectorProperty("logfile",
            Description = @"The full log path to a log file. (E.g. C:\CCNet\logs\githubHookTrigger.log)",
            Required = false)]
        public string LogFile { get; set; }

        [ReflectorProperty("secret",
            Description = @"The secret matching the secret defined on Github.",
            Required = false)]
        public string Secret { get; set; }

        [ReflectorProperty("branches",
            Description = @"The branches that cause the trigger to fire.",
            Required = false)]
        public string[] Branches { get; set; }

        [ReflectorProperty("buildCondition",
            Description = "",
            Required = false)]
        public BuildCondition BuildCondition { get; set; }

        /// <summary>
        /// Info about the push notification.
        /// </summary>
        internal PushData PushData { get; set; }
    }
}