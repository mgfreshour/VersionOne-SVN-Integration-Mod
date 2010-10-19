/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Reflection;
using SharpSvn;

namespace VersionOne.ServiceHost.SourceServices.Subversion
{
	public class SvnConnector : IDisposable
	{
		private readonly SvnClient client = new SvnClient();

		public SvnConnector()
		{
            MethodInfo mi = client.GetType().GetProperty("AdministrativeDirectoryName").GetSetMethod(true);
            mi.Invoke(null, new object[] { "_svn" });		
		}

		public void SetAuthentication(string username, string password)
		{
		    client.Authentication.DefaultCredentials = new NetworkCredential(username, password);
		}

		public void Poke(string url, int revision)
		{
			try
			{
			    client.Log(new Uri(url), new SvnLogArgs(new SvnRevisionRange(new SvnRevision(revision), SvnRevision.Head)), LogHandler);
			}
			catch (Exception ex) { OnError(ex); }
		}

        private void LogHandler(object sender, SvnLogEventArgs e) {
            try
            {
                List<string> filesChanged = new List<string>();
                ChangeSetDictionary changedItems = new ChangeSetDictionary();
                
                if(e.ChangedPaths == null) 
                {
                    return;
                }
                
                foreach (SvnChangeItem item in e.ChangedPaths) {
                    filesChanged.Add(item.Path);
                    changedItems.Add(item.Path, new ChangedPathInfo(item));
                }

                int revision = Convert.ToInt32(e.Revision);
                OnChangeSet(revision, e.Author, e.Time, e.LogMessage, filesChanged, changedItems);
            }
            catch(Exception ex) 
            {
                OnError(ex);    
            }

        }
		
        private static SvnUriTarget CreateSvnUriTarget(string uriString, int revision) 
        {
            Uri uri = new Uri(uriString);
            SvnUriTarget target = new SvnUriTarget(uri, new SvnRevision(revision));
            return target;
        }

        private static SvnUriTarget CreateSvnUriTarget(string uriString, SvnRevision revision) {
            Uri uri = new Uri(uriString);
            SvnUriTarget target = new SvnUriTarget(uri, revision);
            return target;
        }

        public RevisionPropertyCollection GetRevisionProperties(string url) {
            return GetRevisionProperties(url, SvnRevision.Head);
        }

        public RevisionPropertyCollection GetRevisionProperties(string url, int revision) 
        {
            return GetRevisionProperties(url, new SvnRevision(revision));
        }

        private RevisionPropertyCollection GetRevisionProperties(string url, SvnRevision revision) 
        {
            try 
            {
                SvnPropertyCollection propertyCollection;
                client.GetRevisionPropertyList(CreateSvnUriTarget(url, revision), out propertyCollection);
                RevisionPropertyCollection revisionPropertyCollection = new RevisionPropertyCollection((int)revision.Revision);
                foreach (SvnPropertyValue value in propertyCollection) {
                    revisionPropertyCollection.Add(value.Key, value.StringValue);
                }
                return revisionPropertyCollection;
            } 
            catch(Exception ex) 
            {
                OnError(ex);
            }
            
            return null;
        }

        public int SetRevisionProperty(string path, int revision, string propname, string propval) 
        {
            try 
            {
                client.SetRevisionProperty(CreateSvnUriTarget(path, revision), propname, propval);
            } 
            catch(Exception ex) 
            {
                OnError(ex);
            }
            return int.MinValue;
        }

        public PropertiesCollection GetProperies(string target, bool recurse)
        {
            return GetProperties(target, SvnRevision.None, recurse);
        }

        public PropertiesCollection GetProperies(string target, int asOfRevision, bool recurse)
        {
            return GetProperties(target, new SvnRevision(asOfRevision), recurse);
        }

        private PropertiesCollection GetProperties(string target, SvnRevision asOfRevision, bool recurse) 
        {
            try 
            {
                PropertiesCollection result = new PropertiesCollection();
                Collection<SvnPropertyListEventArgs> output;
                SvnPropertyListArgs args = new SvnPropertyListArgs();
                args.Revision = asOfRevision;
                args.Depth = recurse ? SvnDepth.Infinity : SvnDepth.Children;

                client.GetPropertyList(new Uri(target), args, out output);

                foreach (SvnPropertyListEventArgs eventArgs in output) {
                    Dictionary<string, string> properties = new Dictionary<string, string>(eventArgs.Properties.Count);
                    foreach (SvnPropertyValue value in eventArgs.Properties) {
                        properties.Add(value.Key, value.StringValue);
                    }
                    result.Add(eventArgs.Path, properties);
                }

                return result;
            } 
            catch(Exception ex) 
            {
                OnError(ex);
            }

            return null;
        }
		
		public void SaveProperty(string propertyName, string value, string target, bool recursive, bool skipChecks)
		{
			try
			{
                SvnSetPropertyArgs args = new SvnSetPropertyArgs();
			    args.SkipChecks = skipChecks;
                args.Depth = recursive ? SvnDepth.Infinity : SvnDepth.Children;
			    client.SetProperty(target, propertyName, value, args);
			}
			catch (Exception ex)
			{
				OnError(ex);
			}
		}

        public int Checkout(string repoUrl, string workingPath, bool recurse, bool ignoreExternals)
        {
            return Checkout(repoUrl, workingPath, SvnRevision.Head, recurse, ignoreExternals);
        }

        public int Checkout(string repoUrl, string workingPath, int revision, bool recurse, bool ignoreExternals)
        {
            return Checkout(repoUrl, workingPath, new SvnRevision(revision), recurse, ignoreExternals);
        }

        private int Checkout(string repoUrl, string workingPath, SvnRevision revision, bool recurse, bool ignoreExternals)
        {
            try
            {
                SvnCheckOutArgs args = new SvnCheckOutArgs();
                args.Revision = revision;
                args.IgnoreExternals = ignoreExternals;
                args.Depth = recurse ? SvnDepth.Infinity : SvnDepth.Children;
                SvnUpdateResult result;
                client.CheckOut(new Uri(repoUrl), workingPath, args, out result);
                return (int) result.Revision;
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            return int.MinValue;
        }

		public void Cleanup(string workingPath)
		{
			try
			{
			    client.CleanUp(workingPath);
			}
			catch (Exception ex)
			{
				OnError(ex);
			}
		}

        public int Commit(ICollection<string> targets, bool recurse, bool keepLocks, string logMessage) 
        {
            try 
            {
                SvnCommitArgs args = new SvnCommitArgs();
                args.Depth = SvnDepth.Infinity;
                args.KeepLocks = true;
                args.LogMessage = logMessage;
                SvnCommitResult result;

                client.Commit(targets, args, out result);
                OnCommitted((int)result.Revision, result.Author, result.Time);

                return (int)result.Revision;
            }
            catch (Exception ex) 
            {
                OnError(ex);
            }

            return int.MinValue;
        }
		
		#region Events

		private void OnChangeSet(int revision, string author, DateTime time, string message, List<string> changed, ChangeSetDictionary changedPathInfos)
		{
			if (_revision != null)
				_revision(this, new RevisionArgs(revision, author, time, message, changed, changedPathInfos));
		}
		private event EventHandler<RevisionArgs> _revision;
		public event EventHandler<RevisionArgs> Revision
		{
			add { _revision += value; }
			remove { _revision -= value; }
		}
		public class RevisionArgs : EventArgs
		{
			public readonly int Revision;
			public readonly string Author;
			public readonly DateTime Time;
			public readonly string Message;
			public readonly List<string> Changed;
			public readonly ChangeSetDictionary ChangePathInfos;

			public RevisionArgs(int revision, string author, DateTime time, string message, List<string> changed, ChangeSetDictionary changedPathInfos)
			{
				Revision = revision;
				Author = author;
				Time = time;
				Message = message;
				Changed = changed;
				ChangePathInfos = changedPathInfos;
			}
		}

		private void OnError(Exception ex)
		{
			if (_error != null)
				_error(this, new ExceptionEventArgs(ex));
		}
		private event EventHandler<ExceptionEventArgs> _error;
		public event EventHandler<ExceptionEventArgs> Error
		{
			add { _error += value; }
			remove { _error -= value; }
		}

		private void OnCommitted(int revision, string author, DateTime time)
		{
			if (_committed != null)
				_committed(this, new CommitEventArgs(revision, author, time));
		}
		private event EventHandler<CommitEventArgs> _committed;
		public event EventHandler<CommitEventArgs> Committed
		{
			add { _committed += value; }
			remove { _committed -= value; }
		}
		public class CommitEventArgs : EventArgs
		{
			public readonly int Revision;
			public readonly string Author;
			public readonly DateTime Time;

			public CommitEventArgs(int revision, string author, DateTime time)
			{
				Revision = revision;
				Author = author;
				Time = time;
			}
		}

		#endregion
		
		#region Dispose Pattern
		
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
            client.Dispose();
		}

		~SvnConnector()
		{
			// When we are GC'd, we don't dispose managed objects - we let the GC handle that
			Dispose(false);
		}
		
		#endregion
	}
}
