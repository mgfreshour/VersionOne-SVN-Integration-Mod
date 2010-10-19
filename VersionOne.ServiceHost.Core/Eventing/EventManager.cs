/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using VersionOne.ServiceHost.Logging;

namespace VersionOne.ServiceHost.Eventing
{
	public class EventManager : IEventManager
	{
		private readonly IDictionary<Type, EventDelegate> _subscriptions = new Dictionary<Type, EventDelegate>();

		public void Publish(object pubobj) 
		{
			EventDelegate subs;
			if (_subscriptions.TryGetValue(pubobj.GetType(), out subs))
			{
				try
				{
					subs(pubobj);
				}
				catch (Exception ex)
				{
					LogMessage.Log("Event Manager Caught Unhandled Exception", ex, this);
					LogMessage.Log(ex.Message, this);
				}
			}
		}

		public void Subscribe(Type pubtype, EventDelegate listener)
		{
			EventDelegate subs;
			if (!_subscriptions.TryGetValue(pubtype, out subs))
				_subscriptions[pubtype] = listener;
			else
				_subscriptions[pubtype] = (EventDelegate)Delegate.Combine(subs, listener);
		}

		public void Unsubscribe(Type pubtype, EventDelegate listener)
		{
			EventDelegate subscription;
			
			if(_subscriptions.TryGetValue(pubtype, out subscription))
			{
				EventDelegate updatedSubscription = (EventDelegate) Delegate.Remove(subscription, listener);
				
				if (updatedSubscription == null)
				{
					_subscriptions.Remove(pubtype);
					return;
				}

				_subscriptions[pubtype] = updatedSubscription;
			}
		}
	}
}