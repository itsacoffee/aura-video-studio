# Provider Selection Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        USER INTERFACE (Web)                          │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  CreateWizard.tsx                                             │  │
│  │  ┌────────────────────────────────────────────────────────┐  │  │
│  │  │  Step 3: Review & Settings                              │  │  │
│  │  │                                                          │  │  │
│  │  │  [Profile Dropdown: Free/Balanced/Pro-Max]              │  │  │
│  │  │                                                          │  │  │
│  │  │  ┌──────────────────────────────────────────────────┐  │  │  │
│  │  │  │  ProviderSelection Component                      │  │  │  │
│  │  │  │                                                    │  │  │  │
│  │  │  │  Script LLM: [Auto ▼]  Options:                  │  │  │  │
│  │  │  │              • RuleBased (Free, Always)          │  │  │  │
│  │  │  │              • Ollama (Free, Local)              │  │  │  │
│  │  │  │              • OpenAI (Pro, Cloud)               │  │  │  │
│  │  │  │              • AzureOpenAI (Pro, Cloud)          │  │  │  │
│  │  │  │              • Gemini (Pro, Cloud)               │  │  │  │
│  │  │  │                                                    │  │  │  │
│  │  │  │  TTS:        [Auto ▼]  Options:                  │  │  │  │
│  │  │  │              • Windows SAPI (Free)                │  │  │  │
│  │  │  │              • ElevenLabs (Pro, Cloud)           │  │  │  │
│  │  │  │              • PlayHT (Pro, Cloud)               │  │  │  │
│  │  │  │                                                    │  │  │  │
│  │  │  │  Visuals:    [Auto ▼]  Options:                  │  │  │  │
│  │  │  │              • Stock (Free)                       │  │  │  │
│  │  │  │              • LocalSD (NVIDIA, 6GB+)            │  │  │  │
│  │  │  │              • CloudPro (Stability/Runway)       │  │  │  │
│  │  │  │                                                    │  │  │  │
│  │  │  │  Upload:     [Auto ▼]  Options:                  │  │  │  │
│  │  │  │              • Off                                │  │  │  │
│  │  │  │              • YouTube                            │  │  │  │
│  │  │  └──────────────────────────────────────────────────┘  │  │  │
│  │  │                                                          │  │  │
│  │  │  [Run Preflight Check]                                  │  │  │
│  │  └────────────────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ POST /api/script
                                    │ { providerSelection: { script, tts, visuals, upload } }
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           API LAYER                                  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Program.cs: /api/script endpoint                            │  │
│  │                                                               │  │
│  │  1. Parse ScriptRequest (includes ProviderSelectionDto)      │  │
│  │  2. Extract per-stage selection (script/tts/visuals/upload)  │  │
│  │  3. Pass to ScriptOrchestrator                               │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         CORE ORCHESTRATION                           │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  ScriptOrchestrator                                           │  │
│  │                                                               │  │
│  │  preferredTier = request.ProviderSelection?.Script ?? "Free" │  │
│  │       │                                                       │  │
│  │       └──────────────────────────────────────────────────────┼──┤
│  │                                                               │  │
│  │  ┌────────────────────────────────────────────────────────┐  │  │
│  │  │  ProviderMixer.SelectLlmProvider()                     │  │  │
│  │  │                                                         │  │  │
│  │  │  if (preferredTier is specific provider name):         │  │  │
│  │  │      normalizedName = NormalizeProviderName(...)       │  │  │
│  │  │      if availableProviders[normalizedName] exists:     │  │  │
│  │  │          return that provider                          │  │  │
│  │  │                                                         │  │  │
│  │  │  else if (preferredTier == "Pro"):                     │  │  │
│  │  │      try OpenAI → Azure → Gemini                       │  │  │
│  │  │      if none available, fallback to Free              │  │  │
│  │  │                                                         │  │  │
│  │  │  else if (preferredTier == "Free"):                    │  │  │
│  │  │      try Ollama → RuleBased                           │  │  │
│  │  │                                                         │  │  │
│  │  │  *** CRITICAL FIX ***                                   │  │  │
│  │  │  if (nothing found in dictionary):                     │  │  │
│  │  │      return "RuleBased" (guaranteed fallback)          │  │  │
│  │  │      ↓                                                  │  │  │
│  │  │  NEVER THROW "No LLM providers available"             │  │  │
│  │  └────────────────────────────────────────────────────────┘  │  │
│  │                                                               │  │
│  │  TryGenerateWithProviderAsync("RuleBased")                   │  │
│  │      │                                                        │  │
│  │      if provider not in dictionary:                          │  │
│  │          if providerName == "RuleBased":                     │  │
│  │              instantiate via reflection ──┐                  │  │
│  │              cache in dictionary          │                  │  │
│  │                                            │                  │  │
│  └────────────────────────────────────────────┼──────────────────┘  │
└─────────────────────────────────────────────────┼──────────────────┘
                                                  │
                                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         PROVIDER LAYER                               │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  LLM Providers:                                               │  │
│  │    • RuleBasedLlmProvider (always instantiable)              │  │
│  │    • OllamaLlmProvider (if running)                          │  │
│  │    • OpenAiLlmProvider (if key configured)                   │  │
│  │    • AzureOpenAiLlmProvider (if key configured)              │  │
│  │    • GeminiLlmProvider (if key configured)                   │  │
│  │                                                               │  │
│  │  TTS Providers:                                               │  │
│  │    • WindowsTtsProvider (always available)                   │  │
│  │    • ElevenLabsTtsProvider (if key configured)               │  │
│  │    • PlayHTTtsProvider (if key configured)                   │  │
│  │                                                               │  │
│  │  Visual Providers:  *** NEW ***                              │  │
│  │    • StockImageProvider (always available)                   │  │
│  │    • StableDiffusionWebUiProvider (if SD running, NVIDIA)    │  │
│  │    • StabilityImageProvider (NEW - if key configured)        │  │
│  │    • RunwayImageProvider (NEW - if key configured)           │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                      PREFLIGHT SERVICE                               │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  CheckVisualsStageAsync(profile)                              │  │
│  │                                                               │  │
│  │  if profile == "Pro" or "CloudPro":                           │  │
│  │      ✓ Check Stability API (health endpoint)                 │  │
│  │      ✓ Check Runway API (health endpoint)                    │  │
│  │      ✗ REMOVED: "cloud not yet implemented"                  │  │
│  │                                                               │  │
│  │  Returns:                                                      │  │
│  │    { stage: "Visuals",                                        │  │
│  │      status: "Available" | "Configured" | "Unreachable",     │  │
│  │      provider: "Stability" | "Runway" | "Stock",             │  │
│  │      message: "...",                                          │  │
│  │      hint: "Configure API key in Settings" }                 │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘

KEY CHANGES:
✅ ProviderMixer NEVER throws "No LLM providers available"
✅ Per-stage provider selection in UI with clear labels
✅ Cloud visual providers (Stability & Runway) implemented
✅ Preflight checks cloud providers (no "not implemented")
✅ Guaranteed fallback to RuleBased (dynamic instantiation)
✅ Provider name normalization (case-insensitive)
✅ All 432 .NET tests + 30 web tests passing
```
