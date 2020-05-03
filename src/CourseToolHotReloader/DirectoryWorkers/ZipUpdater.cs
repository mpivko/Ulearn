using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CourseToolHotReloader.Dtos;
using Ionic.Zip;

namespace CourseToolHotReloader.DirectoryWorkers
{
	public class ZipUpdater
	{
		public static Guid CreateZipByUpdates(IList<ICourseUpdate> courseUpdates, IList<ICourseUpdate> deletedFiles)
		{
			var deletedFileContent = string.Join("\r\n", deletedFiles.Select(u => u.RelativePath));
			var guid = Guid.NewGuid();

			using (var zip = new ZipFile())
			{
				foreach (var update in courseUpdates)
				{
					if (Directory.Exists(update.FullPath))
					{
						zip.AddDirectory(update.FullPath, update.RelativePath);
					}

					if (File.Exists(update.FullPath))
					{
						zip.AddFile(update.FullPath, Path.GetDirectoryName(update.RelativePath));
					}
				}

				zip.AddEntry("deleted.txt", deletedFileContent);

				zip.Save($"{guid.ToString()}.zip");
			}

			return guid;
		}

		public static MemoryStream CreateZipByFolder(string pathToFolder)
		{

			/*using (var zip1 = ZipFile(zipFile.FullName, new ReadOptions { Encoding = Encoding.GetEncoding(866) }))
			{
			}
			*/

			using var zip = new ZipFile(Encoding.UTF8);
			zip.AddDirectory(pathToFolder);

			var ms = new MemoryStream();

			zip.Save(ms);

#if DEBUG
			using var zip1 = new ZipFile();
			zip1.AddDirectory(pathToFolder);
			zip1.Save("../temp.zip");
#endif
			return ms;
		}
	}
}