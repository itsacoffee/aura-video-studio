using System.Runtime.InteropServices;

namespace Aura.Core.Dependencies;

internal static class FfmpegRuntimeHelper
{
    internal static string GetExecutableName() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";

    internal static string GetRuntimeRid()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        }

        return "linux-x64";
    }
}
