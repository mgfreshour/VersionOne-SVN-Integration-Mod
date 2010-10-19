/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using VersionOne.ServiceHost.Logging;

namespace VersionOne.ServiceHost
{
	internal class ServiceMode : CommonMode
	{
		internal void Start()
		{
			try
			{
				Startup();
				LogMessage.Log("ServiceHost running as Service", EventManager);
			}
			catch (Exception ex)
			{
				throw new ApplicationException("Startup Failed", ex);
			}
		}
		
		internal void Stop()
		{
			try
			{
				Shutdown();
			}
			catch (Exception ex)
			{
				throw new ApplicationException("Shutdown Failed", ex);
			}
		}
	}
}