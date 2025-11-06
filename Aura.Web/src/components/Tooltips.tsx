import { Link, tokens } from '@fluentui/react-components';

/**
 * Centralized tooltip content for the Create Wizard
 * Each tooltip includes helpful text and links to documentation
 */

// eslint-disable-next-line react-refresh/only-export-components -- Constant data closely tied to TooltipWithLink component
export const TooltipContent = {
  // Brief Section
  topic: {
    text: 'The main subject or theme of your video. Be specific (e.g., "How to train a puppy") rather than vague (e.g., "Dogs") for better results.',
    docLink: null,
  },
  audience: {
    text: 'Target viewer demographic. This affects language complexity and presentation style. Choose carefully to match your viewers.',
    docLink: '/docs/UX_GUIDE.md#audience',
  },
  goal: {
    text: 'Primary purpose: Inform (teach facts), Entertain (engage), Persuade (convince), or Educate (deep learning).',
    docLink: null,
  },
  tone: {
    text: 'Informative: fact-focused. Casual: friendly chat. Professional: business setting. Energetic: upbeat and dynamic.',
    docLink: '/docs/UX_GUIDE.md#tone',
  },
  language: {
    text: 'Language for both script generation and narration. This affects voice selection and subtitle generation.',
    docLink: null,
  },
  aspect: {
    text: '16:9 best for YouTube and desktop viewing. 9:16 for TikTok, Instagram Stories, mobile. 1:1 for Instagram feed.',
    docLink: null,
  },

  // Plan Section
  duration: {
    text: 'Recommended: 30-60 seconds for social media, 2-5 minutes for tutorials, 5-10 minutes for in-depth content.',
    docLink: null,
  },
  pacing: {
    text: 'Chill (120-140 WPM, relaxed delivery). Conversational (150-170 WPM, natural). Fast (180-200 WPM, high energy).',
    docLink: '/docs/UX_GUIDE.md#pacing',
  },
  density: {
    text: 'Sparse: focused, one idea per scene. Balanced: moderate detail. Dense: comprehensive, lots of information.',
    docLink: '/docs/UX_GUIDE.md#density',
  },
  style: {
    text: 'Affects script structure and visual generation approach. Educational: clear steps. Cinematic: storytelling.',
    docLink: null,
  },

  // Brand Kit
  brandKit: {
    text: 'Add your logo, colors, and branding to maintain consistent visual identity.',
    docLink: '/docs/UX_GUIDE.md#brand-kit',
  },
  watermark: {
    text: 'PNG or SVG logo overlay. Recommended: transparent background, max 200px height.',
    docLink: null,
  },
  brandColor: {
    text: 'Primary brand color in hex format (#FF6B35). Used for subtle overlays.',
    docLink: null,
  },
  accentColor: {
    text: 'Secondary color for highlights and text (#00D9FF).',
    docLink: null,
  },

  // Captions
  captionsStyle: {
    text: 'Configure subtitle appearance when burned into video.',
    docLink: '/docs/TTS-and-Captions.md',
  },
  captionsFormat: {
    text: 'SRT for broad compatibility, VTT for web/HTML5 video players.',
    docLink: null,
  },
  captionsBurnIn: {
    text: 'When enabled, captions are permanently embedded in video. Cannot be disabled by viewers.',
    docLink: null,
  },

  // Stock Sources
  stockSources: {
    text: 'Enable stock image/video providers. More sources = more visual variety.',
    docLink: '/docs/UX_GUIDE.md#stock-sources',
  },
  pexels: {
    text: 'High-quality free stock photos and videos. API key recommended for higher limits.',
    docLink: 'https://www.pexels.com/api/',
  },
  pixabay: {
    text: 'Large library of free images and videos. API key recommended.',
    docLink: 'https://pixabay.com/api/docs/',
  },
  unsplash: {
    text: 'Beautiful free photography. API key required for production use.',
    docLink: 'https://unsplash.com/developers',
  },
  localAssets: {
    text: 'Use your own images/videos from a local folder.',
    docLink: null,
  },
  stableDiffusion: {
    text: 'Generate custom images with AI. Requires local Stable Diffusion installation.',
    docLink: '/docs/LOCAL_PROVIDERS_SETUP.md',
  },

  // Offline Mode
  offline: {
    text: 'Use only local resources (Ollama, Windows TTS). No cloud API calls.',
    docLink: '/docs/UX_GUIDE.md#offline-mode',
  },

  // Advanced Settings
  advanced: {
    text: 'Power user features for fine-tuning generation parameters.',
    docLink: '/docs/UX_GUIDE.md#advanced',
  },
  voiceSettings: {
    text: 'Adjust speech rate, pitch, and pause style for narration.',
    docLink: '/docs/TTS-and-Captions.md',
  },
  sdParams: {
    text: 'Stable Diffusion parameters: steps, CFG scale, seed, dimensions.',
    docLink: '/docs/LOCAL_PROVIDERS_SETUP.md#stable-diffusion',
  },

  // Keyboard Shortcuts
  keyboardShortcuts: {
    text: 'Press Ctrl+K to view all keyboard shortcuts.',
    docLink: null,
  },

  // Voice/TTS Providers
  ttsWindowsSapi: {
    text: 'Windows SAPI: Free, offline, works only on Windows. Basic quality, no API key needed. Best for testing.',
    docLink: null,
  },
  ttsPiper: {
    text: 'Piper: Free offline neural TTS. Requires model download. Good quality, works offline, cross-platform.',
    docLink: '/docs/LOCAL_PROVIDERS_SETUP.md#piper',
  },
  ttsElevenLabs: {
    text: 'ElevenLabs: Premium cloud TTS with realistic voices. Requires API key and costs per character. Excellent quality.',
    docLink: 'https://elevenlabs.io/pricing',
  },
  ttsPlayHT: {
    text: 'PlayHT: Premium cloud TTS with voice cloning. Requires subscription. High quality with custom voices.',
    docLink: 'https://play.ht/pricing',
  },
  ttsMimic3: {
    text: 'Mimic3: Free offline neural TTS. Requires local installation. Good quality, privacy-focused.',
    docLink: '/docs/LOCAL_PROVIDERS_SETUP.md#mimic3',
  },
  voiceProvider: {
    text: 'Choose based on your needs: Windows SAPI/Piper for offline/free, ElevenLabs/PlayHT for premium quality.',
    docLink: null,
  },

  // Settings - Performance
  hardwareAcceleration: {
    text: 'Uses GPU to speed up video encoding. NVENC requires NVIDIA GPU (RTX 20/30/40 series). QuickSync requires Intel CPU with integrated graphics.',
    docLink: null,
  },
  qualityModeDraft: {
    text: 'Draft mode: Fastest render, lower quality. Expect 2-5x realtime speed. Good for quick previews.',
    docLink: null,
  },
  qualityModeStandard: {
    text: 'Standard mode: Balanced quality and speed. Expect 1-2x realtime speed. Recommended for most videos.',
    docLink: null,
  },
  qualityModeHigh: {
    text: 'High Quality: Better visual quality, slower render. Expect 0.5-1x realtime speed. Use for final output.',
    docLink: null,
  },
  qualityModeMaximum: {
    text: 'Maximum Quality: Best possible quality, slowest render. Expect 0.2-0.5x realtime speed. Use for archival/professional work.',
    docLink: null,
  },
  encoderNvenc: {
    text: 'NVIDIA NVENC: Hardware encoding on NVIDIA GPUs. Very fast (5-10x realtime) but requires RTX 20 series or newer.',
    docLink: null,
  },
  encoderQuickSync: {
    text: 'Intel QuickSync: Hardware encoding on Intel CPUs with integrated graphics. Fast (3-7x realtime).',
    docLink: null,
  },
  encoderSoftware: {
    text: 'Software encoding (x264): Slower but works on any hardware. Better quality per file size. Expect 0.5-2x realtime.',
    docLink: null,
  },
  parallelJobs: {
    text: 'Number of videos to render simultaneously. More jobs = faster overall but uses more RAM. Recommend: 1 job per 8GB RAM.',
    docLink: null,
  },
  renderThreads: {
    text: 'CPU cores dedicated to rendering. Auto (0) uses all available cores. Lower for background rendering.',
    docLink: null,
  },

  // Settings - API Keys / Providers
  apiKeyOpenAI: {
    text: 'OpenAI API key for GPT models (starts with sk- or sk-proj-). Get from platform.openai.com. GPT-4: best quality, higher cost. GPT-3.5-Turbo: faster, lower cost.',
    docLink: 'https://platform.openai.com/api-keys',
  },
  apiKeyAnthropic: {
    text: 'Anthropic API key for Claude models. Get from console.anthropic.com. Claude excels at detailed, nuanced content.',
    docLink: 'https://console.anthropic.com/settings/keys',
  },
  apiKeyGoogleGemini: {
    text: 'Google AI API key for Gemini models. Free tier available. Get from ai.google.dev.',
    docLink: 'https://ai.google.dev',
  },
  apiKeyElevenLabs: {
    text: 'ElevenLabs API key for premium TTS. Costs per character (approx $0.30 per 1000 chars). Get from elevenlabs.io.',
    docLink: 'https://elevenlabs.io/app/settings/api-keys',
  },
  apiKeyPlayHT: {
    text: 'PlayHT API key for voice cloning TTS. Subscription required. Get from play.ht.',
    docLink: 'https://play.ht/studio/settings/api-keys',
  },
  apiKeyPexels: {
    text: 'Pexels API key for stock photos/videos. Free tier: 200 requests/hour. Get from pexels.com/api.',
    docLink: 'https://www.pexels.com/api/new/',
  },
  apiKeyUnsplash: {
    text: 'Unsplash API key for stock photography. Free tier: 50 requests/hour. Get from unsplash.com/developers.',
    docLink: 'https://unsplash.com/oauth/applications/new',
  },
  localVsCloud: {
    text: 'Local providers (Ollama, Piper): Free, private, offline but require setup. Cloud: Easy, high quality but cost money.',
    docLink: null,
  },
  stableDiffusionLocal: {
    text: 'Local Stable Diffusion for custom image generation. Requires GPU (8GB+ VRAM recommended). Free but slower than cloud.',
    docLink: '/docs/LOCAL_PROVIDERS_SETUP.md#stable-diffusion',
  },

  // Welcome Page
  welcomeQuickDemo: {
    text: 'Generate a sample video with safe defaults. Takes 2-5 minutes. No configuration needed. Great for first-time users.',
    docLink: null,
  },
  welcomeCreateNew: {
    text: 'Start the full video creation wizard. Customize topic, style, voice, and quality. Expect 5-15 minutes for first video.',
    docLink: null,
  },
  welcomeSettings: {
    text: 'Configure API keys, hardware acceleration, and defaults. Set up once, use everywhere.',
    docLink: null,
  },
  welcomeSystemStatus: {
    text: 'View your hardware capabilities and available providers. Check if GPU acceleration is available.',
    docLink: null,
  },

  // Video Editor
  editorTimeline: {
    text: 'Timeline shows your video clips over time. Drag clips to reorder, resize to adjust duration. Click to select.',
    docLink: null,
  },
  editorPlayhead: {
    text: 'The playhead (red line) shows current playback position. Drag to scrub through your video.',
    docLink: null,
  },
  editorVideoTrack: {
    text: 'Video track holds visual clips (images, videos). Stack tracks for picture-in-picture or overlays.',
    docLink: null,
  },
  editorAudioTrack: {
    text: 'Audio track holds sound clips (narration, music, effects). Multiple audio tracks mix together.',
    docLink: null,
  },
  editorTransform: {
    text: 'Transform controls adjust position (X, Y), size (Scale), and rotation of selected clip.',
    docLink: null,
  },
  editorEffectBrightness: {
    text: 'Brightness: Adjust overall light levels. Negative values darken, positive values brighten.',
    docLink: null,
  },
  editorEffectContrast: {
    text: 'Contrast: Adjust difference between light and dark areas. Higher values = more dramatic.',
    docLink: null,
  },
  editorEffectSaturation: {
    text: 'Saturation: Color intensity. 0 = grayscale, 100 = normal, 200 = very vibrant colors.',
    docLink: null,
  },
  editorEffectBlur: {
    text: 'Blur: Softens image. Use for backgrounds, privacy, or artistic effect.',
    docLink: null,
  },
  editorTransition: {
    text: 'Transitions smooth the change between clips. Fade, dissolve, slide, or cut instantly.',
    docLink: null,
  },

  // Export Dialog
  exportCodecH264: {
    text: 'H.264: Best compatibility. Works on all devices and platforms. Standard choice for most videos.',
    docLink: null,
  },
  exportCodecH265: {
    text: 'H.265 (HEVC): Better compression, smaller files. Newer devices only. Use for 4K or when file size matters.',
    docLink: null,
  },
  exportCodecVP9: {
    text: 'VP9: Open-source codec, good compression. Best for web (YouTube supports natively). Slower to encode.',
    docLink: null,
  },
  exportQualityVsBitrate: {
    text: 'Higher bitrate = better quality but larger files. 5 Mbps good for 1080p, 8-10 Mbps for high quality, 20+ for 4K.',
    docLink: null,
  },
  exportHardwareEncoder: {
    text: 'Hardware encoder uses GPU for fast encoding. Quality similar to software but 5-10x faster. Requires compatible GPU.',
    docLink: null,
  },
  exportPreset: {
    text: 'Presets combine codec, resolution, and bitrate for common platforms. Custom lets you fine-tune everything.',
    docLink: null,
  },
  exportFileSize: {
    text: 'Estimated file size based on bitrate and duration. Higher bitrate = larger files. Compression quality affects size.',
    docLink: null,
  },
};

/**
 * Helper component for rendering tooltip content with optional doc link
 */
export function TooltipWithLink({
  content,
}: {
  content: { text: string; docLink: string | null };
}) {
  return (
    <div style={{ maxWidth: '300px' }}>
      {content.text}
      {content.docLink && (
        <>
          {' '}
          <Link
            href={content.docLink}
            target={content.docLink.startsWith('http') ? '_blank' : undefined}
            style={{ color: tokens.colorBrandForegroundLink }}
          >
            Learn more
          </Link>
        </>
      )}
    </div>
  );
}
