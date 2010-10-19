/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;
using VersionOne.ServiceHost.HostedServices;

namespace VersionOne.ServiceHost
{
	public class ServicesConfigurationHandler : IConfigurationSectionHandler
	{
		public object Create(object parent, object configContext, XmlNode section)
		{
			return new ServicesConfiguration(section);
		}
	}

	public class ServiceInfo
	{
		public readonly string Name;
		public readonly XmlElement Config;
		public readonly IHostedService Service;
		
		public ServiceInfo(string name, IHostedService svc, XmlElement config)
		{
			Name = name;
			Service = svc;
			Config = config;
		}
	}
	
	internal class ServicesConfiguration : List<ServiceInfo>
	{
		public ServicesConfiguration(XmlNode section)
		{
			foreach (XmlNode child in section.ChildNodes)
			{
				if (child.NodeType != XmlNodeType.Element)
					continue;

				XmlAttribute disabledAttribute = child.Attributes["disabled"];
				if (disabledAttribute != null)
				{
					if ( disabledAttribute.Value == "1" || disabledAttribute.Value.ToLower() == "true" )
					{
						continue;
					}
				}

				XmlAttribute attrib = child.Attributes["class"];
				if (attrib == null)
					continue;

				Type type = Type.GetType(attrib.Value);

				try
				{
					IHostedService svc = (IHostedService)Activator.CreateInstance(type);
					Add(new ServiceInfo(child.LocalName, svc, (XmlElement)child));
					Console.WriteLine("Loaded {0}.", attrib.Value);
				}
				catch (Exception ex)
				{
					Console.WriteLine( "Failed to load {0}.\r\n{1}", attrib.Value, ex );					
				}
			}
		}
	}
}