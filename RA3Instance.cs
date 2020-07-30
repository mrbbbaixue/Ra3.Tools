using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA3.Tools
{
    public class RA3Instance
    {
        public string GamePath;
        public string LaunchParamter;
        public bool UseBarLauncher;
        /// <summary>  
        /// 红警3进程实例
        /// </summary>  
        /// <param name="gamePath">游戏路径（可选，为空则从注册表读取）</param>  
        /// <param name="readPathFromRegistry">是否从注册表读取</param>  
        public RA3Instance(string gamePath , bool readPathFromRegistry)
        {
            //Read GamePath
            if ( string.IsNullOrWhiteSpace(gamePath) || readPathFromRegistry)
            {
                using (var view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                using (var ra3 = view32.OpenSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3"))
                {
                    GamePath = (string)ra3.GetValue("Install Dir");
                }
            }
            else
            {

                GamePath = gamePath;
            }
            //Check BarLauncher
            if (File.Exists("RA3BarLauncher.exe"))
            {
                UseBarLauncher = true;
            }
        }
        public bool IsRA3PathValid()
        {
            try
            {
                return Directory.EnumerateFiles(GamePath, "RA3_*_1.12.SkuDef").Any();
            }
            catch (Exception) { }
            return false;
        }

        public bool IsRA3FileValid()
        {
            return false;
        }

        public void Register()
        {
            try
            {
                if (File.Exists("RA3.reg"))
                {
                    string regPath = Path.GetFullPath("RA3.reg"); 
                    regPath = @"""" + regPath + @"""";
                    Process.Start("regedit", string.Format(" /s {0}", regPath));
                }
                //write registion here.
            }
            catch (Exception) { }
        }

        public void OpenRA3ModFolder()
        {
            
        }
        public void OpenRA3MapFolder()
        {

        }
        public void OpenRA3ReplayFolder()
        {

        }


        //From @BSG-75 (https://github.com/BSG-75)
        public bool DoesRA3NeedSteamAppID()
        {
            var ra3Path = GamePath;
            if (ra3Path.IndexOf("steam", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return false;
            }

            var steamAppIDPath = Path.Combine(ra3Path, "steam_appid.txt");
            return !File.Exists(steamAppIDPath) || File.ReadAllText(steamAppIDPath).Trim() != "17480";
        }

        public bool DoesRA3NeedPatchedParFile()
        {
            var tucParPath = Path.Combine(GamePath , "Data", "ra3_1.12.par");
            if (!File.Exists(tucParPath))
            {
                return false;
            }

            return !File.ReadAllBytes(tucParPath).SequenceEqual(_patchedParFile);
        }

        public void GenerateSteamAppID()
        {
            var steamAppIDPath = Path.Combine(GamePath , "steam_appid.txt");
            File.WriteAllText(steamAppIDPath, "17480");
        }

        public void GeneratePatchedParFile()
        {
            var tucParPath = Path.Combine(GamePath , "Data", "ra3_1.12.par");
            var oldFileId = 0;
            while (File.Exists($"{tucParPath}.{oldFileId}.old"))
            {
                ++oldFileId;
            }
            File.Move(tucParPath, $"{tucParPath}.{oldFileId}.old");
            File.WriteAllBytes(tucParPath, _patchedParFile);
        }

        private static readonly byte[] _patchedParFile = new byte[]
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
        //
        

        //ToDo:1.完善检测文件完整的函数
        //ToDo:3.添加打开地图/Mod文件夹的函数
        //ToDo:5.启动红警3并且注入dll的函数？
        //ToDo:6.启动红警3的函数
        //ToDo:7.读取Ratotal全局配置，并且保存RA3Settings
        //ToDo:8.软链接修改Mod,Map,Replay的位置
        //ToDo:9.读取本机用的所有马甲名称，尝试读取当前正在使用的？
    }
}
