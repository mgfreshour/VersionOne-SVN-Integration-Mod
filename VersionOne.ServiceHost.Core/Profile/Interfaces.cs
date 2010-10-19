/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
namespace VersionOne.Profile
{
	public interface IProfileStore
	{
		IProfile this[string profilepath] { get; }
		void Flush();
		void DiscardChanges();
	}

	public interface IProfile
	{
		string Path { get;}
		string Name { get;}
		string Value { get; set;}
		IProfile Parent { get;}
		IProfile this[string childpath] { get;}
	}
}