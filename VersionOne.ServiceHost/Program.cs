/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.ServiceProcess;

namespace VersionOne.ServiceHost
{
	public class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			
			if (args.Length == 0)
				RunConsole();
			else if (args.Length != 1)
				Help();
			else if (args[0] == "--install")
				InstallService();
			else if (args[0] == "--uninstall")
				UninstallService();
			else if (args[0] == "--service")
				RunService();
			else
				Help();
		}

		private static void UninstallService()
		{
			try
			{
				try { ServiceUtil.StopService(Config.ShortName); } catch { }
				
				if (ServiceUtil.UnInstallService(Config.ShortName))
					Console.WriteLine("Service Uninstall Successful");
				else
					Console.WriteLine("Service Uninstall Failed");
			}
			catch (Exception ex)
			{
				throw new ApplicationException("Uninstall Failed - " + ex.Message);
			}
		}

		private static void InstallService()
		{
			try
			{
				if (ServiceUtil.InstallService("\"" + Assembly.GetEntryAssembly().Location + "\" --service", Config.ShortName, Config.LongName, ServiceUtil.LocalService, null))
					Console.WriteLine("Service Installation Successful");
				else
					Console.WriteLine("Service Installation Failed");
				
				try { ServiceUtil.StartService(Config.ShortName); } catch {}
			}
			catch (Exception ex)
			{
				throw new ApplicationException("Install Failed - " + ex.Message);
			}
		}

		private static void RunService()
		{
			ServiceBase.Run(new NTService(Config.ShortName));
		}

		private static void RunConsole()
		{
			new ConsoleMode().Run();
		}		

		private static void Help()
		{
			Console.WriteLine("\t\t--install\t\tInstall Windows NT Service");
			Console.WriteLine("\t\t--uninstall\t\tUninstall Windows NT Service");
		}

		private static InstallerConfiguration _config;
		private static InstallerConfiguration Config
		{
			get
			{
				if (_config == null)
					_config = (InstallerConfiguration)System.Configuration.ConfigurationManager.GetSection("Installer");
				return _config;
			}
		}
	}
}