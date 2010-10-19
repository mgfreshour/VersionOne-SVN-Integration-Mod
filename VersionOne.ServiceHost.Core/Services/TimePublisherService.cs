/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Timers;
using System.Xml;
using VersionOne.Profile;
using VersionOne.ServiceHost.Eventing;
using VersionOne.ServiceHost.HostedServices;
using VersionOne.ServiceHost.Logging;

namespace VersionOne.ServiceHost.Core.Services
{
	/// <summary>
	/// A service that raises an event on an interval basis.
	/// Although the Timer could cause reentrancy, our timer is protected
	/// against that.
	/// </summary>
	public class TimePublisherService : IHostedService
	{
		private double _interval = 0;
		private Type _publishtype = null;
		private IEventManager _eventmanager;
		private Timer _timer;

		public void Initialize(XmlElement config, IEventManager eventManager, IProfile profile)
		{
			if (!double.TryParse(config["Interval"].InnerText, out _interval))
				_interval = -1;
			_publishtype = Type.GetType(config["PublishClass"].InnerText);
			
			_eventmanager = eventManager;
			_eventmanager.Subscribe(typeof(ServiceHostState),HostStateChanged);

			_timer = new Timer(_interval);
			_timer.Enabled = false;
			_timer.Elapsed += Timer_Elapsed;
		}

		private void HostStateChanged(object pubobj)
		{
			ServiceHostState state = (ServiceHostState) pubobj;
			if (state == ServiceHostState.Startup)
				_timer.Enabled = true;
			else if (state == ServiceHostState.Shutdown)
				_timer.Enabled = false;			
		}

		// Prevents reentrancy into event handlers.
		private bool _busy = false;

		/// <summary>
		/// Timer event that publishes the configured event.
		/// </summary>
		/// <param name="sender">Not used.</param>
		/// <param name="e">The timer event.</param>
		/// <remarks>If the previous event has not returned, we skip the current interval.</remarks>
		void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			// The check-and-set of _busy is not thread-safe, but that shouldn't matter unless the inverval is set unreasonably small.
			if (!_busy)
			{
				_busy = true;
				object pub = Activator.CreateInstance(_publishtype);
				LogMessage.Log(LogMessage.SeverityType.Debug, string.Format("Timer Elapsed {0} {1} {2}", _interval, _publishtype.Name, e.SignalTime), _eventmanager);
				_eventmanager.Publish(pub);
				_busy = false;
			}
		}
	}
}