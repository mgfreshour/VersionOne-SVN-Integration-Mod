/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Configuration;
using VersionOne.ServiceHost;
using VersionOne.ServiceHost.Logging;

namespace VersionOne.ServiceExecutor
{
	internal class BatchMode : CommonMode
	{
		internal void Run()
		{
			try
			{
				InternalRun();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		private void InternalRun()
		{
			Startup();

			string startUpClass = ConfigurationSettings.AppSettings["StartUpEvent"];
			if (!string.IsNullOrEmpty(startUpClass))
			{
				object pub = Activator.CreateInstance(Type.GetType(startUpClass));
				EventManager.Publish(pub);
			}
			Shutdown();
		}
	}
}
