/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.IO;
using System.Reflection;
using System.Xml;
using VersionOne.Profile;
using VersionOne.ServiceHost.Eventing;

namespace VersionOne.ServiceHost.Logging
{
	public class FileLogService : BaseLogService
	{
		private const string _majorsep = "================================================================";
		private const string _minorsep = "----------------------------------------------------------------";
		private string _filename;
		private StreamWriter _writer = null;
		
		protected override void Log(LogMessage message)
		{
#if !DEBUG
			if (message.Severity == LogMessage.SeverityType.Debug)
				return;
#endif
			if (_writer != null)
			{
				_writer.WriteLine(string.Format("[{0}] {2} {1}",message.Severity, message.Message, message.Stamp));
				string prefix = string.Format("[Exception] {0} ", message.Stamp);
				Exception ex = message.Exception;
				while (ex != null)
				{
					_writer.WriteLine(prefix + ex.ToString());
					string extracontent = AdditionalExceptionContent(ex);
					if (!string.IsNullOrEmpty(extracontent))
					{
						_writer.WriteLine(_minorsep);
						_writer.WriteLine(extracontent);
						_writer.WriteLine(_minorsep);
					}
					_writer.WriteLine(_minorsep);

					ex = ex.InnerException;
				}
			}
		}

		public override void Initialize(XmlElement config, IEventManager eventManager, IProfile profile)
		{
			_filename = config["LogFile"].InnerText;
			
			base.Initialize(config,eventManager,profile);

			string folder = Path.GetDirectoryName(_filename);
			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);			
			
			_writer = new StreamWriter(_filename, true);
			_writer.AutoFlush = true;
			_writer.WriteLine(_majorsep);
			_writer.WriteLine("[Startup] Log opened {0}", DateTime.Now);
			_writer.WriteLine("[Startup] By {0}", Assembly.GetEntryAssembly().Location);			
		}

		protected override void Shutdown()
		{
			if (_writer != null)
			{
				_writer.WriteLine("[Shutdown] Log closed {0}", DateTime.Now);
				_writer.WriteLine(_majorsep);
				_writer.Close();
				_writer = null;
			}
			
			base.Shutdown();
		}
	}
}