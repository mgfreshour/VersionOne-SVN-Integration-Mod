/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("VersionOne, Inc.")]
[assembly: AssemblyCopyright("Copyright � 2007, VersionOne, Inc. All rights reserved.")]
[assembly: AssemblyTrademark("")]

#if DEBUG
[assembly: AssemblyDescription("Debug")]
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyDescription("Release")]
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: ComVisible(false)]

