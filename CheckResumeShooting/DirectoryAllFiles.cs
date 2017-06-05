using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheckResumeShooting
{
	public class DirectoryAllFiles
	{
		List<FileInformation> FileList = new List<FileInformation>();

		public List<FileInformation> GetAllFiles(DirectoryInfo dir)
		{
			var allFile = dir.GetFiles().ToArray();

			if (allFile.Length > 0)
			{
				foreach (var fi in allFile)
				{
					this.FileList.Add(new FileInformation { FileName = fi.Name, FilePath = fi.FullName });
				}

				//return this.FileList;
			}

			var allDir = dir.GetDirectories();

			foreach (var d in allDir)
			{
                GetAllFiles(d);

                //if (this.FileList.Count > 0) return this.FileList;
            }

			return this.FileList;
		}
	}

	public class FileInformation
	{
		public string FileName { get; set; }
		public string FilePath { get; set; }
	}
}