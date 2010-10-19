/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;

namespace VersionOne.ServiceHost.SourceServices
{
	public class ChangeSetInfo
	{
		public readonly string Author;
		public readonly string Message;
		public readonly IList<string> ChangedFiles;
		public readonly DateTime ChangeDate;
		public readonly string Revision;
		public readonly IList<string> References;
        public readonly string ReferenceUrl;
        public readonly string ReposName;

        /*
	    public int RevisionAsInteger {
	        get {
	            int revision;
	            return int.TryParse(Revision, out revision) ? revision : 0;
	        }
	    }
         * */
		
		public ChangeSetInfo(string author, string message, IList<string> changedfiles, string revision, DateTime changedate, IList<string> references, string referenceUrl, string repos_name)
		{
			Author = author;
			Message = message;
			ChangedFiles = changedfiles;
			Revision = revision;
			ChangeDate = changedate;
			References = references;
            ReferenceUrl = referenceUrl;
            ReposName = repos_name;
		}
	}
}
