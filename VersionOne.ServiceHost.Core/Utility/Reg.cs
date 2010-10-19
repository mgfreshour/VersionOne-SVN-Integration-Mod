/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using Microsoft.Win32;

namespace VersionOne.ServiceHost.Core
{
	public sealed class Reg
	{
		private static RegistryKey GetRootKey(string rootname)
		{
			switch (rootname)
			{
				case "HKEY_LOCAL_MACHINE":
				case "HKLM":
					return Registry.LocalMachine;

				case "HKEY_USERS":
					return Registry.Users;

				case "HKEY_CLASSES_ROOT":
				case "HKCR":
					return Registry.ClassesRoot;

				case "HKEY_CURRENT_USER":
				case "HKCU":
					return Registry.ClassesRoot;

				case "HKEY_CURRENT_CONFIG":
					return Registry.CurrentConfig;

				case "HKEY_DYN_DATA":
					return Registry.DynData;

				case "HKEY_PERFORMANCE_DATA":
					return Registry.PerformanceData;

				default:
					throw new ArgumentOutOfRangeException("rootname", rootname, "Unrecognized Registry Root");
			}
		}

		private static RegistryKey OpenKey(string keyname, bool create)
		{
			string[] keyparts = keyname.Split(new char[] { '\\' }, 2);
			RegistryKey rootkey = GetRootKey(keyparts[0]);
			if (keyparts.Length < 2) return rootkey;
			return create ? rootkey.CreateSubKey(keyparts[1]) : rootkey.OpenSubKey(keyparts[1], true);
		}

		/// <summary>
		/// Delete the specified key if it is empty.  If that causes its parent key to be empty, continue upwards.
		/// </summary>
		public static void DeleteEmptyKey(string keyname)
		{
			RegistryKey key = OpenKey(keyname, false);
			if (key != null)
				for (; ; )
				{
					int keycount, valuecount;
					keycount = key.SubKeyCount;
					valuecount = key.ValueCount;

					key.Close();

					if (keycount != 0 || valuecount != 0) break;

					int i = keyname.LastIndexOf('\\');
					if (i == -1) break;

					string subkeyname = keyname.Substring(i + 1);
					keyname = keyname.Substring(0, i);

					key = OpenKey(keyname, false);
					key.DeleteSubKey(subkeyname);
				}
		}

		/// <summary>
		/// Delete the specified key, including values and all subkeys.
		/// </summary>
		public static void DeleteKey(string keyname, bool deleteemptyparents)
		{
			int i = keyname.LastIndexOf('\\');
			if (i == -1) return;

			string subkeyname = keyname.Substring(i + 1);
			string parentkeyname = keyname.Substring(0, i);

			if (KeyExists(keyname))
				using (RegistryKey key = OpenKey(parentkeyname, false))
				{
					if (key != null)
						key.DeleteSubKeyTree(subkeyname);
				}

			if (deleteemptyparents)
				DeleteEmptyKey(parentkeyname);
		}

		public static void DeleteKey(string keyname)
		{
			DeleteKey(keyname, false);
		}

		/// <summary>
		/// Sets a new registry value and returns old value
		/// </summary>
		/// <param name="keyname">Full key path</param>
		/// <param name="valuename">Name of value.  May be empty for key's default value</param>
		/// <param name="value">New value.  May be null to delete the value.</param>
		/// <returns>Previous value</returns>
		public static object SetValue(string keyname, string valuename, object value)
		{
			object oldvalue = null;
			using (RegistryKey key = OpenKey(keyname, (value != null)))
			{
				if (key != null)
				{
					oldvalue = key.GetValue(valuename);
					if (value != null)
						key.SetValue(valuename, value);
					else
						key.DeleteValue(valuename, false);
					key.Close();
				}
			}
			return oldvalue;
		}

		/// <summary>
		/// Retrieve a registry value.
		/// </summary>
		/// <param name="keyname">Full key path</param>
		/// <param name="valuename">Name of value.  May be empty for key's default value</param>
		public static object GetValue(string keyname, string valuename)
		{
			object value = null;
			using (RegistryKey key = OpenKey(keyname, false))
			{
				if (key != null)
				{
					value = key.GetValue(valuename);
					key.Close();
				}
			}
			return value;
		}

		public static bool KeyExists(string keyname)
		{
			using (RegistryKey key = OpenKey(keyname, false))
			{
				return (key != null);
			}
		}

		public static int SubKeyCount(string keyname)
		{
			using (RegistryKey key = OpenKey(keyname, false))
			{
				if (key != null)
					return key.SubKeyCount;
			}
			return 0;
		}

		public static string[] GetSubKeyNames(string keyname)
		{
			using (RegistryKey key = OpenKey(keyname, false))
			{
				if (key != null)
					return key.GetSubKeyNames();
			}
			return null;
		}

		public static string[] GetValueNames(string keyname)
		{
			using (RegistryKey key = OpenKey(keyname, false))
			{
				if (key != null)
					return key.GetValueNames();
			}
			return null;
		}
	}
}