using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;
using Aura.Core.Services.TTS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for voice cloning, dialogue detection, and multi-voice synthesis.
/// </summary>
[ApiController]
[Route("api/voices")]
public class VoiceController : ControllerBase
{
    private readonly IVoiceCloningService? _cloningService;
    private readonly IDialogueDetectionService? _dialogueService;
    private readonly IVoiceAssignmentService? _assignmentService;
    private readonly IMultiVoiceSynthesizer? _synthesizer;
    private readonly ILogger<VoiceController> _logger;

    public VoiceController(
        ILogger<VoiceController> logger,
        IVoiceCloningService? cloningService = null,
        IDialogueDetectionService? dialogueService = null,
        IVoiceAssignmentService? assignmentService = null,
        IMultiVoiceSynthesizer? synthesizer = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cloningService = cloningService;
        _dialogueService = dialogueService;
        _assignmentService = assignmentService;
        _synthesizer = synthesizer;
    }

    /// <summary>
    /// Creates a cloned voice from uploaded audio samples.
    /// </summary>
    /// <param name="name">Name for the cloned voice.</param>
    /// <param name="samples">Audio sample files (WAV, MP3, or M4A).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created cloned voice.</returns>
    [HttpPost("clone")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ClonedVoice>> CloneVoice(
        [FromForm] [Required] string name,
        [FromForm] List<IFormFile> samples,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        if (_cloningService == null)
        {
            return StatusCode(503, new
            {
                success = false,
                error = "Voice cloning service is not available",
                correlationId
            });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                success = false,
                error = "Voice name is required",
                correlationId
            });
        }

        if (samples == null || samples.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                error = "At least one audio sample is required",
                correlationId
            });
        }

        _logger.LogInformation(
            "[{CorrelationId}] Creating cloned voice '{Name}' from {SampleCount} samples",
            correlationId,
            name,
            samples.Count);

        var tempDir = Path.Combine(Path.GetTempPath(), "VoiceClone", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var samplePaths = new List<string>();

        try
        {
            // Save uploaded files to temp directory
            foreach (var sample in samples)
            {
                var safeName = Path.GetFileName(sample.FileName);
                var path = Path.Combine(tempDir, safeName);
                using var stream = System.IO.File.Create(path);
                await sample.CopyToAsync(stream, ct).ConfigureAwait(false);
                samplePaths.Add(path);
            }

            var settings = new VoiceCloneSettings(Description: $"Cloned voice: {name}");
            var voice = await _cloningService.CreateClonedVoiceAsync(name, samplePaths, settings, ct)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "[{CorrelationId}] Successfully created cloned voice: {VoiceId}",
                correlationId,
                voice.Id);

            return Ok(new
            {
                success = true,
                voice,
                correlationId
            });
        }
        catch (Exception ex) when (ex is ArgumentException or FileNotFoundException)
        {
            _logger.LogWarning(ex, "[{CorrelationId}] Voice cloning validation failed", correlationId);
            return BadRequest(new
            {
                success = false,
                error = ex.Message,
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Voice cloning failed", correlationId);
            return StatusCode(500, new
            {
                success = false,
                error = "Failed to create cloned voice",
                details = ex.Message,
                correlationId
            });
        }
        finally
        {
            // Cleanup temp directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Gets all cloned voices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of cloned voices.</returns>
    [HttpGet("cloned")]
    public async Task<ActionResult<IReadOnlyList<ClonedVoice>>> GetClonedVoices(CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        if (_cloningService == null)
        {
            return Ok(new
            {
                success = true,
                voices = Array.Empty<ClonedVoice>(),
                correlationId
            });
        }

        try
        {
            var voices = await _cloningService.GetClonedVoicesAsync(ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                voices,
                count = voices.Count,
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to get cloned voices", correlationId);
            return StatusCode(500, new
            {
                success = false,
                error = "Failed to retrieve cloned voices",
                correlationId
            });
        }
    }

    /// <summary>
    /// Generates a preview sample using a cloned voice.
    /// </summary>
    /// <param name="voiceId">ID of the cloned voice.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Audio preview file.</returns>
    [HttpGet("{voiceId}/preview")]
    public async Task<IActionResult> GetVoicePreview(string voiceId, CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        if (_cloningService == null)
        {
            return StatusCode(503, new
            {
                success = false,
                error = "Voice cloning service is not available",
                correlationId
            });
        }

        try
        {
            var sampleText = "Hello! This is a preview of your cloned voice. How does it sound?";
            var result = await _cloningService.GenerateSampleAsync(voiceId, sampleText, ct)
                .ConfigureAwait(false);

            if (!System.IO.File.Exists(result.AudioPath))
            {
                return NotFound(new
                {
                    success = false,
                    error = "Preview audio file not found",
                    correlationId
                });
            }

            var contentType = result.AudioPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                ? "audio/wav"
                : "audio/mpeg";

            return PhysicalFile(result.AudioPath, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to generate voice preview", correlationId);
            return StatusCode(500, new
            {
                success = false,
                error = "Failed to generate preview",
                details = ex.Message,
                correlationId
            });
        }
    }

    /// <summary>
    /// Deletes a cloned voice.
    /// </summary>
    /// <param name="voiceId">ID of the voice to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("{voiceId}")]
    public async Task<IActionResult> DeleteClonedVoice(string voiceId, CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        if (_cloningService == null)
        {
            return StatusCode(503, new
            {
                success = false,
                error = "Voice cloning service is not available",
                correlationId
            });
        }

        try
        {
            await _cloningService.DeleteClonedVoiceAsync(voiceId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                message = "Voice deleted successfully",
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to delete voice", correlationId);
            return StatusCode(500, new
            {
                success = false,
                error = "Failed to delete voice",
                details = ex.Message,
                correlationId
            });
        }
    }

    /// <summary>
    /// Analyzes a script for dialogue and characters.
    /// </summary>
    /// <param name="request">Analysis request with script text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dialogue analysis result.</returns>
    [HttpPost("analyze-dialogue")]
    public async Task<ActionResult<DialogueAnalysis>> AnalyzeDialogue(
        [FromBody] AnalyzeDialogueRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        if (_dialogueService == null)
        {
            return StatusCode(503, new
            {
                success = false,
                error = "Dialogue detection service is not available",
                correlationId
            });
        }

        if (string.IsNullOrWhiteSpace(request?.Script))
        {
            return BadRequest(new
            {
                success = false,
                error = "Script text is required",
                correlationId
            });
        }

        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] Analyzing dialogue ({Length} characters)",
                correlationId,
                request.Script.Length);

            var analysis = await _dialogueService.AnalyzeScriptAsync(request.Script, ct)
                .ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                analysis,
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Dialogue analysis failed", correlationId);
            return StatusCode(500, new
            {
                success = false,
                error = "Failed to analyze dialogue",
                details = ex.Message,
                correlationId
            });
        }
    }

    /// <summary>
    /// Assigns voices to characters based on dialogue analysis.
    /// </summary>
    /// <param name="request">Assignment request with dialogue analysis and settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Voice assignment result.</returns>
    [HttpPost("assign-voices")]
    public async Task<ActionResult<VoiceAssignment>> AssignVoices(
        [FromBody] AssignVoicesRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        if (_assignmentService == null)
        {
            return StatusCode(503, new
            {
                success = false,
                error = "Voice assignment service is not available",
                correlationId
            });
        }

        if (request?.DialogueAnalysis == null)
        {
            return BadRequest(new
            {
                success = false,
                error = "Dialogue analysis is required",
                correlationId
            });
        }

        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] Assigning voices for {CharacterCount} characters",
                correlationId,
                request.DialogueAnalysis.Characters.Count);

            var settings = request.Settings ?? new VoiceAssignmentSettings();
            var assignment = await _assignmentService.AssignVoicesAsync(
                request.DialogueAnalysis,
                settings,
                ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                assignment,
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Voice assignment failed", correlationId);
            return StatusCode(500, new
            {
                success = false,
                error = "Failed to assign voices",
                details = ex.Message,
                correlationId
            });
        }
    }

    /// <summary>
    /// Synthesizes multi-voice audio from a voice assignment.
    /// </summary>
    /// <param name="request">Synthesis request with voice assignment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Path to synthesized audio file.</returns>
    [HttpPost("synthesize-multi")]
    public async Task<IActionResult> SynthesizeMultiVoice(
        [FromBody] SynthesizeMultiVoiceRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        if (_synthesizer == null)
        {
            return StatusCode(503, new
            {
                success = false,
                error = "Multi-voice synthesizer is not available",
                correlationId
            });
        }

        if (request?.Assignment == null)
        {
            return BadRequest(new
            {
                success = false,
                error = "Voice assignment is required",
                correlationId
            });
        }

        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] Synthesizing multi-voice audio for {LineCount} lines",
                correlationId,
                request.Assignment.VoicedLines.Count);

            var audioPath = await _synthesizer.SynthesizeMultiVoiceAsync(
                request.Assignment,
                null, // Progress not exposed via REST
                ct).ConfigureAwait(false);

            if (!System.IO.File.Exists(audioPath))
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = "Synthesized audio file not found",
                    correlationId
                });
            }

            var contentType = audioPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                ? "audio/wav"
                : "audio/mpeg";

            return PhysicalFile(audioPath, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Multi-voice synthesis failed", correlationId);
            return StatusCode(500, new
            {
                success = false,
                error = "Failed to synthesize multi-voice audio",
                details = ex.Message,
                correlationId
            });
        }
    }
}

/// <summary>
/// Request model for dialogue analysis.
/// </summary>
public record AnalyzeDialogueRequest
{
    /// <summary>
    /// The script text to analyze.
    /// </summary>
    [Required]
    public string Script { get; init; } = string.Empty;
}

/// <summary>
/// Request model for voice assignment.
/// </summary>
public record AssignVoicesRequest
{
    /// <summary>
    /// The dialogue analysis result.
    /// </summary>
    [Required]
    public DialogueAnalysis? DialogueAnalysis { get; init; }

    /// <summary>
    /// Optional settings for voice assignment.
    /// </summary>
    public VoiceAssignmentSettings? Settings { get; init; }
}

/// <summary>
/// Request model for multi-voice synthesis.
/// </summary>
public record SynthesizeMultiVoiceRequest
{
    /// <summary>
    /// The voice assignment with all lines and assigned voices.
    /// </summary>
    [Required]
    public VoiceAssignment? Assignment { get; init; }
}
