using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace RA3.Tools
{
    public static class Utility
    {
        // From https://github.com/RA3CoronaDevelopers/CoronaLauncher/
        private static long GetDirectoryLength(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return 0;
            long len = 0;

            DirectoryInfo di = new DirectoryInfo(directoryPath);

            foreach (FileInfo fi in di.GetFiles())
            {
                len += fi.Length;
            }
            DirectoryInfo[] dis = di.GetDirectories();
            if (dis.Length > 0)
            {
                for (int i = 0; i < dis.Length; i++)
                {
                    len += GetDirectoryLength(dis[i].FullName);
                }
            }
            return len;
        }
        internal static string GetDirectorySize(string directoryPath)
        {
            var len = GetDirectoryLength(directoryPath);
            if (len > 0)
            {
                double sizeKB = len / 1024;
                if (sizeKB < 1024)
                {
                    return $"{Math.Round(sizeKB, 2)}KB";
                }
                else
                {
                    double sizeMB = sizeKB / 1024;
                    if (sizeMB < 1024)
                    {
                        return $"{Math.Round(sizeMB, 2)}MB";
                    }
                    else
                    {
                        double sizeGB = sizeMB / 1024;
                        return $"{Math.Round(sizeGB, 2)}GB";
                    }
                }
            }
            return "0B";
        }

        public static readonly byte[] PatchedParFile = new byte[]
        {
            0x13, 0x60, 0xC4, 0x41, 0x2A, 0x02, 0x11, 0x3C, 0x56, 0x32, 0x29, 0x76, 0x05, 0x20, 0x35, 0x53,
            0x4A, 0x07, 0x23, 0x34, 0x13, 0x4C, 0x57, 0x54, 0x01, 0x41, 0x71, 0x49, 0x77, 0x05, 0x65, 0x73,
            0x47, 0x05, 0x2A, 0x34, 0x55, 0x50, 0x27, 0x03, 0x24, 0x5F, 0x14, 0x57, 0x58, 0x11, 0x53, 0x03,
            0x1F, 0x22, 0x5E, 0x0E, 0x4D, 0x51, 0x4A, 0x2F, 0x2F, 0x52, 0x04, 0x30, 0x05, 0x3E, 0x42, 0x04,
            0x12, 0x17, 0x11, 0x23, 0x25, 0x14, 0x6F, 0x72, 0x03, 0x46, 0x47, 0x1E, 0x6E, 0x72, 0x14, 0x2E,
            0x3A, 0x04, 0x23, 0x47, 0x10, 0x1B, 0x09, 0x54, 0x15, 0x04, 0x19, 0x3C, 0x47, 0x1D, 0x4C, 0x15,
            0x57, 0x6E, 0x00, 0x55, 0x47, 0x16, 0x19, 0x23, 0x77, 0x18, 0x10, 0x0C, 0x45, 0x10, 0x2C, 0x26,
            0x7C, 0x39, 0x3C, 0x56, 0x45, 0x1A, 0x21, 0x33, 0x42, 0x41, 0x17, 0x2E, 0x39, 0x40, 0x05, 0x05,
            0x0A, 0x42, 0x51, 0x7D, 0x50, 0x0E, 0x50, 0x0C, 0x46, 0x46, 0x19, 0x0A, 0x28, 0x51, 0x4D, 0x07,
            0x0B, 0x3C, 0x65, 0x42, 0x7D
        };
    }
    public class ResourceFolder
    {
        public readonly string Path;
        public ResourceFolder(string path)
        {
            Path = path;
            if (!Directory.Exists(Path))
            {
                try
                {
                    Directory.CreateDirectory(Path);
                }
                catch
                {
                    //Error handling
                }
            }
        }
        public void OpenInExplorer()
        {
            var explorerProcessInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"{Path}"
            };
            Process.Start(explorerProcessInfo);
        }
        public string GetSize()
        {
            try { return Utility.GetDirectorySize(Path); }
            catch (Exception) { return "ERROR"; }
        }

        //添加mklink函数？

    }

    public class Skudef
    {
        public string Path;
        const string UniversalHeader = "mod-game 1.12";
        public string BigFileSearchPath;
        private string _commandSearchBigFile => string.IsNullOrEmpty(BigFileSearchPath) ? $"set-search-path big:;\"{BigFileSearchPath}\";" : "set-search-path big:;.;";
        private List<string> _addedBigs;

        public Skudef(string skudefPath)
        {
            Path = skudefPath;
            if (File.Exists(Path))
            {
                _addedBigs = TryRead();
            }
            else
            {
                _addedBigs = new List<string> { };
            }
        }

        public void TryWrite()
        {
            var output = new List<string> { };
            output.Add(UniversalHeader);
            if (!string.IsNullOrEmpty(BigFileSearchPath))
            {
                output.Add(_commandSearchBigFile);
            }
            foreach (var bigFile in _addedBigs)
            {
                output.Add($"add-big {bigFile}");
            }
            File.WriteAllLines(Path,output);
        }

        public void RemoveBig(string bigPath)
        {
            _addedBigs.Remove(bigPath);
        }
        public void AddBig(string bigPath)
        {
            _addedBigs.Add(bigPath);
        }

        // Private methods.
        private List<string> TryRead()
        {
            string[] skudefCommands = File.ReadAllLines(Path);
            var list = new List<string> { };
            foreach (string skudefCommand in skudefCommands)
            {
                if (skudefCommand.Contains("add-big "))
                {

                    list.Add(skudefCommand.Replace("add-big ", "").Replace("\"",""));
                }
            }

            return list;
        }

    }

}

