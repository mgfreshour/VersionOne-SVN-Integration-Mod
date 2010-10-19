/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Reflection;
using System.Xml;

namespace VersionOne.ServiceHost.Core.Utility
{
	public class ConfigurationReader
	{
		private static string Read(XmlElement config, string name, string def)
		{
			return config[name] != null ? config[name].InnerText : def;
		}

		private static bool Read(XmlElement config, string name, bool def)
		{
			bool res = def;
			if (config[name] != null)
				if (!bool.TryParse(config[name].InnerText, out res))
					res = def;
			return res;
		}

		public static void ReadConfigurationValues<T>(T config, XmlElement configSection)
		{
			FieldInfo[] configFields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);

			foreach (FieldInfo fieldInfo in configFields)
			{
				object[] tagNames = fieldInfo.GetCustomAttributes(typeof(ConfigFileValueAttribute), false);

				if (tagNames.Length == 1)
				{
					ConfigFileValueAttribute tagName = tagNames[0] as ConfigFileValueAttribute;
					if (tagName.IsBool)
					{
						fieldInfo.SetValue(config, Read(configSection, tagName.Name, bool.Parse(tagName.DefaultValue)));
					}
					else
					{
						fieldInfo.SetValue(config, Read(configSection, tagName.Name, tagName.DefaultValue));
					}

					System.Diagnostics.Debug.WriteLine(string.Format("Set '{0}' to '{1}'.", fieldInfo.Name, fieldInfo.GetValue(config)));
				}
				else
				{
					throw new ConfigurationException(string.Format("No tag specified for {0}.", fieldInfo.Name));
				}
			}
		}
	}


	[AttributeUsage(AttributeTargets.Field)]
	public class ConfigFileValueAttribute : Attribute
	{
		public ConfigFileValueAttribute(string name)
		{
			_name = name;
		}

		public ConfigFileValueAttribute(string name, string defaultValue)
		{
			_name = name;
			_defaultValue = defaultValue;
		}

		public ConfigFileValueAttribute(string name, string @defaultValue, bool isBool)
		{
			_name = name;
			_defaultValue = defaultValue;
			_isBool = isBool;
		}

		#region Name property
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}
		#endregion

		#region DefaultValue property
		private string _defaultValue = string.Empty;
		public string DefaultValue
		{
			get { return _defaultValue; }
			set { _defaultValue = value; }
		}
		#endregion

		#region IsBool property
		private bool _isBool = false;
		public bool IsBool
		{
			get { return _isBool; }
			set { _isBool = value; }
		}
		#endregion
	}

	public class ConfigurationException : Exception
	{
		public ConfigurationException(string message)
			: base(message)
		{
		}
	}

}
