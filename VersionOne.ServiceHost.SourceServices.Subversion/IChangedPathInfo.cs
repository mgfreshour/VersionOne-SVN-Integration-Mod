/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
namespace VersionOne.ServiceHost.SourceServices.Subversion
{
    public interface IChangedPathInfo
    {
        SubversionAction Action { get; }

        int CopyFromRevision { get; }

        string CopyFromPath { get; }
    }
}