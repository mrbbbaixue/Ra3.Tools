using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ra3.Tools.Interop.Windows;

internal static class UnelevatedProcessLauncher
{
    internal static void Launch(ProcessStartInfo startInfo)
    {
        var shellWindows = (IShellWindows)new CShellWindows();
        object location = CsidlDesktop;
        object unused = new();
        var serviceProvider = (IServiceProvider)shellWindows.FindWindowSW(ref location, ref unused, SwcDesktop, out _, SwfoNeedDispatch);

        var serviceGuid = SidTopLevelBrowser;
        var interfaceGuid = typeof(IShellBrowser).GUID;
        var shellBrowser = (IShellBrowser)serviceProvider.QueryService(ref serviceGuid, ref interfaceGuid);

        var dispatchGuid = typeof(IDispatch).GUID;
        var folderView = (IShellFolderViewDual)shellBrowser.QueryActiveShellView().GetItemObject(SvgioBackground, ref dispatchGuid);
        var shellDispatch = (IShellDispatch2)folderView.Application;
        shellDispatch.ShellExecute(startInfo.FileName,
                                   startInfo.Arguments,
                                   startInfo.WorkingDirectory,
                                   startInfo.Verb,
                                   SwShownormal);
    }

    private const int CsidlDesktop = 0;
    private const int SwcDesktop = 8;
    private const int SwfoNeedDispatch = 1;
    private const int SwShownormal = 1;
    private const uint SvgioBackground = 0;
    private static readonly Guid SidTopLevelBrowser = new("4C96BE40-915C-11CF-99D3-00AA004AE837");

    [ComImport]
    [Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39")]
    [ClassInterface(ClassInterfaceType.None)]
    private class CShellWindows
    {
    }

    [ComImport]
    [Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    private interface IShellWindows
    {
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object FindWindowSW([MarshalAs(UnmanagedType.Struct)] ref object location,
                            [MarshalAs(UnmanagedType.Struct)] ref object root,
                            int shellClass,
                            out int windowHandle,
                            int options);
    }

    [ComImport]
    [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IServiceProvider
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        object QueryService(ref Guid service, ref Guid interfaceId);
    }

    [ComImport]
    [Guid("000214E2-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellBrowser
    {
        void VTableGap01();
        void VTableGap02();
        void VTableGap03();
        void VTableGap04();
        void VTableGap05();
        void VTableGap06();
        void VTableGap07();
        void VTableGap08();
        void VTableGap09();
        void VTableGap10();
        void VTableGap11();
        void VTableGap12();
        IShellView QueryActiveShellView();
    }

    [ComImport]
    [Guid("000214E3-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellView
    {
        void VTableGap01();
        void VTableGap02();
        void VTableGap03();
        void VTableGap04();
        void VTableGap05();
        void VTableGap06();
        void VTableGap07();
        void VTableGap08();
        void VTableGap09();
        void VTableGap10();
        void VTableGap11();
        void VTableGap12();

        [return: MarshalAs(UnmanagedType.Interface)]
        object GetItemObject(uint aspectOfView, ref Guid interfaceId);
    }

    [ComImport]
    [Guid("00020400-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    private interface IDispatch
    {
    }

    [ComImport]
    [Guid("E7A1AF80-4D96-11CF-960C-0080C7F4EE85")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    private interface IShellFolderViewDual
    {
        object Application { [return: MarshalAs(UnmanagedType.IDispatch)] get; }
    }

    [ComImport]
    [Guid("A4C6892C-3BA9-11D2-9DEA-00C04FB16162")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    private interface IShellDispatch2
    {
        void ShellExecute([MarshalAs(UnmanagedType.BStr)] string file,
                          [MarshalAs(UnmanagedType.Struct)] object arguments,
                          [MarshalAs(UnmanagedType.Struct)] object directory,
                          [MarshalAs(UnmanagedType.Struct)] object operation,
                          [MarshalAs(UnmanagedType.Struct)] object show);
    }
}
