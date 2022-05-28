using Microsoft.Win32;
using System.IO;

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
        /// <returns>true if succeed. false if failed (language not find in game folder).</returns>
        public static bool SetLanguage(string gamePath, string value)
        {
            try
            {
                // verify if v1.12 skudef file of this value exists.
                var isTargetSkudefExists = File.Exists(Path.Combine(gamePath, $"RA3_{value}_1.12.skudef"));
                var isTargetCsfExists = File.Exists(Path.Combine(gamePath, "Launcher", $"{value}.csf"));
                if (isTargetSkudefExists && isTargetCsfExists)
                {
                    using var view32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
                    using var ra3 = view32.OpenSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
                    if (ra3 == null)
                    {
                        using var newRa3 = view32.CreateSubKey("Software\\Electronic Arts\\Electronic Arts\\Red Alert 3", writable: true);
                        newRa3.SetValue("Language", value, RegistryValueKind.String);
                        return true;
                    }
                    ra3.SetValue("Language", value, RegistryValueKind.String);
                    return true;
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
