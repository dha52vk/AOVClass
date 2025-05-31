using AovClass;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aov_Mod_GUI.Models
{
    public class InfosPackage
    {
        public Dictionary<string, PackageElement> Elements = [];
        private readonly DirectoryInfo? tempDir;
        private bool isPacked = true;
        public string PackageTitle = "";

        public InfosPackage(string pkgPath)
        {
            PackageTitle = Path.GetFileName(pkgPath);
            tempDir = Directory.CreateTempSubdirectory();
            if (!File.Exists(pkgPath))
                return;
            ZipFile.ExtractToDirectory(pkgPath, tempDir.FullName);
            ScanPackage("");
        }

        public InfosPackage(string key, PackageElement element)
        {
            isPacked = false;
            Elements[key] = element;
            tempDir = null;
        }

        public void ScanPackage(string subPath)
        {
            if (!isPacked || tempDir == null)
            {
                return;
            }
            foreach (string filePath in Directory.GetFiles(Path.Combine(tempDir.FullName, subPath)))
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                try
                {
                    Elements[Path.Combine(subPath, Path.GetFileName(filePath))] = 
                        PackageSerializer.Deserialize(AovTranslation.Decompress(bytes) ?? bytes);
                }
                catch
                {

                }
            }
            foreach (string folderPath in Directory.GetDirectories(Path.Combine(tempDir.FullName, subPath)))
            {
                ScanPackage(Path.Combine(subPath, Path.GetFileName(folderPath)));
            }
        }

        public void SaveTo(string pkgPath)
        {
            if (!isPacked || tempDir == null)
            {
                File.WriteAllBytes(pkgPath,
                    AovTranslation.Compress(PackageSerializer.Serialize(Elements.First().Value)));
                return;
            }
            SaveInfosChange("");
            ZipDirectories(Directory.GetDirectories(tempDir.FullName), pkgPath);
        }

        private void SaveInfosChange(string subPath)
        {
            if (!isPacked || tempDir == null)
            {
                return;
            }
            foreach (var pair in Elements)
            {
                File.WriteAllBytes(Path.Combine(tempDir.FullName, pair.Key), 
                    AovTranslation.Compress(PackageSerializer.Serialize(pair.Value)) );
            }
            foreach (string folderPath in Directory.GetDirectories(Path.Combine(tempDir.FullName, subPath)))
            {
                SaveInfosChange(folderPath);
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
