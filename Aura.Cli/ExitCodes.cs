namespace Aura.Cli;

/// <summary>
/// Standard exit codes for CLI commands
/// </summary>
public static class ExitCodes
{
    /// <summary>
    /// Success - operation completed successfully
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// E200 - Preflight check failed (missing dependencies, insufficient hardware, etc.)
    /// </summary>
    public const int PreflightFail = 200;

    /// <summary>
    /// E310 - Script generation failed
    /// </summary>
    public const int ScriptFail = 310;

    /// <summary>
    /// E320 - Visual assets acquisition failed
    /// </summary>
    public const int VisualsFail = 320;

    /// <summary>
    /// E330 - TTS (text-to-speech) synthesis failed
    /// </summary>
    public const int TtsFail = 330;

    /// <summary>
    /// E340 - Video rendering failed
    /// </summary>
    public const int RenderFail = 340;

    /// <summary>
    /// E100 - Invalid arguments or configuration
    /// </summary>
    public const int InvalidArguments = 100;

    /// <summary>
    /// E500 - Unexpected error
    /// </summary>
    public const int UnexpectedError = 500;

    /// <summary>
    /// E101 - Invalid configuration
    /// </summary>
    public const int InvalidConfiguration = 101;

    /// <summary>
    /// E102 - Not implemented yet
    /// </summary>
    public const int NotImplemented = 102;

    /// <summary>
    /// E501 - Runtime error
    /// </summary>
    public const int RuntimeError = 501;

    /// <summary>
    /// E502 - Unhandled exception
    /// </summary>
    public const int UnhandledException = 502;
}
