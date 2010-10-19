/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System.Configuration;
using System.Xml;

namespace VersionOne.ServiceHost
{
	public class InstallerConfigurationHandler : IConfigurationSectionHandler
	{
		public object Create(object parent, object configContext, XmlNode section)
		{
			return new InstallerConfiguration(section);
		}
	}

	internal class InstallerConfiguration
	{
		public readonly string ShortName;
		public readonly string LongName;

		public InstallerConfiguration(XmlNode section)
		{
			XmlElement shortnode = section["ShortName"];
			if (shortnode == null)
				throw new ConfigurationErrorsException("Missing Short Name Element", section);
			ShortName = shortnode.InnerText;
			XmlElement longnode = section["LongName"];
			if (longnode == null)
				throw new ConfigurationErrorsException("Missing Long Name Element", section);
			LongName = longnode.InnerText;
		}
	}
}