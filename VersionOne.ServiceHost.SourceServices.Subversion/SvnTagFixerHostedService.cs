/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using VersionOne.Profile;
using VersionOne.ServiceHost.Core;
using VersionOne.ServiceHost.Eventing;
using VersionOne.ServiceHost.Logging;

namespace VersionOne.ServiceHost.SourceServices.Subversion
{
	public class SvnTagFixerHostedService : AbstractRevisionProcessor
	{
		public static string ExternalsKey = "svn:externals";
	    private const string CommitMessage = "auto-committed by ServiceHost TagFixer";
		private string _respostoryRoot;

		public class SvnTagFixerIntervalSync {}

		protected override Type PubType
		{
			get { return typeof (SvnTagFixerIntervalSync); }
		}

		protected override void InernalInitialize(XmlElement config, IEventManager eventmanager, IProfile profile)
		{
			_respostoryRoot = config["RepositoryRoot"].InnerText;
		}

		protected override void InternalDispose(bool deterministic) { }

		protected override void ProcessRevision(int revision, string author, DateTime changeDate, string message, IList<string> filesChanged, ChangeSetDictionary changedPathInfos)
		{
			LogMessage.Log(LogMessage.SeverityType.Debug, string.Format("Processing Revision {0}.", revision), _eventManager);
			PropertiesCollection toSave = new PropertiesCollection();
			foreach (string path in changedPathInfos.Keys)
			{
				IChangedPathInfo info = changedPathInfos[path];

                //// Only execute if path is a Tag (the Rev is a Copy operation, with destination including "Tags")
                if ((!path.ToUpper().Contains("/TAGS/")) || (info.Action != SubversionAction.Add) || (info.CopyFromPath == null) || (info.CopyFromRevision < 0))
                    continue;
				
				string pathToUse = FixPath(path);
				// get any svn:external properties, and update them to specify the revision
                PropertiesCollection properties = GetProperties(pathToUse, revision, true);
                foreach (string changedPath in properties.Keys)
                {
                    if (properties[changedPath].ContainsKey(ExternalsKey))
                    {
                        string originalValue = properties[changedPath][ExternalsKey];
                        Dictionary<string, string> updated = new Dictionary<string, string>();
                        updated.Add(ExternalsKey, UpdateValue(originalValue, info.CopyFromRevision)); // Get the revision of the copy source
                        toSave[changedPath] = updated;

                        LogMessage.Log(LogMessage.SeverityType.Debug,
                                       string.Format("Updating property \"{0}\" on \"{1}\" to value \"{2}\".", ExternalsKey, changedPath,
                                                     updated[ExternalsKey]), _eventManager);
                    }
                }
			}
			SaveProperties(toSave);
			base.ProcessRevision(revision, author, changeDate, message, filesChanged, changedPathInfos);
		}

		private string FixPath(string path)
		{
			// if Repo Path is file:///svn/repo2/SVNRss/Tags/Build. fix /SVNRss/Tags/Build/2/Commom to file:///svn/repo2/SVNRss/Tags/Build/2/Commom
			if (!path.StartsWith(_respostoryRoot))
				return _respostoryRoot + path;
			else
				return path;
		}

		private string UpdateValue(string original, int revision)
		{
			string[] foo = {"/n"};
			string[] values = original.Split(foo, StringSplitOptions.RemoveEmptyEntries);
			List<string> updatedValues = new List<string>();
			
			foreach(string value in values)
			{
				string updatedValue = value.TrimEnd();
				// Change from svn://svn/1/common to svn://svn/1/common@999
				if (!updatedValue.Contains("@"))
					updatedValue += "@" + revision;
				updatedValues.Add(updatedValue);
			}

			return string.Join("/n", updatedValues.ToArray());
		}

		protected virtual void SaveProperties(PropertiesCollection toSave)
		{
			// Checkout each folder (top only)
			// Set the proptery on the WC
			// Commit the change
			foreach(string path in toSave.Keys)
			{
				LogMessage.Log(LogMessage.SeverityType.Info,
									   string.Format("Saving property \"{0}\" on \"{1}\" with value \"{2}\".", ExternalsKey, path,
													 toSave[path][ExternalsKey]), _eventManager);

				string tempWorkingPath = Path.GetTempPath() + Path.GetRandomFileName();
				FileSys.DeleteFolder(tempWorkingPath);
				FileSys.CreateFolder(tempWorkingPath);
                int outrev = Connector.Checkout(path, tempWorkingPath, false, true);
                if (outrev >= 0) 
                {
                    LogMessage.Log(LogMessage.SeverityType.Debug, "CheckOut Rev: " + outrev + " => " + path + " => " + tempWorkingPath, _eventManager);

                    Connector.SaveProperty(ExternalsKey, toSave[path][ExternalsKey], tempWorkingPath, false, false);

                    int rev = Connector.Commit(new string[] { tempWorkingPath }, false, false, CommitMessage);

                    if (rev >= 0)
                        LogMessage.Log(LogMessage.SeverityType.Debug, "Committed Working Path: " + path + " to rev " + rev, _eventManager);
                    else
                        LogMessage.Log(LogMessage.SeverityType.Error, "Failed to commit working path: " + path, _eventManager);
                } else
                    LogMessage.Log(LogMessage.SeverityType.Info, "Failed to CheckOut to Working Path: " + path + " => " + tempWorkingPath, _eventManager);

				Connector.Cleanup(tempWorkingPath);
				
				FileSys.DeleteFolder(tempWorkingPath, true);
			}
		}

        protected virtual PropertiesCollection GetProperties(string path, int revision, bool recurse) 
        {
            return Connector.GetProperies(path, revision, true);
        }
	}
}
