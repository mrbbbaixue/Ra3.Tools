using System.Management;

namespace Ra3.Tools.Graphics;

public sealed class VideoControllerService
{
    public IReadOnlyList<string> GetAllControllers()
    {
        var result = new List<string>();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (var controller in searcher.Get().Cast<ManagementObject>())
            {
                if (controller["Name"] is string name && !string.IsNullOrWhiteSpace(name))
                {
                    result.Add(name);
                }
            }
        }
        catch
        {
        }

        return result;
    }

    public string PredictPrimaryController(IReadOnlyCollection<string> controllers)
    {
        if (controllers.Count == 0)
        {
            return "<Follow System Settings>";
        }

        string candidate = "<Follow System Settings>";
        foreach (var controller in controllers)
        {
            if (IsVirtual(controller))
            {
                continue;
            }

            if (IsNvidia(controller))
            {
                return controller;
            }

            if (IsAmd(controller))
            {
                if (!IsNvidia(candidate))
                {
                    candidate = controller;
                }

                continue;
            }

            if (IsIntel(controller) && !IsAmd(candidate))
            {
                candidate = controller;
            }
        }

        return candidate;

        static bool IsNvidia(string value) => value.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                                              value.Contains("GeForce", StringComparison.OrdinalIgnoreCase);
        static bool IsAmd(string value) => value.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                                           value.Contains("Radeon", StringComparison.OrdinalIgnoreCase);
        static bool IsIntel(string value) => value.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
                                             value.Contains("Arc", StringComparison.OrdinalIgnoreCase);
        static bool IsVirtual(string value) => value.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                                               value.Contains("Remote", StringComparison.OrdinalIgnoreCase) ||
                                               value.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                                               value.Contains("Oray", StringComparison.OrdinalIgnoreCase);
    }
}
