/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text;

namespace VersionOne.IIS
{
	public abstract class IISObject
	{
		private static IDictionary<string, IISObject> _map = new Dictionary<string, IISObject>();

		private string _path;
		protected string Path { get { return _path; } }

		public string Name
		{
			get
			{
				string[] parts = _path.Split('/');
				return parts[parts.Length - 1];
			}
		}

		private IDictionary _properties = new Hashtable();
		private IDictionary Properties { get { return _properties; } }

		private bool _haschanged;
		public bool HasChanged { get { return _haschanged; } }

		public IISObject(string path)
		{
			_path = path;
			_map[path] = this;
		}

		public void AcceptChanges()
		{
			if (HasChanged)
			{
				using (DirectoryEntry entry = new DirectoryEntry(_path))
				{
					foreach (DictionaryEntry propentry in Properties)
						entry.InvokeSet((string)propentry.Key, propentry.Value);
					entry.CommitChanges();
				}
			}
		}

		public void RejectChanges()
		{
			Properties.Clear();
			_haschanged = false;
		}

		protected T Create<T>(string name, string type) where T : IISObject
		{
			using (DirectoryEntry entry = new DirectoryEntry(_path))
				using (DirectoryEntry e = entry.Children.Add(name, type))
					e.CommitChanges();
			return GetChild<T>(name);
		}

		protected T GetProperty<T>(string name)
		{
			object value = Properties[name];
			if (value == null)
			{
				using (DirectoryEntry entry = new DirectoryEntry(_path))
					value = Properties[name] = entry.InvokeGet(name);
			}
			return (T)value;
		}

		protected void SetProperty<T>(string name, T value)
		{
			T v = GetProperty<T>(name);
			if (!v.Equals(value))
			{
				Properties[name] = value;
				_haschanged = true;
			}
		}

		protected void Execute(string method)
		{
			using (DirectoryEntry entry = new DirectoryEntry(_path))
				entry.Invoke(method, null);
		}

		protected object Execute(string method, params object[] args)
		{
			using (DirectoryEntry entry = new DirectoryEntry(_path))
				return entry.Invoke(method, args);
		}

		protected T GetChild<T>(string name) where T : IISObject
		{
			string fullpath = Path + "/" + name;
			if (DirectoryEntry.Exists(fullpath))
				return GetNode<T>(fullpath);
			return null;
		}

		protected T GetParent<T>() where T : IISObject
		{
			string[] parts = _path.Split('/');
			string path = string.Join("/", parts, 0, parts.Length - 1);
			return GetNode<T>(path);
		}

		private static T GetNode<T>(string fullpath) where T : IISObject
		{
			IISObject v;
			if (!_map.TryGetValue(fullpath, out v))
				v = (T)Activator.CreateInstance(typeof(T), new object[] { fullpath });
			return (T)v;
		}

		protected IDictionary<string, T> GetChildren<T>(string type) where T : IISObject
		{
			IDictionary<string, T> results = new Dictionary<string, T>();

			using (DirectoryEntry entry = new DirectoryEntry(_path))
			{
				foreach (DirectoryEntry child in entry.Children)
				using (child)
					if (child.SchemaClassName == type)
						results.Add(child.Name, GetChild<T>(child.Name));
			}

			return results;
		}
	}

	public class IISComputer : IISObject
	{
		private IISWebService _webservice;
		public IISWebService WebService
		{
			get
			{
				if (_webservice == null)
					_webservice = GetChild<IISWebService>("W3SVC");
				return _webservice;
			}
		}

		public IISComputer() : this("localhost") { }
		public IISComputer(string servername) : base("IIS://" + servername) { }
	}

	public class IISWebService : IISObject
	{
		public IISWebService(string path) : base(path) { }

		public IISComputer Computer { get { return GetParent<IISComputer>(); } }

		private IDictionary<string, IISWebServer> _webservers;
		private IDictionary<string, IISWebServer> WebServersDict
		{
			get
			{
				if (_webservers == null)
					_webservers = GetChildren<IISWebServer>("IIsWebServer");
				return _webservers;
			}
		}

		public ICollection<IISWebServer> WebServers { get { return WebServersDict.Values; } }

		private IISApplicationPools _apppools;
		public IISApplicationPools AppPools
		{
			get
			{
				if (_apppools == null)
					_apppools = GetChild<IISApplicationPools>("AppPools");
				return _apppools;
			}
		}
	}

	public class IISApplicationPools : IISObject
	{
		public IISApplicationPools(string path) : base(path) { }

		public IISWebService WebService { get { return GetParent<IISWebService>(); } }

		private IDictionary<string, IISApplicationPool> _apppools;
		private IDictionary<string, IISApplicationPool> AppPoolsDict
		{
			get
			{
				if (_apppools == null)
					_apppools = GetChildren<IISApplicationPool>("IIsApplicationPool");
				return _apppools;
			}
		}

		public ICollection<IISApplicationPool> AppPools { get { return AppPoolsDict.Values; } }

		public IISApplicationPool GetApplicationPool(string name)
		{
			IISApplicationPool pool;
			AppPoolsDict.TryGetValue(name, out pool);
			return pool;
		}

		public IISApplicationPool AddApplicationPool(string name)
		{
			IISApplicationPool apppool = GetApplicationPool(name);
			if (apppool == null)
			{
				apppool = Create<IISApplicationPool>(name, "IIsApplicationPool");
				AppPoolsDict.Add(name, apppool);
			}
			return apppool;
		}
	}

	public class IISApplicationPool : IISObject
	{
		public IISApplicationPool(string path) : base(path) { }

		public IISApplicationPools AppPools { get { return GetParent<IISApplicationPools>(); } }

		public int PeriodicRestartMemory
		{
			get { return GetProperty<int>("PeriodicRestartMemory"); }
			set { SetProperty("PeriodicRestartMemory", value); }
		}
		public int PeriodicRestartPrivateMemory
		{
			get { return GetProperty<int>("PeriodicRestartPrivateMemory"); }
			set { SetProperty("PeriodicRestartPrivateMemory", value); }
		}

		public void Start()
		{
			Execute("Start");
		}

		public void Stop()
		{
			Execute("Stop");
		}

		public void Recycle()
		{
			Execute("Recycle");
		}
	}

	public class IISWebServer : IISObject
	{
		public IISWebServer(string path) : base(path) { }

		public IISWebService WebService { get { return GetParent<IISWebService>(); } }

		private IDictionary<string, IISRootWebVirtualDir> _virtualdirs;
		private IDictionary<string, IISRootWebVirtualDir> VirtualDirsDict
		{
			get
			{
				if (_virtualdirs == null)
					_virtualdirs = GetChildren<IISRootWebVirtualDir>("IIsWebVirtualDir");
				return _virtualdirs;
			}
		}

		public string ServerBindings { get { return GetProperty<string>("ServerBindings"); } }

		public string HostName
		{
			get
			{
				string[] parts = ServerBindings.Split(':');
				string host = "localhost";
				if (parts[2] != string.Empty)
					host = parts[2];
				return host;
			}
		}

		public int Port
		{
			get
			{
				string[] parts = ServerBindings.Split(':');
				string port = "80";
				if (parts[1] != string.Empty)
					port = parts[1];
				return int.Parse(port);
			}
		}

		public string Url
		{
			get
			{
				string url = "http://" + HostName;
				if (Port != 80)
					url += ":" + Port;
				return url;
			}
		}

		public ICollection<IISRootWebVirtualDir> VirtualDirs { get { return VirtualDirsDict.Values; } }
	}

	public class IISWebVirtualDir : IISObject
	{
		public IISWebVirtualDir(string path) : base(path) { }
		public IISWebVirtualDir ParentDir { get { return GetParent<IISWebVirtualDir>(); } }
		public virtual bool IsRoot { get { return false; } }

		private IDictionary<string, IISWebVirtualDir> _virtualdirs;
		private IDictionary<string, IISWebVirtualDir> VirtualDirsDict
		{
			get
			{
				if (_virtualdirs == null)
					_virtualdirs = GetChildren<IISWebVirtualDir>("IIsWebVirtualDir");
				return _virtualdirs;
			}
		}

		public ICollection<IISWebVirtualDir> VirtualDirs { get { return VirtualDirsDict.Values; } }

		public string AppPoolID
		{
			get { return GetProperty<string>("AppPoolID"); }
			set { SetProperty("AppPoolID", value); }
		}

		public IISWebVirtualDir GetVirtualDir(string instancename)
		{
			IISWebVirtualDir instance;
			if (VirtualDirsDict.TryGetValue(instancename, out instance))
				return instance;
			return null;
		}
	}

	public class IISRootWebVirtualDir : IISWebVirtualDir
	{
		public override bool IsRoot { get { return true; } }
		public IISWebServer WebServer { get { return GetParent<IISWebServer>(); } }
		public IISRootWebVirtualDir(string path) : base(path) { }
	}
}