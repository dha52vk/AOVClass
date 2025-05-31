using AovClass;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aov_Mod_GUI.Models
{
    public class ProjectPackage
    {
        public Dictionary<string, ActionsXml> Projects = new Dictionary<string, ActionsXml>(0, StringComparer.CurrentCultureIgnoreCase);
        private readonly DirectoryInfo tempDir;
        public string PackageTitle = "";

        public ProjectPackage(string pkgPath)
        {
            PackageTitle = Path.GetFileName(pkgPath);
            tempDir = Directory.CreateTempSubdirectory();
            if (!File.Exists(pkgPath))
                return;
            ZipFile.ExtractToDirectory(pkgPath, tempDir.FullName);
            ScanXml("");
        }

        public void ScanXml(string subPath)
        {
            foreach (string filePath in Directory.GetFiles(Path.Combine(tempDir.FullName, subPath)))
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                ActionsXml action = new();
                try
                {
                    action.LoadFromText(Encoding.UTF8.GetString(AovTranslation.Decompress(bytes) ?? bytes));
                    Projects[Path.Combine(subPath, Path.GetFileName(filePath))] = action;
                }
                catch
                {

                }
            }
            foreach (string folderPath in Directory.GetDirectories(Path.Combine(tempDir.FullName, subPath)))
            {
                ScanXml(Path.Combine(subPath, Path.GetFileName(folderPath)));
            }
        }

        public void SaveTo(string pkgPath)
        {
            SaveXmlChange();
            ZipDirectories(Directory.GetDirectories(tempDir.FullName), pkgPath);
        }

        private void SaveXmlChange()
        {
            foreach (var pair in Projects)
            {
                File.WriteAllBytes(Path.Combine(tempDir.FullName, pair.Key), AovTranslation.Compress(
                    Encoding.UTF8.GetBytes(pair.Value.GetOuterXml())));
            }
        }

        static void ZipDirectories(string[] directories, string zipFilePath)
        {
            using ICSharpCode.SharpZipLib.Zip.ZipOutputStream zipOutputStream = new(File.Create(zipFilePath));
            zipOutputStream.SetLevel(0); // Set compression level

            foreach (string folderPath in directories)
            {
                AddFolderToZip(folderPath, zipOutputStream, Path.GetFileName(folderPath));
            }
        }

        static void AddFolderToZip(string folderPath, ICSharpCode.SharpZipLib.Zip.ZipOutputStream zipOutputStream, string entryName)
        {
            foreach (string file in Directory.GetFiles(folderPath))
            {
                ICSharpCode.SharpZipLib.Zip.ZipEntry entry = new(Path.Combine(entryName, Path.GetFileName(file)));
                zipOutputStream.PutNextEntry(entry);

                using FileStream fs = File.OpenRead(file);
                byte[] buffer = new byte[4096];
                int sourceBytes;
                do
                {
                    sourceBytes = fs.Read(buffer, 0, buffer.Length);
                    zipOutputStream.Write(buffer, 0, sourceBytes);
                } while (sourceBytes > 0);
            }

            foreach (string dir in Directory.GetDirectories(folderPath))
            {
                string dirName = Path.GetFileName(dir);
                AddFolderToZip(dir, zipOutputStream, Path.Combine(entryName, dirName));
            }
        }
    }
}
