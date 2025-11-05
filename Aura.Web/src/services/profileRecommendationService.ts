/**
 * Profile recommendation service for first-run wizard
 * Provides intelligent provider profile recommendations based on hardware capabilities
 */

export interface HardwareProfile {
  vram: number;
  hasGpu: boolean;
  cpuCores?: number;
  ramGB?: number;
  gpuVendor?: string;
}

export interface ProfileRecommendation {
  tier: 'free' | 'pro';
  confidence: 'high' | 'medium' | 'low';
  reasoning: string[];
  providers: {
    llm: string[];
    tts: string[];
    image: string[];
  };
  explanations: {
    llm: string;
    tts: string;
    image: string;
  };
}

/**
 * Recommend the best provider profile based on detected hardware
 */
export function recommendProfile(hardware: HardwareProfile | null): ProfileRecommendation {
  // Default to free tier if no hardware detected
  if (!hardware) {
    return {
      tier: 'free',
      confidence: 'high',
      reasoning: [
        'Hardware detection was not available',
        'Free providers work on all systems',
        'You can always upgrade to pro providers later',
      ],
      providers: {
        llm: ['Ollama (Local)', 'Rule-based'],
        tts: ['Windows SAPI', 'Piper TTS'],
        image: ['Stock Images'],
      },
      explanations: {
        llm: 'Ollama runs locally for privacy, or use rule-based templates for simple scripts',
        tts: 'Windows SAPI provides built-in voices, Piper offers better quality offline',
        image: 'Stock images from Pexels/Unsplash are free and high-quality',
      },
    };
  }

  // Analyze hardware capabilities
  const canRunSD = hardware.hasGpu && hardware.vram >= 6;
  const hasDecentGpu = hardware.hasGpu && hardware.vram >= 4;
  const hasGoodRam = (hardware.ramGB || 0) >= 16;
  const hasEnoughRam = (hardware.ramGB || 0) >= 8;

  // High-end system: recommend trying pro providers
  if (canRunSD && hasGoodRam) {
    return {
      tier: 'pro',
      confidence: 'high',
      reasoning: [
        `Your GPU has ${hardware.vram}GB VRAM - excellent for local AI`,
        'You can run Stable Diffusion locally',
        'Pro providers will give you the best quality',
        'Your system can handle premium features',
      ],
      providers: {
        llm: ['OpenAI GPT-4', 'Anthropic Claude', 'Ollama (Local)'],
        tts: ['ElevenLabs', 'PlayHT', 'Piper TTS'],
        image: ['Stable Diffusion (Local)', 'Replicate', 'Stock Images'],
      },
      explanations: {
        llm: 'GPT-4 or Claude provide superior script quality, with Ollama as a free backup',
        tts: 'ElevenLabs offers the most natural voices, with free alternatives available',
        image: 'Your GPU can run Stable Diffusion locally for custom images on demand',
      },
    };
  }

  // Mid-range system with GPU: mix of free and pro
  if (hasDecentGpu && hasEnoughRam) {
    return {
      tier: 'free',
      confidence: 'medium',
      reasoning: [
        `Your GPU has ${hardware.vram}GB VRAM - good for local processing`,
        'Free providers work great on your system',
        'Consider adding pro APIs for specific needs',
        'You can start free and upgrade selectively',
      ],
      providers: {
        llm: ['Ollama (Local)', 'Google Gemini (Free tier)'],
        tts: ['Piper TTS', 'Windows SAPI', 'ElevenLabs (optional)'],
        image: ['Stock Images', 'Stable Diffusion if 6GB+ VRAM'],
      },
      explanations: {
        llm: 'Ollama runs well locally, Gemini offers free cloud tier for occasional use',
        tts: 'Piper provides excellent offline quality, add ElevenLabs if you want premium',
        image: 'Stock images are reliable, add Stable Diffusion if you have 6GB+ VRAM',
      },
    };
  }

  // Basic system: stick with free
  return {
    tier: 'free',
    confidence: 'high',
    reasoning: [
      'Your system specs are best suited for free providers',
      'Free providers are optimized for efficiency',
      'You can always add pro providers later',
      'Start simple and expand as needed',
    ],
    providers: {
      llm: ['Ollama (Local)', 'Rule-based'],
      tts: ['Windows SAPI', 'Piper TTS'],
      image: ['Stock Images'],
    },
    explanations: {
      llm: 'Ollama can run smaller models efficiently, rule-based works on any system',
      tts: 'Windows SAPI is built-in, Piper offers better quality with minimal resources',
      image: 'Stock images provide consistent quality without requiring local generation',
    },
  };
}

/**
 * Get user-friendly explanation for why a specific provider is recommended
 */
export function getProviderExplanation(
  providerType: 'llm' | 'tts' | 'image',
  providerName: string,
  hardware: HardwareProfile | null
): string {
  const explanations: Record<string, Record<string, string>> = {
    llm: {
      'OpenAI GPT-4':
        'Industry-leading language model that creates the most natural, contextual scripts. Best for professional content.',
      'Anthropic Claude':
        'Excels at creative, nuanced writing. Great for storytelling and educational content.',
      'Google Gemini':
        'Free tier available with good quality. Balanced option between free and premium.',
      'Ollama (Local)':
        'Privacy-focused local AI. Requires no API keys or internet. Perfect for offline work.',
      'Rule-based':
        'Template-based generation that works everywhere. Simple, predictable, always available.',
    },
    tts: {
      ElevenLabs:
        'Most natural-sounding voices available. Industry standard for professional content. Offers 10min free per month.',
      PlayHT:
        'High-quality voices with emotion control. Supports voice cloning. Free trial available.',
      'Piper TTS':
        'Excellent offline neural voices. Fast, efficient, and completely free. Great for most use cases.',
      'Windows SAPI':
        'Built into Windows, works immediately. Good for testing and simple projects.',
      Mimic3: 'Open-source quality voices. Requires local server but provides great results.',
    },
    image: {
      'Stable Diffusion (Local)': hardware?.vram
        ? `Your ${hardware.vram}GB VRAM GPU can generate custom images locally. Unlimited free generation with full creative control.`
        : 'Requires 6GB+ VRAM GPU. Generates unlimited custom images with full control.',
      'Stock Images':
        'Reliable, high-quality photos from Pexels and Unsplash. Free, unlimited, professionally shot.',
      Replicate:
        'Cloud-based AI image generation. Pay per use (~$0.10/video). Access to latest models without local GPU.',
    },
  };

  return explanations[providerType]?.[providerName] || 'Recommended for your setup';
}

/**
 * Get tier selection guidance message
 */
export function getTierGuidance(tier: 'free' | 'pro', confidence: string): string {
  if (tier === 'free') {
    return confidence === 'high'
      ? '🎯 Perfect match: Free providers are ideal for your system and will work great!'
      : '✅ Good fit: Free providers work well on your system. Add pro providers anytime.';
  } else {
    return confidence === 'high'
      ? '🚀 Recommended: Your system can take full advantage of premium providers!'
      : '💡 Optional: Pro providers will work, but free options are also excellent.';
  }
}
