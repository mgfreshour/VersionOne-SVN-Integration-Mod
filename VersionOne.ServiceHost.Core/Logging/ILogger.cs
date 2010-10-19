/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using System.Text;
using VersionOne.ServiceHost.Eventing;
using VersionOne.ServiceHost.Logging;

namespace VersionOne.ServiceHost.Logging
{
    /// <summary>
    /// Allows classes that don't need to know about the EventManager to log errors and debug info in an abstract manner
    /// </summary>
    public interface ILogger
    {
        void Error(string message);
        void Debug(string message);
        void Info(string message);
    }

    public class EventManagerLogger : ILogger
    {
        private readonly IEventManager _eventManager;

        public EventManagerLogger(IEventManager eventManager)
        {
            _eventManager = eventManager;
        }

        public void Error(string message)
        {
            LogMessage.Log(LogMessage.SeverityType.Error, message,_eventManager);
        }

        public void Debug(string message)
        {
            LogMessage.Log(LogMessage.SeverityType.Debug, message, _eventManager);
        }

        public void Info(string message)
        {
            LogMessage.Log(LogMessage.SeverityType.Info, message, _eventManager);
        }
    }
}