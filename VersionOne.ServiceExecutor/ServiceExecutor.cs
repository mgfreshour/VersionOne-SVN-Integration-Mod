/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections.Generic;
using System.Text;

namespace VersionOne.ServiceExecutor
{
	class ServiceExecutor
	{
		static void Main(string[] args)
		{
			new BatchMode().Run();

			if (args.Length > 0)
			{
				Console.WriteLine("Press any key to exit...");
				Console.ReadKey();
			}
		}
	}
}
