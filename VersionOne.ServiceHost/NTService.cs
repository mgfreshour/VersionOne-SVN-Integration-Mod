/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System.ServiceProcess;

namespace VersionOne.ServiceHost
{
	internal class NTService : ServiceBase
	{
		private ServiceMode _servicemode = new ServiceMode();
		
		public NTService(string shortname)
		{
			ServiceName = shortname;
		}

		protected override void OnStart(string[] args)
		{
			base.OnStart(args);
			_servicemode.Start();
		}

		protected override void OnStop()
		{
			_servicemode.Stop();
			base.OnStop();
		}
	}
}