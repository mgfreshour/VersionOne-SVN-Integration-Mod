/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;

namespace VersionOne.ServiceHost.SourceServices 
{
    public class ExceptionEventArgs : EventArgs 
    {
        public readonly Exception Exception;

        public ExceptionEventArgs(Exception exception) 
        {
            Exception = exception;
        }
    }
}