/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using System.Xml;
using VersionOne.SDK.APIClient;
using VersionOne.Profile;
using VersionOne.ServiceHost.Core.Services;
using VersionOne.ServiceHost.Eventing;
using VersionOne.ServiceHost.HostedServices;
using VersionOne.ServiceHost.Logging;
using Attribute=VersionOne.SDK.APIClient.Attribute;
// for the basic object model
using VersionOne.SDK.ObjectModel;
// if you need to use filtering in getting filtered subsets of relations or collections of assets
using VersionOne.SDK.ObjectModel.Filters;


namespace VersionOne.ServiceHost.SourceServices
{
	public class ChangeSetWriterService : V1WriterServiceBase, IHostedService
	{
		private string _changecomment;
		private string _referencename;
		private bool _alwaysCreate;
		private V1Instance _v1;

		private LinkInfo _linkinfo;

		private class LinkInfo
		{
			public readonly string Name;
			public readonly string URL;
			public readonly bool OnMenu;
			public LinkInfo(string name, string url, bool onmenu)
			{
				Name = name;
				URL = url;
				OnMenu = onmenu;
			}
		} // end class LinkInfo

		public override void Initialize(XmlElement config, IEventManager eventManager, IProfile profile)
		{
			if (String.IsNullOrEmpty(config["Settings"]["Username"].InnerText))
			{
				Console.Write("Please enter v1 Username : ");
				config["Settings"]["Username"].InnerText = Console.ReadLine();
			}
			if (String.IsNullOrEmpty(config["Settings"]["Password"].InnerText))
			{
				Console.Write("Please enter v1 Password : ");
				config["Settings"]["Password"].InnerText = Console.ReadLine();
			}


			_v1 = new V1Instance(config["Settings"]["ApplicationUrl"].InnerText, config["Settings"]["Username"].InnerText, config["Settings"]["Password"].InnerText);

			base.Initialize(config, eventManager, profile);
			_changecomment = _config["ChangeComment"].InnerText;
			_referencename = _config["ReferenceAttribute"].InnerText;

			bool alwaysCreate = false;
			if (_config["AlwaysCreate"] != null)
				bool.TryParse(_config["AlwaysCreate"].InnerText, out alwaysCreate);
			_alwaysCreate = alwaysCreate;

			VerifyMeta();
			LoadLinkInfo();
			_eventManager.Subscribe(typeof(ChangeSetInfo), ChangeSetListener);
		} // end Initialize( )

		protected override void VerifyRuntimeMeta()
		{
			base.VerifyRuntimeMeta();
			IAttributeDefinition refdef = PrimaryWorkitemReferenceDef;
		} // end VerifyRuntimeMeta( )

		private void LoadLinkInfo()
		{
			XmlElement linkroot = _config["Link"];
			if (linkroot != null)
			{
				XmlElement namenode = linkroot["Name"];
				XmlElement linknode = linkroot["URL"];
				XmlElement onmenunode = linkroot["OnMenu"];
				if (namenode != null && linknode != null && onmenunode != null)
				{
					string name = namenode.InnerText;
					string url = linknode.InnerText;
					bool onmenu;
					if (!bool.TryParse(onmenunode.InnerText, out onmenu))
						onmenu = false;

					if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(url))
						_linkinfo = new LinkInfo(name, url, onmenu);
				}
			}
		} // end LoadLinkInfo( )

		#region Meta Properties

		private IAssetType ChangeSetType { get { return Central.MetaModel.GetAssetType("ChangeSet"); } }
		private IAttributeDefinition ChangeSetPrimaryWorkitemsDef { get { return Central.MetaModel.GetAttributeDefinition("ChangeSet.PrimaryWorkitems"); } }
		private IAttributeDefinition ChangeSetNameDef { get { return Central.MetaModel.GetAttributeDefinition("ChangeSet.Name"); } }
		private IAttributeDefinition ChangeSetReferenceDef { get { return Central.MetaModel.GetAttributeDefinition("ChangeSet.Reference"); } }
		private IAttributeDefinition ChangeSetDescriptionDef { get { return Central.MetaModel.GetAttributeDefinition("ChangeSet.Description"); } }

		private IAssetType PrimaryWorkitemType { get { return Central.MetaModel.GetAssetType("PrimaryWorkitem"); } }
		private IAttributeDefinition PrimaryWorkitemReferenceDef { get { return Central.MetaModel.GetAttributeDefinition("PrimaryWorkitem.ChildrenMeAndDown." + _referencename); } }

		private IAttributeDefinition LinkNameDef { get { return Central.MetaModel.GetAttributeDefinition("Link.Name"); } }
		private IAttributeDefinition LinkURLDef { get { return Central.MetaModel.GetAttributeDefinition("Link.URL"); } }
		private IAttributeDefinition LinkOnMenuDef { get { return Central.MetaModel.GetAttributeDefinition("Link.OnMenu"); } }

		private new string StoryName { get { return Central.Loc.Resolve("Plural'Story"); } }
		private new string DefectName { get { return Central.Loc.Resolve("Plural'Defect"); } }

		private static NeededAssetType[] _neededassettypes = 
		{
			new NeededAssetType("ChangeSet", new string[]{"PrimaryWorkitems", "Name", "Reference", "Description"}),
			new NeededAssetType("PrimaryWorkitem", new string[]{}),
			new NeededAssetType("Link", new string[]{"Name", "URL", "OnMenu"}),
		};

		protected override NeededAssetType[] NeededAssetTypes { get { return _neededassettypes; } }
		#endregion

		private void ChangeSetListener(object pubobj)
		{
			ChangeSetInfo info = (ChangeSetInfo)pubobj;
			if (info.References.Count == 0)
			{
				return;
			}

			try
			{
				ProcessChangeSetInfo(info);
			}
			catch (Exception ex)
			{
				LogMessage.Log(string.Format("Process Change Set {0} Info Failed: {1}", info.Revision, ex), _eventManager);
			}
		} // end ChangeSetListener( )

		private void ProcessChangeSetInfo(ChangeSetInfo info)
		{
			// Find related work items
			var work_items = new List<PrimaryWorkitem>();
			foreach (string refe in info.References)
			{
				var wi = _v1.Get.PrimaryWorkitemByDisplayID(refe);
				if (wi == null) {
					// Let's see if this is a secondary work item
					var sec_wi = _v1.Get.SecondaryWorkitemByDisplayID(refe);
					if (sec_wi != null) {
						wi = sec_wi.Parent;
					} else {
						// We can't find it, let's just get out of here
						return;
					}
				}
				work_items.Add(wi);
			}

			// Find or create the changeset asset
			var cs_filter = new ChangeSetFilter();
			cs_filter.Reference.Add(info.Revision);
			var change_sets = _v1.Get.ChangeSets(cs_filter);
			string cs_name = string.Format("'{0}' on '{1}' - {2}", info.Author, TimeZone.CurrentTimeZone.ToLocalTime(info.ChangeDate), info.Message);
			string cs_ref = string.Format("{0} - {1}", info.ReposName, info.Revision);
			if (change_sets.Count == 0)
			{
				change_sets = new List<ChangeSet>();
				var cs = _v1.Create.ChangeSet(info.Message, cs_ref);
				change_sets.Add(cs);
			}

			//Update changeset asset
			foreach (ChangeSet cs in change_sets)
			{
				foreach (var wi in work_items)
				{
					cs.PrimaryWorkitems.Add(wi);
					cs.Name = cs_name;
					cs.Reference = cs_ref;
				}
			}

			// Find or create the links to the work item
			var link_url = string.Format(info.ReferenceUrl, info.Revision);
			var link_name = string.Format(_linkinfo.Name, info.Revision);
			var link_filter = new LinkFilter();
			link_filter.URL.Add(link_url);
			foreach (var wi in work_items)
			{
				var links = wi.GetLinks(link_filter);
				if (links.Count > 0)
				{
					continue;
				}
				wi.CreateLink(link_name, link_url, true);
			}
		} // end ProcessChangeSetInfoByModels( )
	} // end class ChangeSetWriterService
} // end namespace VersionOne.ServiceHost.SourceServices