/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;

namespace VersionOne.ServiceHost.Logging
{
	public class ConsoleLogService : BaseLogService
	{
		private LogMessage.SeverityType _severity = LogMessage.SeverityType.Info;

		protected override void Log(LogMessage msg)
		{
			if (msg.Severity >= _severity)
			{
				Console.WriteLine(string.Format("[{0}] {1}", msg.Severity, msg.Message));
				Exception ex = msg.Exception;
				while (ex != null)
				{
					Console.WriteLine(string.Format("[{0}] Exception: {1}", msg.Severity, ex.Message));
					ex = ex.InnerException;
				}
			}
		}

		public override void Initialize(System.Xml.XmlElement config, VersionOne.ServiceHost.Eventing.IEventManager eventManager, VersionOne.Profile.IProfile profile)
		{
			base.Initialize(config, eventManager, profile);

			if (config["LogLevel"] != null && ! string.IsNullOrEmpty(config["LogLevel"].InnerText))
			{
				string logLevel = config["LogLevel"].InnerText;

				try
				{
					_severity = (LogMessage.SeverityType) Enum.Parse(typeof(LogMessage.SeverityType), logLevel, true);
				}
				catch (Exception)
				{
					Console.WriteLine( "Couldn't parse LogLevel '{0}'. Try Debug, Info, or Error.");
				}

			}
		}

		protected override void Startup()
		{
			Console.WriteLine("[Startup]");
		}		
		
		protected override void Shutdown()
		{
			Console.WriteLine("[Shutdown]");
		}
	}
}