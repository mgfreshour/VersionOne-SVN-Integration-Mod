/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Xml;
using VersionOne.Profile;
using VersionOne.ServiceHost.Eventing;
using VersionOne.ServiceHost.HostedServices;
using VersionOne.ServiceHost.Logging;

namespace VersionOne.ServiceHost.Core
{
	public abstract class FileProcessorService : IHostedService
	{
		private IEventManager _eventManager;
		private FileMonitor _monitor;

		protected IEventManager EventManager { get { return _eventManager; } }
		
		public void Initialize(XmlElement config, IEventManager eventManager, IProfile profile)
		{
			_monitor = new FileMonitor(profile, config["Watch"].InnerText, config["Filter"].InnerText, Process);
			_eventManager = eventManager;			
			_eventManager.Subscribe(EventSinkType, _monitor.ProcessFolder);
		}

		private void Process(string file)
		{
			try
			{
				LogMessage.Log(string.Format("Starting Processing File: {0}", file),EventManager);
				InternalProcess(file);
				LogMessage.Log(string.Format("Finished Processing File: {0}", file), EventManager);
			}
			catch (Exception ex)
			{
				LogMessage.Log(string.Format("Failed Processing File: {0}", file), ex, EventManager);
			}
		}
		
		protected abstract void InternalProcess(string file);
		protected abstract Type EventSinkType { get; }
	}
}