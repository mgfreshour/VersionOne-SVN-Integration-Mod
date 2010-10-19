/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using VersionOne.SDK.APIClient;
using VersionOne.Profile;
using VersionOne.ServiceHost.Core.Services;
using VersionOne.ServiceHost.Eventing;
using VersionOne.ServiceHost.HostedServices;
using VersionOne.ServiceHost.Logging;
using Attribute = VersionOne.SDK.APIClient.Attribute;

namespace VersionOne.ServiceHost
{
	internal class ConsoleMode : CommonMode
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

			Console.WriteLine("Press enter to close...");
			Console.Read();
		}

		private void InternalRun()
		{
			Startup();

			Console.TreatControlCAsInput = true;
			bool quit = false;
			while (!quit)
			{
				ConsoleKeyInfo info = Console.ReadKey(true);
				switch (info.Key)
				{
					case ConsoleKey.Q:
						quit = true;
						break;
					case ConsoleKey.H:
						Console.WriteLine("\t\tq\t\tQuits console");
						Console.WriteLine("\t\th\t\tPrints this help");
						break;
					case ConsoleKey.C:
						if ((info.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control)
							quit = true;
						break;
					default:
						break;
				}
			}

			Shutdown();
		}
	}
}