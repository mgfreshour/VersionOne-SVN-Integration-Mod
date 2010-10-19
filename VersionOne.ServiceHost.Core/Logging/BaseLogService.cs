/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.IO;
using System.Net;
using System.Xml;
using VersionOne.Profile;
using VersionOne.ServiceHost.Core;
using VersionOne.ServiceHost.Eventing;
using VersionOne.ServiceHost.HostedServices;

namespace VersionOne.ServiceHost.Logging
{
	public abstract class BaseLogService : IHostedService
	{
		public virtual void Initialize(XmlElement config, IEventManager eventManager, IProfile profile)
		{
			eventManager.Subscribe(typeof(LogMessage), LogMessageListener);
			eventManager.Subscribe(typeof(ServiceHostState),ServiceHostStateListener);
		}

		private void LogMessageListener(object pubobj)
		{
			Log((LogMessage)pubobj);
		}

		private void ServiceHostStateListener(object pubobj)
		{
			ServiceHostState state = (ServiceHostState) pubobj;
			switch (state)
			{
				case ServiceHostState.Startup:
					Startup();
					break;
				case ServiceHostState.Shutdown:
					Shutdown();
					break;
			}
		}		

		protected abstract void Log(LogMessage message);
		protected virtual void Startup() { }
		protected virtual void Shutdown() { }

		protected string AdditionalExceptionContent(Exception exception)
		{
			if (exception == null)
				return null;
			
			if (exception is WebException)
			{
				WebException webEx = (WebException)exception;
				WebResponse response = webEx.Response;
				if (response != null)
					using (StreamReader reader = new StreamReader(response.GetResponseStream()))
						return reader.ReadToEnd();
			}
			return null;
		}
	}
}