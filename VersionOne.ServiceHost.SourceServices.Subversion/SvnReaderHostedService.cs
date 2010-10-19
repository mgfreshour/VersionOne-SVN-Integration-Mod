/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using VersionOne.Profile;
using VersionOne.ServiceHost.Eventing;
using VersionOne.ServiceHost.Logging;

namespace VersionOne.ServiceHost.SourceServices.Subversion
{
	public class SvnReaderHostedService : AbstractRevisionProcessor
	{
		protected string ReferenceExpression { get; set; }
		protected string ReferenceUrl { get; set; }
		protected string ReposName { get; set; }

		protected override void InernalInitialize(XmlElement config, IEventManager eventmanager, IProfile profile)
		{
			ReferenceExpression = config["ReferenceExpression"].InnerText;
			ReferenceUrl = config["ReferenceUrl"].InnerText;
			ReposName = config["ReposName"].InnerText;
		}

		protected override void InternalDispose(bool deterministic) { }

		protected override Type PubType
		{
			get { return typeof(SvnReaderIntervalSync); }
		}

		protected override void ProcessRevision(int revision, string author, DateTime changeDate, string message, IList<string> filesChanged, ChangeSetDictionary changedPathInfos)
		{
			List<string> references = GetReferences(message);

			ChangeSetInfo changeSet = new ChangeSetInfo(author, message, filesChanged, revision.ToString(), changeDate, references, ReferenceUrl, ReposName);
			

			string[] referenceStrings = new string[changeSet.References.Count];
			changeSet.References.CopyTo(referenceStrings, 0);
			string referenceMessage = (referenceStrings.Length > 0) ? "references: " + string.Join(", ", referenceStrings) : "No References found.";
			LogMessage.Log(string.Format("Publishing ChangeSet: {0}, \"{1}\"; {2}", changeSet.Revision, changeSet.Message, referenceMessage), _eventManager);
			PublishChangeSet(changeSet);

			base.ProcessRevision(revision, author, changeDate, message, filesChanged, changedPathInfos);
		}

		protected virtual void PublishChangeSet(ChangeSetInfo changeSet)
		{
			_eventManager.Publish(changeSet);
		}

		private List<string> GetReferences(string message)
		{
			List<string> result = new List<string>();
			Regex expression = new Regex(ReferenceExpression);
			foreach (Match match in expression.Matches(message))
				result.Add(match.Value);

			return result;
		}
		
		public class SvnReaderIntervalSync { }
	}
}
