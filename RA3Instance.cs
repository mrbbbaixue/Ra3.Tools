using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace RA3.Tools
{
    public class RA3Instance
        {
            public string GamePath;
            public string LaunchParamter;
            public bool UseBarLauncher;
            //
            public ResourceFolder ModFolder = new ResourceFolder(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Red Alert 3\\Mods\\");
            public ResourceFolder ReplayFolder = new ResourceFolder(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Red Alert 3\\Replays\\");
            public ResourceFolder MapFolder = new ResourceFolder(Environment.GetEnvironmentVariable("appdata") + "\\Red Alert 3\\Maps\\");
            public ResourceFolder ProfileFolder = new ResourceFolder(Environment.GetEnvironmentVariable("appdata") + "\\Red Alert 3\\Profiles\\");
            /// <summary>  
            /// 红警3进程实例
            /// </summary>  
            /// <param name="gamePath">游戏路径（可选，为空则从注册表读取）</param>  
            public RA3Instance(string gamePath = "")
            {
                //Read GamePath
                if (string.IsNullOrWhiteSpace(gamePath))
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

            public void Launch()
            {
                var LauncherPath = GamePath + "RA3.exe";
                if ((UseBarLauncher == true) && File.Exists("RA3BarLauncher.exe"))
                {
                    LauncherPath = "RA3BarLauncher.exe";
                }
                //
                var ra3ProcessInfo = new ProcessStartInfo
                {
                    FileName = LauncherPath,
                    Arguments = LaunchParamter,
                    WorkingDirectory = GamePath
                };
                Process.Start(ra3ProcessInfo);
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
                var tucParPath = Path.Combine(GamePath, "Data", "ra3_1.12.par");
                if (!File.Exists(tucParPath))
                {
                    return false;
                }

                return !File.ReadAllBytes(tucParPath).SequenceEqual(Utility.PatchedParFile);
            }

            public void GenerateSteamAppID()
            {
                var steamAppIDPath = Path.Combine(GamePath, "steam_appid.txt");
                File.WriteAllText(steamAppIDPath, "17480");
            }

            public void GeneratePatchedParFile()
            {
                var tucParPath = Path.Combine(GamePath, "Data", "ra3_1.12.par");
                var oldFileId = 0;
                while (File.Exists($"{tucParPath}.{oldFileId}.old"))
                {
                    ++oldFileId;
                }
                File.Move(tucParPath, $"{tucParPath}.{oldFileId}.old");
                File.WriteAllBytes(tucParPath, Utility.PatchedParFile);
            }
            //ToDo:1.完善检测文件完整的函数
            //ToDo:5.启动红警3并且注入dll的函数？
            //ToDo:7.读取Ratotal全局配置，并且保存RA3Settings（应当来自RA3.RatotalWebApi工具类）
            //ToDo:8.软链接修改Mod,Map,Replay的位置（在ResourceFolder类中）
            //ToDo:11.检查各个文件夹的大小（在ResourceFolder类中）
            //ToDo:9.读取本机用的所有马甲名称，尝试读取当前正在使用的？
            //ToDo:10.读取并且修改红警3的所有设置（对于用户/global）
            //ToDo:11.删除Skirmish.ini（尝试读取当前正在使用的马甲）
        }
    }
