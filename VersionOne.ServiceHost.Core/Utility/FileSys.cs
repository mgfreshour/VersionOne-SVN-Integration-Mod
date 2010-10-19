/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace VersionOne.ServiceHost.Core
{
	public sealed class FileSys
	{
		[DllImport("kernel32.dll")]
		private static extern bool MoveFileEx(string source, string dest, uint flags);

		public static string TempName(string basename)
		{
			long ticks = DateTime.Now.Ticks;
			return string.Format("{0}.{1:X}", basename, ticks);
		}

		public static void DeleteFolder(string folder)
		{
			DeleteFolder(folder, false);
		}

		public static void DeleteFolder(string folder, bool allowDelay)
		{
			if (Directory.Exists(folder))
				InternalDeleteFolder(folder, allowDelay);
		}

		private static void InternalDeleteFolder(string folder, bool allowDelay)
		{
			try
			{
				Directory.Delete(folder, true);
				return;
			}
			catch
			{
			}

			foreach (string file in Directory.GetFiles(folder))
				InternalDeleteFile(file, allowDelay);
			foreach (string subfolder in Directory.GetDirectories(folder))
				InternalDeleteFolder(subfolder, allowDelay);

			try
			{
				Directory.Delete(folder, false);
			}
			catch
			{
				if (!allowDelay) throw;
				MoveFileEx(folder, null, 4); //MOVEFILE_DELAY_UNTIL_REBOOT
			}
		}

		public static void CreateFolder(string folder)
		{
			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);
		}

		public static void CopyFolder(string sourcefolder, string destfolder)
		{
			CreateFolder(destfolder);
			foreach (string sourcefile in Directory.GetFiles(sourcefolder))
				CopyFile(sourcefile, Path.Combine(destfolder, Path.GetFileName(sourcefile)));
			foreach (string subfolder in Directory.GetDirectories(sourcefolder))
				CopyFolder(subfolder, Path.Combine(destfolder, Path.GetFileName(subfolder)));
		}

		public static void MoveFolder(string sourcefolder, string destfolder)
		{
			DeleteFolder(destfolder);
			Directory.Move(sourcefolder, destfolder);
		}

		public static void DeleteFile(string file)
		{
			DeleteFile(file, false);
		}

		public static void DeleteFile(string file, bool allowDelay)
		{
			if (File.Exists(file))
				InternalDeleteFile(file, allowDelay);
		}

		private static void InternalDeleteFile(string file, bool allowDelay)
		{
			File.SetAttributes(file, FileAttributes.Normal);
			try
			{
				File.Delete(file);
			}
			catch
			{
				if (!allowDelay) throw;
				MoveFileEx(file, null, 4); //MOVEFILE_DELAY_UNTIL_REBOOT
			}
		}

		public static void CopyFile(string sourcefile, string destfile)
		{
			DeleteFile(destfile);
			CreateFolder(Path.GetDirectoryName(destfile));
			File.Copy(sourcefile, destfile);
		}

		public static void MoveFile(string sourcefile, string destfile)
		{
			DeleteFile(destfile);
			CreateFolder(Path.GetDirectoryName(destfile));
			File.SetAttributes(sourcefile, FileAttributes.Normal);
			File.Move(sourcefile, destfile);
		}

		public static void DeleteEmptyFolder(string filename)
		{
			string foldername = Path.GetDirectoryName(filename);
			while (foldername != null || foldername != string.Empty)
			{
				if (!Directory.Exists(foldername) || Directory.GetFileSystemEntries(foldername).Length > 0)
					return;
				Directory.Delete(foldername);
				foldername = Path.GetDirectoryName(foldername);
			}
		}

		/// <summary>
		/// Copies a file. If the destination already exists, rename it.
		/// </summary>
		/// <returns>The new name of the backed-up original file.</returns>
		public static string SafeCopyFile(string sourcefile, string destfile)
		{
			string backup = null;
			if (File.Exists(destfile))
			{
				backup = TempName(destfile);
				MoveFile(destfile, backup);
			}
			else
				CreateFolder(Path.GetDirectoryName(destfile));
			File.Copy(sourcefile, destfile);
			return backup;
		}

		/// <summary>
		/// Deletes backup information from SafeCopyFile
		/// </summary>
		/// <param name="backup">Returned backup name from original SafeCopyFile call</param>
		public static void CommitFile(string backup)
		{
			if (backup != null)
				DeleteFile(backup);
		}

		/// <summary>
		/// Restores a file created by SafeCopyFile
		/// </summary>
		/// <param name="destfile">Destination file from original SafeCopyFile call</param>
		/// <param name="backup">Returned backup name from original SafeCopyFile call</param>
		public static void RollbackFile(string destfile, string backup)
		{
			if (backup == null || !File.Exists(backup))
				DeleteFile(destfile);
			else
				MoveFile(backup, destfile);
		}

		public static string SafeCopyFolder(string sourcefolder, string destfolder)
		{
			string backup = null;
			if (Directory.Exists(destfolder))
			{
				backup = TempName(destfolder);
				MoveFolder(destfolder, backup);
			}
			CopyFolder(sourcefolder, destfolder);
			return backup;
		}

		public static void CommitFolder(string backup)
		{
			if (backup != null)
				DeleteFolder(backup);
		}

		public static void RollbackFolder(string destfolder, string backup)
		{
			if (backup == null || !Directory.Exists(backup))
				DeleteFolder(destfolder);
			else
				MoveFolder(backup, destfolder);
		}

		public static Version GetFileVersion(string filename)
		{
			Version ver = null;
			try
			{
				ver = AssemblyName.GetAssemblyName(filename).Version;
			}
			catch
			{
			}
			return ver;
		}

		/// <summary>
		/// Checks if a source file is newer than a destination file.
		/// Uses assembly version if available, then falls back to file time.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="dest"></param>
		/// <returns>True if source is newer.  False otherwise.</returns>
		public static bool IsSourceFileNewer(string source, string dest)
		{
			// if destination does not exist, then source is newer by default
			if (!File.Exists(dest))
				return true;

			// try to compare assembly versions
			Version sourceVer = GetFileVersion(source);
			Version destVer = GetFileVersion(dest);

			if (sourceVer != null && destVer != null)
			{
				if (sourceVer > destVer)
					return true;
				if (sourceVer < destVer)
					return false;
			}
			if (sourceVer != null && destVer == null)
				return true;
			if (sourceVer == null && destVer != null)
				return false;

			// either both have no version or same version
			// compare file times
			DateTime sourceTime = DateTime.MinValue, destTime = DateTime.MinValue;
			try
			{
				sourceTime = File.GetLastWriteTime(source);
			}
			catch
			{
			}
			try
			{
				destTime = File.GetLastWriteTime(dest);
			}
			catch
			{
			}

			return (sourceTime > destTime);
		}

		public static int EntrySize(string fileordir)
		{
			if (File.Exists(fileordir))
				return 1;
			if (Directory.Exists(fileordir))
			{
				int entrysize = 1;
				foreach (string entry in Directory.GetFileSystemEntries(fileordir))
					entrysize += EntrySize(entry);
				return entrysize;
			}
			return 0;
		}

		public static string[] GetFiles(string foldername, string searchpattern, bool recurse)
		{
			if (!recurse)
				return Directory.GetFiles(foldername, searchpattern);

			ArrayList filenames = new ArrayList();
			GetFilesRecursive(foldername, searchpattern, filenames);
			return (string[])filenames.ToArray(typeof(string));
		}

		private static void GetFilesRecursive(string foldername, string searchpattern, ArrayList filenames)
		{
			filenames.AddRange(Directory.GetFiles(foldername, searchpattern));
			foreach (string subfoldername in Directory.GetDirectories(foldername))
				GetFilesRecursive(subfoldername, searchpattern, filenames);
		}

		public static string ReadTextFile(string filename)
		{
			using (StreamReader input = new StreamReader(filename))
				return input.ReadToEnd();
		}

		public static bool FolderInUse(string folder)
		{
			if (Directory.Exists(folder))
			{
				try { Touch(folder); }
				catch { return true; }
			}
			return false;
		}

		public static bool FileInUse(string path)
		{
			if (File.Exists(path))
			{
				try { Touch(path); }
				catch { return true; }
			}
			return false;
		}

		public static void Touch(string path)
		{
			FileAttributes attribs = File.GetAttributes(path);
			bool writeable = ((attribs & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly)) == 0);
			if (!writeable)
				File.SetAttributes(path, FileAttributes.Normal);
			try
			{
				if ((attribs & FileAttributes.Directory) == FileAttributes.Directory)
					Directory.SetLastWriteTimeUtc(path, DateTime.UtcNow);
				else
					File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
			}
			finally
			{
				if (!writeable)
					File.SetAttributes(path, attribs);
			}
		}
	}
}
