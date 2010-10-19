/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using VersionOne.ServiceHost.Eventing;

namespace VersionOne.ServiceHost.Logging
{
	public class LogMessage
	{
		public enum SeverityType
		{
			Debug,			
			Info,
			Error
		}

		public readonly SeverityType Severity;
		public readonly string Message;
		public readonly Exception Exception;
		public readonly DateTime Stamp;
		
		private LogMessage(SeverityType severity, string message, Exception exception)
		{
			Severity = severity;
			Message = message;
			Exception = exception;
			Stamp = DateTime.Now;
		}
		
		public static void Log(string message, IEventManager eventmanager)
		{
			Log(SeverityType.Info, message, null, eventmanager);
		}

		public static void Log(string message, Exception exception, IEventManager eventmanager)
		{
			Log(SeverityType.Error, message, exception, eventmanager);
		}		
		
		public static void Log(SeverityType severity, string message, IEventManager eventmanager)
		{
			Log(severity, message, null, eventmanager);
		}
		
		public static void Log(SeverityType severity, string message, Exception exception, IEventManager eventmanager)
		{
			eventmanager.Publish(new LogMessage(severity, message, exception));
		}
	}
}