/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using System.Text;

namespace VersionOne.ServiceHost.SourceServices.Subversion
{
	public class PropertiesCollection : Dictionary<string, Dictionary<string, string>>
	{
		
	}

	public class RevisionPropertyCollection : Dictionary<string, string>
	{
		public readonly int Revision;

		public RevisionPropertyCollection(int revision)
		{
			Revision = revision;
		}
	}
}
