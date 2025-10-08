using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Providers.Llm;
using Aura.Providers.Tts;
using Aura.Providers.Video;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/aura-api-.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register core services
builder.Services.AddSingleton<HardwareDetector>();
builder.Services.AddSingleton<ILlmProvider, RuleBasedLlmProvider>();
builder.Services.AddSingleton<ITtsProvider, WindowsTtsProvider>();
builder.Services.AddSingleton<IVideoComposer, FfmpegVideoComposer>();
builder.Services.AddSingleton<VideoOrchestrator>();

// Configure Kestrel to listen on specific port
builder.WebHost.UseUrls("http://127.0.0.1:5005");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Health check endpoint
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

// Capabilities endpoint
app.MapGet("/capabilities", async (HardwareDetector detector) =>
{
    try
    {
        var profile = await detector.DetectSystemAsync();
        return Results.Ok(new
        {
            tier = profile.Tier.ToString(),
            cpu = new { cores = profile.PhysicalCores, threads = profile.LogicalCores },
            ram = new { gb = profile.RamGB },
            gpu = profile.Gpu != null ? new { model = profile.Gpu.Model, vramGB = profile.Gpu.VramGB, vendor = profile.Gpu.Vendor } : null,
            enableNVENC = profile.EnableNVENC,
            enableSD = profile.EnableSD,
            offlineOnly = profile.OfflineOnly
        });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error detecting capabilities");
        return Results.Problem("Error detecting system capabilities", statusCode: 500);
    }
})
.WithName("GetCapabilities")
.WithOpenApi();

// Plan endpoint - create or update timeline plan
app.MapPost("/plan", ([FromBody] PlanRequest request) =>
{
    try
    {
        var plan = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(request.TargetDurationMinutes),
            Pacing: request.Pacing,
            Density: request.Density,
            Style: request.Style
        );
        
        return Results.Ok(new { success = true, plan });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error creating plan");
        return Results.Problem("Error creating plan", statusCode: 500);
    }
})
.WithName("CreatePlan")
.WithOpenApi();

// Script generation endpoint
app.MapPost("/script", async ([FromBody] ScriptRequest request, ILlmProvider llmProvider, CancellationToken ct) =>
{
    try
    {
        var brief = new Brief(
            Topic: request.Topic,
            Audience: request.Audience,
            Goal: request.Goal,
            Tone: request.Tone,
            Language: request.Language,
            Aspect: request.Aspect
        );
        
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(request.TargetDurationMinutes),
            Pacing: request.Pacing,
            Density: request.Density,
            Style: request.Style
        );
        
        var script = await llmProvider.DraftScriptAsync(brief, planSpec, ct);
        
        return Results.Ok(new { success = true, script });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error generating script");
        return Results.Problem("Error generating script", statusCode: 500);
    }
})
.WithName("GenerateScript")
.WithOpenApi();

// TTS endpoint
app.MapPost("/tts", async ([FromBody] TtsRequest request, ITtsProvider ttsProvider, CancellationToken ct) =>
{
    try
    {
        var lines = request.Lines.Select(l => new ScriptLine(
            SceneIndex: l.SceneIndex,
            Text: l.Text,
            Start: TimeSpan.FromSeconds(l.StartSeconds),
            Duration: TimeSpan.FromSeconds(l.DurationSeconds)
        )).ToList();
        
        var voiceSpec = new VoiceSpec(
            VoiceName: request.VoiceName,
            Rate: request.Rate,
            Pitch: request.Pitch,
            Pause: request.PauseStyle
        );
        
        var result = await ttsProvider.SynthesizeAsync(lines, voiceSpec, ct);
        
        return Results.Ok(new { success = true, audioPath = result });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error synthesizing audio");
        return Results.Problem("Error synthesizing audio", statusCode: 500);
    }
})
.WithName("SynthesizeAudio")
.WithOpenApi();

// Downloads manifest endpoint
app.MapGet("/downloads/manifest", () =>
{
    try
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "manifest.json");
        if (File.Exists(manifestPath))
        {
            var json = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<JsonDocument>(json);
            return Results.Ok(manifest);
        }
        return Results.NotFound(new { error = "Manifest not found" });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error reading manifest");
        return Results.Problem("Error reading manifest", statusCode: 500);
    }
})
.WithName("GetManifest")
.WithOpenApi();

// Settings endpoints
app.MapPost("/settings/save", ([FromBody] Dictionary<string, object> settings) =>
{
    try
    {
        var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        return Results.Ok(new { success = true });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error saving settings");
        return Results.Problem("Error saving settings", statusCode: 500);
    }
})
.WithName("SaveSettings")
.WithOpenApi();

app.MapGet("/settings/load", () =>
{
    try
    {
        var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura", "settings.json");
        if (File.Exists(settingsPath))
        {
            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return Results.Ok(settings);
        }
        return Results.Ok(new Dictionary<string, object>());
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error loading settings");
        return Results.Problem("Error loading settings", statusCode: 500);
    }
})
.WithName("LoadSettings")
.WithOpenApi();

app.Run();

// DTOs
record PlanRequest(double TargetDurationMinutes, Pacing Pacing, Density Density, string Style);
record ScriptRequest(string Topic, string Audience, string Goal, string Tone, string Language, Aspect Aspect, double TargetDurationMinutes, Pacing Pacing, Density Density, string Style);
record TtsRequest(List<LineDto> Lines, string VoiceName, double Rate, double Pitch, PauseStyle PauseStyle);
record LineDto(int SceneIndex, string Text, double StartSeconds, double DurationSeconds);
