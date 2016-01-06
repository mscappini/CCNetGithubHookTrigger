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

        public IntegrationRequest Fire()
        {
            if (!started)
            {
                started = true;

                if (string.IsNullOrEmpty(this.EndPoint))
                    this.EndPoint = DefaultEndPoint;
                if (string.IsNullOrEmpty(this.Branch))
                    this.Branch = "master";

                Log("Starting OWIN on " + this.EndPoint);

                webApp = OwinStartup.Start(this);

                Log("Started OWIN on " + this.EndPoint);
            }

            if (!string.IsNullOrWhiteSpace(this.PushedBy))
            {
                string pushedBy = this.PushedBy;
                this.PushedBy = null;
                return new IntegrationRequest(BuildCondition.ForceBuild, this.GetType().Name, pushedBy);
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

        [ReflectorProperty("branch",
            Description = @"The branch that causes the trigger to fire.",
            Required = false)]
        public string Branch { get; set; }

        /// <summary>
        /// This is the only information we need for triggering a build. We can just use this to determine if a build should be made.
        /// </summary>
        public string PushedBy { get; set; }
    }
}