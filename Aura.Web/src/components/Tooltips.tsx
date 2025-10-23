import { Link, tokens } from '@fluentui/react-components';

/**
 * Centralized tooltip content for the Create Wizard
 * Each tooltip includes helpful text and links to documentation
 */

export const TooltipContent = {
  // Brief Section
  topic: {
    text: 'The main subject or theme of your video. Be specific for better results.',
    docLink: null,
  },
  audience: {
    text: 'Target viewer demographic. This affects language complexity and presentation style.',
    docLink: '/docs/UX_GUIDE.md#audience',
  },
  goal: {
    text: 'Primary purpose of the video (Inform, Entertain, Persuade, Educate).',
    docLink: null,
  },
  tone: {
    text: 'Overall mood and style. Choose from Informative, Casual, Professional, or Energetic.',
    docLink: '/docs/UX_GUIDE.md#tone',
  },
  language: {
    text: 'Language for narration and captions. Supports multiple languages.',
    docLink: null,
  },
  aspect: {
    text: 'Video dimensions. 16:9 for YouTube/desktop, 9:16 for mobile/stories, 1:1 for social feeds.',
    docLink: null,
  },

  // Plan Section
  duration: {
    text: 'Target length in minutes. Longer videos require more content and processing time.',
    docLink: null,
  },
  pacing: {
    text: 'Narration speed: Chill (relaxed), Conversational (normal), Fast (energetic).',
    docLink: '/docs/UX_GUIDE.md#pacing',
  },
  density: {
    text: 'Content amount per minute: Sparse (minimal), Balanced (moderate), Dense (maximum).',
    docLink: '/docs/UX_GUIDE.md#density',
  },
  style: {
    text: 'Visual presentation style (Educational, Cinematic, Documentary, etc.).',
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
