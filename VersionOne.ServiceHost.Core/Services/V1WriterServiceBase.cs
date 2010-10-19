/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using VersionOne.SDK.APIClient;
using VersionOne.ServiceHost.Eventing;
using VersionOne.Profile;
using VersionOne.ServiceHost.HostedServices;

namespace VersionOne.ServiceHost.Core.Services
{
	public abstract class V1WriterServiceBase : IHostedService
	{
		private ICentral _central;
		protected XmlElement _config;
		protected IEventManager _eventManager;

		protected virtual ICentral Central
		{
			get
			{
				if (_central == null)
				{
					V1Central c = new V1Central(_config["Settings"]);
					c.Validate();
					_central = c;
				}
				return _central;
			}
		}

		public virtual void Initialize (XmlElement config, IEventManager eventManager, IProfile profile)
		{
			_config = config;
			_eventManager = eventManager;
		}

		protected abstract NeededAssetType[] NeededAssetTypes { get; }

		protected void VerifyMeta()
		{
			try
			{
				VerifyNeededMeta(NeededAssetTypes);
				VerifyRuntimeMeta();
			}
			catch (MetaException e)
			{
				throw new ApplicationException("Necessary meta is not present in this VersionOne system", e);
			}
		}

		protected virtual void VerifyRuntimeMeta() { }

		protected struct NeededAssetType
		{
			public readonly string Name;
			public readonly string[] AttributeDefinitionNames;
			public NeededAssetType (string name, string[] attributedefinitionnames)
			{
				Name = name;
				AttributeDefinitionNames = attributedefinitionnames;
			}
		}

		protected void VerifyNeededMeta (NeededAssetType[] neededassettypes)
		{
			foreach (NeededAssetType neededAssetType in neededassettypes)
			{
				IAssetType assettype = Central.MetaModel.GetAssetType(neededAssetType.Name);
				foreach (string attributeDefinitionName in neededAssetType.AttributeDefinitionNames)
				{
					IAttributeDefinition attribdef = assettype.GetAttributeDefinition(attributeDefinitionName);
				}
			}
		}

		#region Meta wrappers
		protected IAssetType RequestType { get { return Central.MetaModel.GetAssetType("Request"); } }
		protected IAssetType DefectType { get { return Central.MetaModel.GetAssetType("Defect"); } }
		protected IAssetType StoryType { get { return Central.MetaModel.GetAssetType("Story"); } }
		protected IAssetType ReleaseVersionType { get { return Central.MetaModel.GetAssetType("StoryCategory"); } }
		protected IAssetType LinkType { get { return Central.MetaModel.GetAssetType("Link"); } }
		protected IAssetType NoteType { get { return Central.MetaModel.GetAssetType("Note"); } }

		protected IAttributeDefinition DefectName { get { return DefectType.GetAttributeDefinition("Name"); } }
		protected IAttributeDefinition DefectDescription { get { return DefectType.GetAttributeDefinition("Description"); } }
		protected IAttributeDefinition DefectOwners { get { return DefectType.GetAttributeDefinition("Owners"); } }
		protected IAttributeDefinition DefectScope { get { return DefectType.GetAttributeDefinition("Scope"); } }
		protected IAttributeDefinition DefectAssetState { get { return RequestType.GetAttributeDefinition("AssetState"); } }

		protected IAttributeDefinition RequestCompanyName { get { return RequestType.GetAttributeDefinition("Name"); } }
		protected IAttributeDefinition RequestNumber { get { return RequestType.GetAttributeDefinition("Number"); } }
		protected IAttributeDefinition RequestSuggestedInstance { get { return RequestType.GetAttributeDefinition("Reference"); } }
		protected IAttributeDefinition RequestMethodology { get { return RequestType.GetAttributeDefinition("Source"); } }
		protected IAttributeDefinition RequestMethodologyName { get { return RequestType.GetAttributeDefinition("Source.Name"); } }
		protected IAttributeDefinition RequestCommunityEdition { get { return RequestType.GetAttributeDefinition("Custom_CommunityEdition"); } }
		protected IAttributeDefinition RequestAssetState { get { return RequestType.GetAttributeDefinition("AssetState"); } }
		protected IAttributeDefinition RequestCreateDate { get { return RequestType.GetAttributeDefinition("CreateDate"); } }
		protected IAttributeDefinition RequestCreatedBy { get { return RequestType.GetAttributeDefinition("CreatedBy"); } }

		protected IOperation RequestInactivate { get { return Central.MetaModel.GetOperation("Request.Inactivate"); } }

		protected IAttributeDefinition StoryName { get { return StoryType.GetAttributeDefinition("Name"); } }
		protected IAttributeDefinition StoryActualInstance { get { return StoryType.GetAttributeDefinition("Reference"); } }
		protected IAttributeDefinition StoryRequests { get { return StoryType.GetAttributeDefinition("Requests"); } }
		protected IAttributeDefinition StoryReleaseVersion { get { return StoryType.GetAttributeDefinition("Category"); } }
		protected IAttributeDefinition StoryMethodology { get { return StoryType.GetAttributeDefinition("Source"); } }
		protected IAttributeDefinition StoryCommunitySite { get { return StoryType.GetAttributeDefinition("Custom_CommunitySite"); } }
		protected IAttributeDefinition StoryScope { get { return StoryType.GetAttributeDefinition("Scope"); } }
		protected IAttributeDefinition StoryOwners { get { return StoryType.GetAttributeDefinition("Owners"); } }

		protected IAttributeDefinition ReleaseVersionName { get { return ReleaseVersionType.GetAttributeDefinition("Name"); } }

		protected IAttributeDefinition LinkAsset { get { return LinkType.GetAttributeDefinition("Asset"); } }
		protected IAttributeDefinition LinkOnMenu { get { return LinkType.GetAttributeDefinition("OnMenu"); } }
		protected IAttributeDefinition LinkURL { get { return LinkType.GetAttributeDefinition("URL"); } }
		protected IAttributeDefinition LinkName { get { return LinkType.GetAttributeDefinition("Name"); } }

		protected IAttributeDefinition NoteName { get { return NoteType.GetAttributeDefinition("Name"); } }
		protected IAttributeDefinition NoteAsset { get { return NoteType.GetAttributeDefinition("Asset"); } }
		protected IAttributeDefinition NotePersonal { get { return NoteType.GetAttributeDefinition("Personal"); } }
		protected IAttributeDefinition NoteContent { get { return NoteType.GetAttributeDefinition("Content"); } }
		#endregion

	}
}
