/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using System.Xml;
using VersionOne.Profile;
using VersionOne.ServiceHost.Eventing;
using VersionOne.ServiceHost.HostedServices;
using VersionOne.ServiceHost.Logging;

namespace VersionOne.ServiceHost.SourceServices.Subversion
{
	public abstract class AbstractRevisionProcessor : IDisposable, IHostedService
	{
		protected IEventManager _eventManager;
		private string _respostoryPath;
		private IProfile _profile;
		private object _lock = new object();
		protected SvnConnector _connector = new SvnConnector();
		protected static Dictionary<string, string> _usernames = new Dictionary<string,string>();
		protected static Dictionary<string, string> _passwords = new Dictionary<string, string>();

		protected virtual int LastRevision
		{
			get
			{
				int rev;
				int.TryParse(_profile["Revision"].Value, out rev);
				return rev;
			}
			set { _profile["Revision"].Value = value.ToString(); }
		}

		public void Initialize(XmlElement config, IEventManager eventManager, IProfile profile)
		{
			string reposServer = config["RepositoryServer"].InnerText;
			_profile = profile;
			_respostoryPath =reposServer + config["RepositoryPath"].InnerText;

			if (!_usernames.ContainsKey(reposServer))
			{
				Console.Write(String.Format("Please enter {0} Username : ", reposServer));
				_usernames[reposServer] = Console.ReadLine();

				Console.Write(String.Format("Please enter {0} Password : ", reposServer));
				_passwords[reposServer] = Console.ReadLine();
			}


			_connector.SetAuthentication(_usernames[reposServer],  _passwords[reposServer]);
			
			_eventManager = eventManager;
			_eventManager.Subscribe(PubType, PokeRepository);

			_connector.Revision += _connector_Revision;
			_connector.Error += _connector_Error;

			InernalInitialize(config, eventManager, profile);
		}

		protected abstract Type PubType { get; }

		protected SvnConnector Connector { get { return _connector; } }

		protected string RespostoryPath { get { return _respostoryPath; } }

		protected abstract void InernalInitialize(XmlElement config, IEventManager eventmanager, IProfile profile);

		private void _connector_Error(object sender, ExceptionEventArgs e)
		{
			LogMessage.Log("Error accessing Subversion Repository.", e.Exception, _eventManager);
		}

		protected void _connector_Revision(object sender, SvnConnector.RevisionArgs e)
		{
			if (e.Revision > LastRevision)
			{
				lock (_lock)
				{
					if ( e.Revision > LastRevision )
					{
						ProcessRevision(e.Revision, e.Author, e.Time, e.Message, e.Changed, e.ChangePathInfos);
						return;
					}
				}
			}

			LogMessage.Log(LogMessage.SeverityType.Debug, string.Format("Ignoring revision {0} - already processed.", e.Revision), _eventManager);
		}

		private void PokeRepository(object pubobj)
		{
			_connector.Poke(RespostoryPath, LastRevision);
		}

		protected virtual void ProcessRevision(int revision, string author, DateTime changeDate, string message, IList<string> filesChanged, ChangeSetDictionary changedPathInfos)
		{
			LastRevision = revision;
		}

		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="deterministic">True if disposal is deterministic, meaning we should dispose managed objects.</param>
		private void Dispose(bool deterministic)
		{
			if (deterministic)
				_connector.Dispose();
			InternalDispose(deterministic);
		}

		protected abstract void InternalDispose(bool deterministic);

		~AbstractRevisionProcessor()
		{
			// When we are GC'd, we don't dispose managed objects - we let the GC handle that
			Dispose(false);
		}
	}
}