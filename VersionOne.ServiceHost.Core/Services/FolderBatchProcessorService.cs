/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Xml;
using VersionOne.Profile;
using VersionOne.ServiceHost.Eventing;
using VersionOne.ServiceHost.HostedServices;
using VersionOne.ServiceHost.Logging;

namespace VersionOne.ServiceHost.Core
{
	public abstract class FolderBatchProcessorService : IHostedService
	{
		private IEventManager _eventManager;
		private RecyclingFileMonitor _monitor;
		private string _folderFilterPattern;
		private string _suiteName;

		protected IEventManager EventManager { get { return _eventManager; } }

		public virtual void Initialize(XmlElement config, IEventManager eventManager, IProfile profile)
		{
			_folderFilterPattern = config["Filter"].InnerText;
			_suiteName = config["SuiteName"].InnerText;
			_monitor = new RecyclingFileMonitor( profile, config["Watch"].InnerText, _folderFilterPattern, Process);
			_eventManager = eventManager;
			_eventManager.Subscribe(EventSinkType, _monitor.ProcessFolder);
		}

		private void Process(string[] files)
		{
			try
			{
				LogMessage.Log(LogMessage.SeverityType.Debug, string.Format("Starting Processing {0} files: {1}", files.Length, string.Join(",", files)), EventManager);
				InternalProcess(files, _suiteName);
				LogMessage.Log( LogMessage.SeverityType.Debug, string.Format( "Finished Processing {0} files: {1}", files.Length, string.Join( ",", files ) ), EventManager );
			}
			catch (Exception ex)
			{
				string message = string.Format( "Failed Processing {0} files: {1}\n{2}", files.Length, string.Join( ",", files ), ex.ToString() );
				LogMessage.Log( LogMessage.SeverityType.Error, message, EventManager );
			}
		}

		protected abstract void InternalProcess(string[] files, string suiteName);
		protected abstract Type EventSinkType { get; }
	}
}