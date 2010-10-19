/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;

namespace VersionOne.ServiceHost
{
	internal class ServiceUtil
	{
		#region DLLImport
		private const int SC_MANAGER_CREATE_SERVICE = 0x0002;
		private const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;
		private const int SERVICE_ERROR_NORMAL = 0x00000001;
		private const int STANDARD_RIGHTS_REQUIRED = 0xF0000;
		private const int SERVICE_QUERY_CONFIG = 0x0001;
		private const int SERVICE_CHANGE_CONFIG = 0x0002;
		private const int SERVICE_QUERY_STATUS = 0x0004;
		private const int SERVICE_ENUMERATE_DEPENDENTS = 0x0008;
		private const int SERVICE_START = 0x0010;
		private const int SERVICE_STOP = 0x0020;
		private const int SERVICE_PAUSE_CONTINUE = 0x0040;
		private const int SERVICE_INTERROGATE = 0x0080;
		private const int SERVICE_USER_DEFINED_CONTROL = 0x0100;
		private const int SERVICE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED |
								  SERVICE_QUERY_CONFIG |
								  SERVICE_CHANGE_CONFIG |
								  SERVICE_QUERY_STATUS |
								  SERVICE_ENUMERATE_DEPENDENTS |
								  SERVICE_START |
								  SERVICE_STOP |
								  SERVICE_PAUSE_CONTINUE |
								  SERVICE_INTERROGATE |
								  SERVICE_USER_DEFINED_CONTROL);
		private const int SERVICE_AUTO_START = 0x00000002;

		private const int GENERIC_WRITE = 0x40000000;
		private const int DELETE = 0x10000;			
		
		[DllImport("advapi32.dll")]
		private static extern IntPtr OpenSCManager(string lpMachineName, string lpSCDB, int scParameter);

		[DllImport("Advapi32.dll")]
		private static extern IntPtr CreateService(IntPtr SC_HANDLE, string lpSvcName, string lpDisplayName, int dwDesiredAccess, int dwServiceType, int dwStartType, int dwErrorControl, string lpPathName, string lpLoadOrderGroup, int lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);

		[DllImport("advapi32.dll")]
		private static extern void CloseServiceHandle(IntPtr SCHANDLE);

		[DllImport("advapi32.dll")]
		private static extern int StartService(IntPtr SVHANDLE, int dwNumServiceArgs, string lpServiceArgVectors);

		[DllImport("advapi32.dll", SetLastError=true)]
		private static extern IntPtr OpenService(IntPtr SCHANDLE, string lpSvcName, int dwDesiredAccess);

		[DllImport("advapi32.dll")]
		private static extern int DeleteService(IntPtr SVHANDLE);

		[DllImport("kernel32.dll")]
		private static extern int GetLastError();

		#endregion DLLImport

		public const string LocalService = "NT AUTHORITY\\LocalService";
		public const string NetworkService = "NT AUTHORITY\\NetworkService";
		public const string LocalSystem = null;

		public static bool InstallService(string svcPath, string svcName, string svcDispName, string svcUsername, string svcPassword)
		{
			try
			{
				IntPtr scm = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE);
				if (scm.ToInt32() != 0)
				{
					IntPtr svc = CreateService(scm, svcName, svcDispName, SERVICE_ALL_ACCESS, SERVICE_WIN32_OWN_PROCESS, SERVICE_AUTO_START, SERVICE_ERROR_NORMAL, svcPath, null, 0, null, svcUsername, svcPassword);
					bool installed = svc.ToInt32() != 0;
					CloseServiceHandle(scm);
					return installed;
				}
				else
					return false;
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		public static void StartService(string svcName)
		{
			ServiceController ctrl = new ServiceController(svcName);
			ctrl.Start();
			ctrl.WaitForStatus(ServiceControllerStatus.StartPending);
		}
		
		public static void StopService(string svcName)
		{
			ServiceController ctrl = new ServiceController(svcName);
			ctrl.Stop();
			ctrl.WaitForStatus(ServiceControllerStatus.StopPending);
		}

		public static bool UnInstallService(string svcName)
		{
			IntPtr sc_hndl = OpenSCManager(null, null, GENERIC_WRITE);
			if ( sc_hndl.ToInt32() != 0 )
			{
				IntPtr svc_hndl = OpenService(sc_hndl, svcName, DELETE);
				if ( svc_hndl.ToInt32() != 0 )
				{
					int i = DeleteService(svc_hndl);
					CloseServiceHandle(sc_hndl);
					return i != 0;
				}
				else
					return false;
			}
			else
				return false;
		}
	}
}