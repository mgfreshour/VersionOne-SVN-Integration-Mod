/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;

namespace VersionOne.Profile
{
	internal class VirtualNode : IProfile
	{
		protected IProfile _rootnode;
		protected IProfile _node;

		private VirtualNode(IProfile rootnode, IProfile node)
		{
			if (!node.Path.StartsWith(rootnode.Path))
				throw new ArgumentOutOfRangeException("node");

			_rootnode = rootnode;
			_node = node;
		}

		public VirtualNode(IProfile rootnode) : this(rootnode, rootnode) { }

		IProfile IProfile.this[string childpath]
		{
			get
			{
				if (childpath.Length == 0)
					return this;

				IProfile resolver = this;

				if (childpath.StartsWith("//"))
				{
					childpath = childpath.Substring(2);
					resolver = new VirtualNode(_rootnode);
				}
				else if (childpath[0] == '/')
				{
					childpath = childpath.Substring(1);
					resolver = new VirtualNode(_rootnode);
				}
				else
				{
					string currentterm;
					int firstslash = childpath.IndexOf('/');
					if (firstslash == -1)
					{
						currentterm = childpath;
						childpath = string.Empty;
					}
					else
					{
						currentterm = childpath.Substring(0, firstslash);
						childpath = childpath.Substring(firstslash + 1);
					}

					if (currentterm == "..")
					{
						if (_node.Path != _rootnode.Path)
							resolver = new VirtualNode(_rootnode, _node.Parent);
					}
					else if (currentterm == ".")
					{
					}
					else
					{
						IProfile childnode = _node[currentterm];
						if (childnode == null)
							return null;
						resolver = new VirtualNode(_rootnode, childnode);
					}
				}

				if (childpath.Length == 0)
					return resolver;

				return resolver[childpath];
			}
		}


		string IProfile.Path
		{
			get
			{
				string path = _node.Path.Substring(_rootnode.Path.Length);
				if (!path.StartsWith("/"))
					path = "/" + path;
				return path;
			}
		}

		string IProfile.Value
		{
			get { return _node.Value; }
			set { _node.Value = value; }
		}
 
		string IProfile.Name { get { return _node.Name; } }

		IProfile IProfile.Parent
		{
			get
			{
				if (_node.Path == _rootnode.Path) return null;
				return new VirtualNode(_rootnode, _node.Parent);
			}
		}
 	}
}