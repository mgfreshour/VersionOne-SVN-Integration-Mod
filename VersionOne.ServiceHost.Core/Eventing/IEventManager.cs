/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;

namespace VersionOne.ServiceHost.Eventing
{
	public delegate void EventDelegate(object pubobj);
	
	public interface IEventManager
	{
		void Publish(object pubobj);
		void Subscribe(Type pubtype, EventDelegate listener);
	    void Unsubscribe(Type pubtype, EventDelegate listener);
	}
}