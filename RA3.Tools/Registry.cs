using Microsoft.Win32;
using System.IO;
using System.Linq;

namespace RA3.Tools
{
    public static class Registry
    {
        public enum RegistryStatus
        {
            Correct,
            NotExist,
            MissingPath,
            MissingMapSync,
            MissingLanguage,
        }

        public static RegistryStatus Status
        {
            get { return IsRegistryValid(); }
        }

        private static RegistryStatus IsRegistryValid()
        {
            using var view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var ra3 = view32.OpenSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
            if (ra3 == null)
            {
                return RegistryStatus.NotExist;
            }
            using var newra3 = view32.CreateSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
            if (newra3.GetValue("Install Dir") == null)
            {
                return RegistryStatus.MissingPath;
            }
            if (newra3.GetValue("UseLocalUserMap") == null || (int)newra3.GetValue("UseLocalUserMap") != 0)
            {
                return RegistryStatus.MissingMapSync;
            }

            using var viewUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            using var languageRa3 = view32.OpenSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
            if (languageRa3 == null || languageRa3.GetValue("Language") == null)
            {
                return RegistryStatus.MissingLanguage;
            }
            return RegistryStatus.Correct;
        }

        public static string GetRA3Path()
        {
            using var view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var ra3 = view32.OpenSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3");
            return ra3?.GetValue("Install Dir") as string ?? string.Empty;
        }

        public static void SetRA3Path(string path)
        {
            using var view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var ra3 = view32.OpenSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
            if (ra3 == null)
            {
                using var newra3 = view32.CreateSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
                newra3.SetValue("Install Dir", path, RegistryValueKind.String);
                return;
            }
            ra3.SetValue("Install Dir", path, RegistryValueKind.String);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gamePath">Game install folder.</param>
        /// <param name="value">Language string.</param>
        /// <returns>true if succeed. false if failed.</returns>
        public static bool SetLanguage(string gamePath, string value)
        {
            try
            {
                var isPathValid = Directory.EnumerateFiles(gamePath, "*.skudef").Any();
                if (isPathValid)
                {                    
                    string GetCsfPath(string language)
                        => Path.Combine(gamePath, "Launcher", $"{language}.csf");

                    string GetSkudefPath(string language, string version)
                        => Path.Combine(gamePath, $"RA3_{language}_{version}.skudef");

                    void SetValueToRegistry(string setValue)
                    {
                        using var view32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
                        using var ra3 = view32.OpenSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
                        if (ra3 == null)
                        {
                            using var newRa3 = view32.CreateSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
                            newRa3.SetValue("Language", setValue, RegistryValueKind.String);
                        }
                        ra3.SetValue("Language", setValue, RegistryValueKind.String);
                    }

                    if ( File.Exists(GetSkudefPath(value, "1.12")) && File.Exists(GetCsfPath(value)) )
                    {
                        SetValueToRegistry(value);
                        return true;
                    }
                    else if ( File.Exists(GetSkudefPath("english", "1.12")) && File.Exists(GetCsfPath("english")) )
                    {
                        SetValueToRegistry("english");
                        return true;
                    }
                }                
            }
            catch { }            
            return false;
        }

        public static void EnableMapSync()
        {
            using var view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var ra3 = view32.OpenSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
            if (ra3 == null)
            {
                using var newra3 = view32.CreateSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
                newra3.SetValue("UseLocalUserMap", 0, RegistryValueKind.DWord);
                return;
            }
            ra3.SetValue("UseLocalUserMap", 0, RegistryValueKind.DWord);
        }

        public static void ResetRegistry(string path)
        {
            using var view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var ra3 = view32.OpenSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
            if (ra3 == null)
            {
                using var newra3 = view32.CreateSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
                newra3.SetValue("Install Dir", path, RegistryValueKind.String);
                newra3.SetValue("UseLocalUserMap", 0, RegistryValueKind.DWord);
                return;
            }
            ra3.SetValue("Install Dir", path);
            ra3.SetValue("UseLocalUserMap", 0, RegistryValueKind.DWord);
        }
    }
}
