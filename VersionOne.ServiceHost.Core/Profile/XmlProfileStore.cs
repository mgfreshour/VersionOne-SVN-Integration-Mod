/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;

namespace VersionOne.Profile
{
	public class XmlProfileStore : IProfileStore
	{
		private IDictionary _values;
		private IDictionary _newvalues;
		private object _lock;
		private string _filename;
		private Stream _stream;

		private XmlProfileStore()
		{
			_values = new Hashtable();
			_newvalues = new Hashtable();
			_lock = new object();
		}

		public XmlProfileStore(string filename) : this()
		{
			_filename = filename;
			if ( !File.Exists(_filename) )
				throw new InvalidOperationException("Cannot find XML Profile File at " + _filename);
			XmlDocument doc = new XmlDocument();
			doc.Load(filename);
			LoadXml(doc.DocumentElement, ProfilePath.RootPath);
		}

		public XmlProfileStore(Stream xmlstream) : this()
		{
			_stream = xmlstream;
			xmlstream.Seek(0, SeekOrigin.Begin);
			XmlDocument doc = new XmlDocument();
			doc.Load(xmlstream);
			LoadXml(doc.DocumentElement, ProfilePath.RootPath);
		}

		private void LoadXml(XmlElement element, ProfilePath path)
		{
			if ( element.HasAttribute("value") )
			{
				string value = element.GetAttribute("value");
				LoadValue(path, value);
			}

			foreach (XmlNode xmlnode in element.ChildNodes)
			{
				XmlElement child = xmlnode as XmlElement;
				if ( child != null )
					LoadXml(child, new ProfilePath(path, XmlNormalizer.TagDecode(child.LocalName)));
			}
		}

		public IProfile this[string profilepath]
		{
			get
			{
				ProfilePath path = ProfilePath.RootPath.ResolvePath(profilepath);
				ProfileNode node = new ProfileNode(this, path);
				return node.IsRoot ? (IProfile) node : (IProfile) new VirtualNode(node);
			}
		}

		public void Flush()
		{
			lock (_lock)
			{
				if ( _newvalues.Count == 0 ) return;

				IDictionary result = new Hashtable(_values);
				IUpdate batch = CreateNewUpdate();

				foreach (DictionaryEntry newvalue in _newvalues)
				{
					ProfilePath path = (ProfilePath) newvalue.Key;
					string value = (string) newvalue.Value;

					if ( value == null )
					{
						if ( _values.Contains(path) )
						{
							batch.DeleteValue(path);
							result.Remove(path);
						}
					}
					else
					{
						if ( _values.Contains(path) )
						{
							batch.SaveValue(path, value);
							result[path] = value;
						}
						else
						{
							batch.AddValue(path, value);
							result.Add(path, value);
						}
					}
				}

				try
				{
					batch.Execute();

					_values = result;
					_newvalues.Clear();

					batch.AcceptChanges();
				}
				catch
#if DEBUG
					(Exception)
#endif
				{
					batch.DiscardChanges();
					throw;
				}
			}
		}

		public void DiscardChanges()
		{
			lock (_lock)
				_newvalues.Clear();
		}

		private string this[ProfilePath path]
		{
			get
			{
				lock (_lock)
				{
					if ( _newvalues.Contains(path) )
						return (string) _newvalues[path];
					return (string) _values[path];
				}
			}
			set
			{
				lock (_lock)
				{
					if ( value != (string) _values[path] )
						_newvalues[path] = value;
					else
						_newvalues.Remove(path);
				}
			}
		}

		private void LoadValue(ProfilePath path, string value)
		{
			_values[path] = value;
		}

		private interface IUpdate
		{
			void Execute();
			void AcceptChanges();
			void DiscardChanges();
			void SaveValue(ProfilePath path, string value);
			void AddValue(ProfilePath path, string value);
			void DeleteValue(ProfilePath path);
		}

		private IUpdate CreateNewUpdate ()
		{
			return new UpdateBatch(this);
		}

		private class UpdateBatch : IUpdate
		{
			private XmlProfileStore _store;
			private XmlProfileStore Store { get { return _store; } }

			private XmlDocument _document;

			public UpdateBatch(XmlProfileStore store)
			{
				_store = store;
				_document = new XmlDocument();
				if ( store._filename != null )
					_document.Load(store._filename);
				else
				{
					if ( !store._stream.CanWrite )
						throw new InvalidOperationException("Stream cannot be written to.");
					if ( !store._stream.CanSeek )
						throw new InvalidOperationException("Stream cannot seek.");

					store._stream.Seek(0, SeekOrigin.Begin);

					_document.Load(store._stream);

					store._stream.Seek(0, SeekOrigin.Begin);
				}
			}

			public void Execute()
			{
			}

			public void AcceptChanges()
			{
				XmlTextWriter writer = null;

				if ( Store._filename != null )
					writer = new XmlTextWriter(Store._filename, Encoding.UTF8);
				else
					writer = new XmlTextWriter(Store._stream, Encoding.UTF8);

				writer.Formatting = Formatting.Indented;

				XmlNodeReader reader = new XmlNodeReader(_document);

				writer.WriteNode(reader, true);

				writer.Flush();

				writer.BaseStream.SetLength(writer.BaseStream.Position);

				if ( Store._filename != null )
					writer.Close();
			}

			public void DiscardChanges()
			{
			}

			public void SaveValue(ProfilePath path, string value)
			{
				string name = EncodedPath(path);
				XmlNode node = _document.DocumentElement.SelectSingleNode(name);
				node.Attributes["value"].Value = value;
			}

			private static string EncodedPath(ProfilePath path)
			{
				string[] paths = new string[path.Length];
				for (int i = 0; i < path.Length; i++)
					paths[i] = XmlNormalizer.TagEncode(path[i]);
				return string.Join("/", paths);
			}

			public void AddValue(ProfilePath path, string value)
			{
				XmlNode rootnode = _document.DocumentElement;
				foreach (string name in path)
				{
					XmlNode node = null;
					string n = XmlNormalizer.TagEncode(name);
					node = rootnode.SelectSingleNode(n);
					if ( node == null )
					{
						node = _document.CreateElement(n);
						rootnode.AppendChild(node);
					}
					rootnode = node;
				}
				XmlAttribute attrib = _document.CreateAttribute("value");
				rootnode.Attributes.Append(attrib);
				attrib.Value = value;
			}

			public void DeleteValue(ProfilePath path)
			{
				string name = EncodedPath(path);
				XmlElement lastone = _document.DocumentElement.SelectSingleNode(name) as XmlElement;
				lastone.Attributes.RemoveNamedItem("value");
				XmlElement node = lastone.ParentNode as XmlElement;
				while (node != _document.DocumentElement)
				{
					if ( !node.HasAttribute("value") )
						lastone = node;

					node = node.ParentNode as XmlElement;
				}
				node.RemoveChild(lastone);
			}
		}

		private class ProfilePath : IEnumerable
		{
			private readonly string[] _path;

			private ProfilePath()
			{
				_path = new string[0];
			}

			public ProfilePath(string[] path)
			{
				if ( path == null || path.Length == 0 )
					throw new ArgumentNullException("path");
				for (int i = 0; i < path.Length; ++i)
				{
					string p = path[i];
					if ( p == null || p.Length == 0 )
						throw new ArgumentNullException(string.Format("path[{0}]", i));
				}

				_path = path;
			}

			public ProfilePath(ProfilePath parent, string child)
			{
				if ( child == null || child.Length == 0 )
					throw new ArgumentNullException("child");
				string[] parentpath = parent._path;
				int length = parentpath.Length;
				_path = new string[length + 1];
				Array.Copy(parentpath, _path, length);
				_path[length] = child;
			}

			internal ProfilePath(ProfilePath path)
			{
				_path = path._path;
			}

			protected ProfilePath(ProfilePath path, int start, int length)
			{
				_path = new string[length];
				Array.Copy(path._path, start, _path, 0, length);
			}

			public static readonly ProfilePath RootPath = new ProfilePath();

			public ProfilePath ParentPath
			{
				get
				{
					if ( IsRoot )
						throw new InvalidOperationException();
					if ( _path.Length > 1 ) return new ProfilePath(this, 0, _path.Length - 1);
					return RootPath;
				}
			}

			public string LastTerm
			{
				get
				{
					if ( IsRoot ) return string.Empty;
					return _path[_path.Length - 1];
				}
			}

			public int Length { get { return _path.Length; } }

			public string this[int index] { get { return _path[index]; } }

			public bool IsRoot { get { return _path.Length == 0; } }

			public ProfilePath ResolvePath(string childpath)
			{
				if ( childpath.Length == 0 )
					return this;

				Stack currentpath = new Stack();
				if ( childpath.StartsWith("//") )
					childpath = childpath.Substring(2);
				else if ( childpath[0] == '/' )
					childpath = childpath.Substring(1);
				else
				{
					foreach (string term in _path)
						currentpath.Push(term);
				}

				while (childpath.Length > 0)
				{
					string currentterm;
					int firstslash = childpath.IndexOf('/');
					if ( firstslash == 0 )
						throw new InvalidOperationException();
					else if ( firstslash == -1 )
					{
						currentterm = childpath;
						childpath = string.Empty;
					}
					else
					{
						currentterm = childpath.Substring(0, firstslash);
						childpath = childpath.Substring(firstslash + 1);
					}

					if ( currentterm == ".." )
					{
						if ( currentpath.Count > 0 )
							currentpath.Pop();
					}
					else if ( currentterm == "." )
					{
					}
					else
						currentpath.Push(currentterm);
				}

				if ( currentpath.Count == 0 )
					return RootPath;

				string[] terms = new string[currentpath.Count];
				for (int i = terms.Length; i > 0;)
					terms[--i] = (string) currentpath.Pop();

				return new ProfilePath(terms);
			}

			public override string ToString()
			{
				return string.Join("/", _path);
			}

			public override bool Equals(object obj)
			{
				if ( ReferenceEquals(obj, null) || GetType() != obj.GetType() )
					return false;

				ProfilePath other = (ProfilePath) obj;

				if ( ReferenceEquals(_path, other._path) ) return true;

				if ( _path.Length != other._path.Length ) return false;

				for (int i = 0; i < _path.Length; ++i)
					if ( !_path[i].Equals(other._path[i]) )
						return false;

				return true;
			}

			public override int GetHashCode()
			{
				return HashCode.Hash(_path);
			}

			public IEnumerator GetEnumerator()
			{
				return _path.GetEnumerator();
			}
		}

		private class ProfileNode : ProfilePath, IProfile
		{
			private XmlProfileStore _store;

			public ProfileNode(XmlProfileStore store, ProfilePath path)
				: base(path)
			{
				_store = store;
			}

			protected ProfileNode(XmlProfileStore store, ProfileNode parent, string child)
				: base(parent, child)
			{
				_store = store;
			}

			public string Path { get { return '/' + ToString(); } }

			public string Name { get { return LastTerm; } }

			public string Token { get { return Path; } }

			public string Value { get { return _store[new ProfilePath(this)]; } set { _store[new ProfilePath(this)] = value; } }

			public IProfile Parent
			{
				get
				{
					if ( IsRoot ) return null;
					return new ProfileNode(_store, ParentPath);
				}
			}

			IProfile IProfile.this[string childpath] { get { return this[childpath]; } }

			public ProfileNode this[string childpath]
			{
				get
				{
					if ( childpath.Length == 0 ) return this;
					return new ProfileNode(_store, ResolvePath(childpath));
				}
			}
		}
	}
}