/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using SharpSvn;

namespace VersionOne.ServiceHost.SourceServices.Subversion
{
	public class ChangedPathInfo : IChangedPathInfo
	{
        private readonly SvnChangeItem changeItem;

        // TODO resolve whether this is really required, may be removed or changed to SharpSvn enum type
        public SubversionAction Action
        {
            get 
            {
                switch(changeItem.Action)
                {
                    case SvnChangeAction.Add:
                        return SubversionAction.Add;
                    case SvnChangeAction.Delete:
                        return SubversionAction.Delete;
                    case SvnChangeAction.Modify:
                        return SubversionAction.Modify;
                    case SvnChangeAction.Replace:
                        return SubversionAction.Replace;
                }
                return SubversionAction.Unknown;
            } 
        }
		
        public int CopyFromRevision
        {
            get { return Convert.ToInt32(changeItem.CopyFromRevision); }
        }

        public string CopyFromPath
        {
            get { return changeItem.CopyFromPath; }
        }

        public ChangedPathInfo(SvnChangeItem item)
        {
            changeItem = item;
        }

		protected ChangedPathInfo() {}
	}

	public class ChangeSetDictionary : Dictionary<string, IChangedPathInfo> { }

	public enum SubversionAction
	{
		Add,
		Delete,
		Replace,
		Modify,
		Unknown
	}
}