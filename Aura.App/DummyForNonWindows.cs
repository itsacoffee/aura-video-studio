// This file exists only to allow Aura.App to build on non-Windows platforms
// On non-Windows, Aura.App builds as a minimal library with no meaningful content
// The actual WinUI3 application only builds on Windows

namespace Aura.App;

/// <summary>
/// Dummy class to satisfy compiler on non-Windows platforms.
/// The actual Aura.App (WinUI3 application) only builds on Windows.
/// </summary>
internal static class DummyForNonWindows
{
    internal static string Platform => "This assembly is a stub for non-Windows platforms";
}
