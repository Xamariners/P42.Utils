﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace P42.Utils
{
	static public class EmbeddedResourceCache
	{
		const string LocalStorageFolderName = "P42.Utils.EmbeddedResourceCache";

		// DO NOT CHANGE Environment.ApplicationDataPath to another path.  This is used to pass EmbeddedResource Fonts to UWP Text elements and there is zero flexibility here.
		public static string FolderPath(string folderName = null)
		{
			if (!Directory.Exists(P42.Utils.Environment.ApplicationDataPath))
				Directory.CreateDirectory(P42.Utils.Environment.ApplicationDataPath);
			folderName = folderName ?? LocalStorageFolderName;
			var folderPath = Path.Combine(P42.Utils.Environment.ApplicationDataPath, folderName);
			if (!Directory.Exists(folderPath))
				Directory.CreateDirectory(folderPath);
			return folderPath;
		}

		static object _locker = new object();
		static Dictionary<string, Task<bool>> _cacheTasks = new Dictionary<string, Task<bool>>();


		public static System.IO.Stream GetStream(string resourceId, Assembly assembly, string folderName = null)
		{
			var task = Task.Run(() => GetStreamAsync(resourceId, assembly, folderName));
			return task.Result;
		}

		public static async Task<System.IO.Stream> GetStreamAsync(string resourceId, Assembly assembly = null, string folderName = null)
		{
			assembly = assembly ?? Environment.EmbeddedResourceAssemblyResolver?.Invoke(resourceId);
			if (assembly == null)
				return null;
			var fileName = await LocalStorageSubPathForEmbeddedResourceAsync(resourceId, assembly, folderName);
			if (fileName == null)
				return null;
			var result = System.IO.File.Open(Path.Combine(FolderPath(folderName), fileName), FileMode.Open);
			return result;
		}


		public static string ApplicationUri(string resourceId, Assembly assembly = null, string folderName = null)
		{
			var localStorageFileName = LocalStorageSubPathForEmbeddedResource(resourceId, assembly, folderName);
			var uriString = "ms-appdata:///local/" + LocalStorageFolderName + "/" + localStorageFileName.Replace('\\', '/');
			return uriString;
		}

		public static string LocalStorageSubPathForEmbeddedResource(string resourceId, Assembly assembly = null, string folderName = null)
		{
			var task = Task.Run(() => LocalStorageSubPathForEmbeddedResourceAsync(resourceId, assembly, folderName));
			return task.Result;
		}


		public static bool Clear(string resourceId = null, string folderName = null)
		{
			if (string.IsNullOrWhiteSpace(resourceId))
			{
				// complete clear
				var folderPath = FolderPath(folderName);
				if (System.IO.Directory.Exists(folderPath))
				{
					var files = System.IO.Directory.EnumerateFiles(folderPath);
					foreach (var file in files)
						System.IO.File.Delete(file);
					System.IO.Directory.Delete(folderPath);
					return true;
				}
				return false;
			}
			var path = Path.Combine(FolderPath(folderName), resourceId);
			if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
			{
				System.IO.File.Delete(path);
				return true;
			}
			return false;
		}


		public static async Task<string> LocalStorageSubPathForEmbeddedResourceAsync(string resourceId, Assembly assembly = null, string folderName = null)
		{
			assembly = assembly ?? Environment.EmbeddedResourceAssemblyResolver?.Invoke(resourceId);
			var fileName = resourceId;

			if (string.IsNullOrWhiteSpace(fileName))
				return null;

			try
			{
				var path = Path.Combine(FolderPath(folderName), fileName);
				System.Diagnostics.Debug.WriteLine("PATH=[" + path + "]");
				if (System.IO.File.Exists(path) && !_cacheTasks.ContainsKey(path))
				{
					System.Diagnostics.Debug.WriteLine("EmbeddedResourceCache: [" + assembly.GetName().Name + ";" + resourceId + "] exists as [" + path + "]");
					return fileName;
				}
				var success = await CacheEmbeddedResource(resourceId, assembly, path);
				return success ? fileName : null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return null;
			}
		}

		static Task<bool> CacheEmbeddedResource(string resourceId, Assembly assembly, string fileName)
		{
			lock (_locker)
			{
				if (_cacheTasks.TryGetValue(fileName, out Task<bool> task))
					return task;
				_cacheTasks.Add(fileName, task = CacheTask(resourceId, assembly, fileName));
				return task;
			}
		}

#pragma warning disable CS1998
		static async Task<bool> CacheTask(string resourceId, Assembly assembly, string path)
#pragma warning restore CS1998
		{
			try
			{
				using (var stream = EmbeddedResource.GetStream(resourceId, assembly))
				{
					if (stream != null)
					{
						if (System.IO.File.Exists(path))
							System.Diagnostics.Debug.WriteLine("DownloadTask: FILE ALREADY EXISTS [" + path + "] [" + assembly.GetName().Name + ";" + resourceId + "]");

						using (var fileStream = new FileStream(path, FileMode.Create))
						{
							stream.Seek(0, SeekOrigin.Begin);
							stream.CopyTo(fileStream);
							fileStream.Flush(true);
							var length = fileStream.Length;
							System.Diagnostics.Debug.WriteLine("DownloadTask: file written [" + path + "] [" + assembly.GetName().Name + ";" + resourceId + "] length=[" + length + "] name=[" + fileStream.Name + "] pos=[" + fileStream.Position + "]");
						}
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
			if (File.Exists(path))
				System.IO.File.Delete(path);
			return false;
		}
	}
}
