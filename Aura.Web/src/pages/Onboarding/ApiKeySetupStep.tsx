import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
  Badge,
  Divider,
  Checkbox,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
} from '@fluentui/react-components';
import { useState, useEffect, useCallback } from 'react';
import { Spinner } from '@fluentui/react-components';
import { Checkmark24Regular, Warning24Regular } from '@fluentui/react-icons';
import { EnhancedApiKeyInput } from '../../components/Onboarding/EnhancedApiKeyInput';
import type { FieldValidationError } from '../../components/Onboarding/FieldValidationErrors';
import { ProviderHelpPanel } from '../../components/ProviderHelpPanel';
import { offlineProvidersApi } from '../../services/api/offlineProvidersApi';
import type { OfflineProviderStatus } from '@/types/offlineProviders';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    textAlign: 'center',
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  sectionHeader: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    marginBottom: tokens.spacingVerticalS,
  },
  sectionTitle: {
    marginTop: tokens.spacingVerticalL,
  },
  sectionDescription: {
    color: tokens.colorNeutralForeground3,
  },
  providerAccordion: {
    width: '100%',
  },
  providerHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    width: '100%',
  },
  providerLogo: {
    fontSize: '24px',
  },
  providerInfo: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  providerContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
  },
  buttonRow: {
    display: 'flex',
    justifyContent: 'space-between',
    marginTop: tokens.spacingVerticalL,
  },
  infoCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  quickStartCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  quickStartActions: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalS,
  },
  quickStartHint: {
    color: tokens.colorNeutralForeground4,
  },
  localProviderCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  localInstructions: {
    marginLeft: tokens.spacingHorizontalL,
    color: tokens.colorNeutralForeground3,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  localActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
  successText: {
    color: tokens.colorPaletteGreenForeground1,
  },
});

export interface ApiKeySetupStepProps {
  apiKeys: Record<string, string>;
  validationStatus: Record<string, 'idle' | 'validating' | 'valid' | 'invalid'>;
  validationErrors: Record<string, string>;
  fieldErrors?: Record<string, FieldValidationError[]>;
  accountInfo?: Record<string, string>;
  onApiKeyChange: (provider: string, value: string) => void;
  onValidateApiKey: (provider: string) => void;
  onSkipValidation?: (provider: string) => void;
  onSkipAll: () => void;
  onLocalProviderReady?: (provider: string) => void;
  allowInvalidKeys?: boolean;
  onAllowInvalidKeysChange?: (allow: boolean) => void;
}

interface ProviderConfig {
  id: string;
  name: string;
  logo: string;
  description: string;
  usedFor: string;
  signupUrl: string;
  steps: string[];
  pricingInfo: {
    freeTier?: string;
    costEstimate: string;
  };
  keyFormat?: string;
  requiresMultipleKeys?: boolean;
  category: 'llm' | 'tts' | 'image' | 'other';
  requiresApiKey?: boolean;
  localSetup?: {
    downloadUrl?: string;
    instructions?: string[];
    readyHint?: string;
  };
}

interface ProviderSection {
  id: string;
  title: string;
  description: string;
  category: 'llm' | 'tts' | 'image' | 'other';
}

const sections: ProviderSection[] = [
  {
    id: 'llm',
    title: 'LLM Providers',
    description: 'Large Language Models for script generation and content creation',
    category: 'llm',
  },
  {
    id: 'tts',
    title: 'Text-to-Speech',
    description: 'AI voice synthesis for narration and voiceovers',
    category: 'tts',
  },
  {
    id: 'image',
    title: 'Image Services',
    description: 'Stock images and AI-generated visuals for video content',
    category: 'image',
  },
];

const providers: ProviderConfig[] = [
  {
    id: 'openai',
    name: 'OpenAI',
    logo: '🤖',
    description: 'Advanced AI models for high-quality script generation',
    usedFor: 'Script generation with OpenAI GPT models, industry-leading AI language models',
    signupUrl: 'https://platform.openai.com/signup',
    steps: [
      'Create an account at platform.openai.com',
      'Add a payment method to your account',
      'Go to the API Keys section',
      'Create a new secret key',
      'Copy the key (starts with "sk-" or "sk-proj-")',
    ],
    pricingInfo: {
      freeTier: '$5 credit for new users',
      costEstimate: '~$0.15 per 5-minute video script',
    },
    keyFormat: 'start with "sk-" or "sk-proj-"',
    category: 'llm',
  },
  {
    id: 'anthropic',
    name: 'Anthropic',
    logo: '🧠',
    description: 'Claude for alternative script generation',
    usedFor: 'Alternative script generation with Claude, known for detailed and creative content',
    signupUrl: 'https://console.anthropic.com',
    steps: [
      'Create an account at console.anthropic.com',
      'Add a payment method',
      'Navigate to API Keys section',
      'Create a new key',
      'Copy the key (starts with "sk-ant-")',
    ],
    pricingInfo: {
      freeTier: '$5 credit for new users',
      costEstimate: '~$0.12 per 5-minute video script',
    },
    keyFormat: 'start with "sk-ant-"',
    category: 'llm',
  },
  {
    id: 'gemini',
    name: 'Google Gemini',
    logo: '✨',
    description: 'Gemini Pro for script generation',
    usedFor: "Script generation with Google's Gemini Pro model",
    signupUrl: 'https://makersuite.google.com/app/apikey',
    steps: [
      'Sign in with your Google account',
      'Navigate to MakerSuite',
      'Click "Create API Key"',
      'Copy the generated key (39 characters)',
    ],
    pricingInfo: {
      freeTier: 'Free tier available with rate limits',
      costEstimate: '~$0.10 per 5-minute script on paid tier',
    },
    keyFormat: 'are 39 characters long',
    category: 'llm',
  },
  {
    id: 'ollama',
    name: 'Ollama',
    logo: '🦙',
    description: 'Local LLM for offline script generation',
    usedFor: 'Run AI models locally on your machine for script generation without API costs',
    signupUrl: 'https://ollama.ai',
    steps: [
      'Download and install Ollama from ollama.ai',
      'Run Ollama in the background',
      'Pull a model (e.g., "ollama pull llama2")',
      'No API key required - runs locally',
    ],
    pricingInfo: {
      freeTier: 'Completely free - runs on your hardware',
      costEstimate: 'Free (requires local compute resources)',
    },
    keyFormat: 'No API key needed for local Ollama',
    requiresApiKey: false,
    localSetup: {
      downloadUrl: 'https://ollama.ai/download',
      instructions: [
        'Install Ollama and ensure the Ollama service is running.',
        'Pull at least one model (e.g., `ollama pull llama3.1`)',
        'Leave the Ollama tray/service running while Aura is open.',
      ],
      readyHint: 'Click "Mark as Ready" once Ollama is installed and running.',
    },
    category: 'llm',
  },
  {
    id: 'elevenlabs',
    name: 'ElevenLabs',
    logo: '🎙️',
    description: 'High-quality AI voice synthesis',
    usedFor: 'Professional AI voice synthesis with natural-sounding voices',
    signupUrl: 'https://elevenlabs.io/sign-up',
    steps: [
      'Create an account at elevenlabs.io',
      'Subscribe to a plan (or use free tier)',
      'Go to Profile Settings',
      'Find the API Key section',
      'Copy your API key (32-character hex string)',
    ],
    pricingInfo: {
      freeTier: '10,000 characters/month (~10 minutes of audio)',
      costEstimate: '$5/month for 30,000 characters (~30 minutes)',
    },
    keyFormat: 'are 32-character hex strings',
    category: 'tts',
  },
  {
    id: 'playht',
    name: 'PlayHT',
    logo: '🔊',
    description: 'Alternative AI voice synthesis',
    usedFor: 'Alternative AI voice synthesis with multiple voice options',
    signupUrl: 'https://play.ht/signup',
    steps: [
      'Create an account at play.ht',
      'Subscribe to a plan',
      'Navigate to Settings',
      'Find API Credentials section',
      'Copy both User ID and Secret Key',
    ],
    pricingInfo: {
      freeTier: '12,500 characters (~10 minutes)',
      costEstimate: '$31/month for 12.5 hours of audio',
    },
    requiresMultipleKeys: true,
    category: 'tts',
  },
  {
    id: 'windows',
    name: 'Windows TTS',
    logo: '🪟',
    description: 'Built-in Windows text-to-speech',
    usedFor: 'Native Windows speech synthesis - always available, no setup required',
    signupUrl: '',
    steps: [
      'No setup required - uses Windows built-in TTS',
      'Available on all Windows 10+ systems',
      'No API key needed - completely free',
    ],
    pricingInfo: {
      freeTier: 'Completely free - built into Windows',
      costEstimate: 'Free (no costs, no limits)',
    },
    keyFormat: 'No API key needed - built into Windows',
    requiresApiKey: false,
    localSetup: {
      downloadUrl: '',
      instructions: [
        'Windows TTS is built into Windows 10 and later',
        'No installation or configuration needed',
        'Automatically available and ready to use',
      ],
      readyHint: 'Windows TTS is always ready - click "Mark as Ready" to confirm.',
    },
    category: 'tts',
  },
  {
    id: 'piper',
    name: 'Piper TTS',
    logo: '🎤',
    description: 'Fast local TTS, works offline',
    usedFor: 'High-quality offline text-to-speech without API costs',
    signupUrl: 'https://github.com/rhasspy/piper',
    steps: [
      'Install Piper via Download Center in Settings',
      'Download voice models (50+ voices available)',
      'No API key required - runs locally',
    ],
    pricingInfo: {
      freeTier: 'Completely free - runs on your hardware',
      costEstimate: 'Free (requires local installation)',
    },
    keyFormat: 'No API key needed for local Piper',
    requiresApiKey: false,
    localSetup: {
      downloadUrl: 'https://github.com/rhasspy/piper/releases',
      instructions: [
        'Install Piper via Settings → Download Center → Engines',
        'Download at least one voice model',
        'Piper will be automatically detected when installed',
      ],
      readyHint: 'Click "Mark as Ready" once Piper is installed and voice models are downloaded.',
    },
    category: 'tts',
  },
  {
    id: 'mimic3',
    name: 'Mimic3 TTS',
    logo: '🎭',
    description: 'Neural TTS, works offline',
    usedFor: 'High-quality neural text-to-speech with natural voices',
    signupUrl: 'https://github.com/MycroftAI/mimic3',
    steps: [
      'Install Mimic3 server (runs on port 59125)',
      'Ensure the Mimic3 service is running',
      'No API key required - runs locally',
    ],
    pricingInfo: {
      freeTier: 'Completely free - runs on your hardware',
      costEstimate: 'Free (requires local installation)',
    },
    keyFormat: 'No API key needed for local Mimic3',
    requiresApiKey: false,
    localSetup: {
      downloadUrl: 'https://github.com/MycroftAI/mimic3',
      instructions: [
        'Install Mimic3 server (see Download Center in Settings)',
        'Start the Mimic3 service (runs on port 59125)',
        'Leave the Mimic3 service running while Aura is open',
      ],
      readyHint: 'Click "Mark as Ready" once Mimic3 is installed and the service is running.',
    },
    category: 'tts',
  },
  {
    id: 'placeholder',
    name: 'Placeholder Images',
    logo: '🎨',
    description: 'Solid color backgrounds with text - always available',
    usedFor: 'Guaranteed fallback image provider - generates solid color backgrounds with text overlays',
    signupUrl: '',
    steps: [
      'No setup required - always available',
      'Generates solid color backgrounds automatically',
      'No API key needed - built-in fallback',
    ],
    pricingInfo: {
      freeTier: 'Completely free - built into Aura',
      costEstimate: 'Free (no costs, no limits)',
    },
    keyFormat: 'No API key needed - always available',
    requiresApiKey: false,
    localSetup: {
      downloadUrl: '',
      instructions: [
        'Placeholder images are built into Aura',
        'No installation or configuration needed',
        'Automatically available as fallback when other providers fail',
      ],
      readyHint: 'Placeholder images are always ready - click "Mark as Ready" to confirm.',
    },
    category: 'image',
  },
  {
    id: 'replicate',
    name: 'Replicate',
    logo: '🎨',
    description: 'Image generation with Stable Diffusion',
    usedFor: 'Custom image generation using Stable Diffusion and other AI models',
    signupUrl: 'https://replicate.com/signin',
    steps: [
      'Sign in with GitHub at replicate.com',
      'Go to Account Settings',
      'Navigate to API Tokens',
      'Create a new token',
      'Copy the token (starts with "r8_")',
    ],
    pricingInfo: {
      freeTier: '$5 credit for new users',
      costEstimate: '~$0.0023 per image (~$0.10 per video)',
    },
    keyFormat: 'start with "r8_"',
    category: 'image',
  },
  {
    id: 'pexels',
    name: 'Pexels',
    logo: '📷',
    description: 'Free stock photos and videos',
    usedFor: 'Access to high-quality stock images and video content for your projects',
    signupUrl: 'https://www.pexels.com/api/',
    steps: [
      'Create a free account at pexels.com',
      'Navigate to the API page',
      'Click "Get Started"',
      'Copy your API key',
    ],
    pricingInfo: {
      freeTier: 'Free with 200 requests/hour',
      costEstimate: 'Completely free for all usage',
    },
    keyFormat: 'are alphanumeric strings',
    category: 'image',
  },
];

export function ApiKeySetupStep({
  apiKeys,
  validationStatus,
  validationErrors: _validationErrors,
  fieldErrors = {},
  accountInfo = {},
  onApiKeyChange,
  onValidateApiKey,
  onSkipValidation,
  onSkipAll,
  onLocalProviderReady,
  allowInvalidKeys = false,
  onAllowInvalidKeysChange,
}: ApiKeySetupStepProps) {
  const styles = useStyles();
  const [rateLimit, setRateLimit] = useState<Record<string, number>>({});
  const [localTtsStatus, setLocalTtsStatus] = useState<{
    windows: OfflineProviderStatus | null;
    piper: OfflineProviderStatus | null;
    mimic3: OfflineProviderStatus | null;
  }>({
    windows: { name: 'Windows TTS', isAvailable: true, message: 'Built into Windows - always available' },
    piper: null,
    mimic3: null,
  });
  const [checkingTts, setCheckingTts] = useState<{
    windows: boolean;
    piper: boolean;
    mimic3: boolean;
  }>({
    windows: false,
    piper: false,
    mimic3: false,
  });

  const checkLocalTtsStatus = useCallback(async (provider: 'windows' | 'piper' | 'mimic3') => {
    setCheckingTts((prev) => ({ ...prev, [provider]: true }));
    try {
      let status: OfflineProviderStatus;
      if (provider === 'windows') {
        // Check Windows TTS via API (handles platform detection)
        try {
          status = await offlineProvidersApi.checkWindowsTts();
        } catch (error) {
          // If API fails, assume available on Windows (graceful degradation)
          console.warn('Windows TTS API check failed, assuming available:', error);
          status = {
            name: 'Windows TTS',
            isAvailable: true,
            message: 'Built into Windows - always available',
          };
        }
      } else {
        status =
          provider === 'piper'
            ? await offlineProvidersApi.checkPiper()
            : await offlineProvidersApi.checkMimic3();
      }
      setLocalTtsStatus((prev) => ({ ...prev, [provider]: status }));
    } catch (error) {
      console.error(`Failed to check ${provider} status:`, error);
      setLocalTtsStatus((prev) => ({
        ...prev,
        [provider]: {
          name:
            provider === 'windows'
              ? 'Windows TTS'
              : provider === 'piper'
                ? 'Piper TTS'
                : 'Mimic3 TTS',
          isAvailable: provider === 'windows', // Windows is always available on Windows
          message: provider === 'windows' ? 'Built into Windows' : 'Status check failed',
        },
      }));
    } finally {
      setCheckingTts((prev) => ({ ...prev, [provider]: false }));
    }
  }, []);

  // Check local TTS status on mount
  useEffect(() => {
    checkLocalTtsStatus('windows');
    checkLocalTtsStatus('piper');
    checkLocalTtsStatus('mimic3');
  }, [checkLocalTtsStatus]);

  // Auto-mark Windows TTS and Placeholder as ready on mount (they're always available)
  useEffect(() => {
    if (onLocalProviderReady) {
      // Windows TTS is always available
      onLocalProviderReady('windows');
      // Placeholder images are always available
      onLocalProviderReady('placeholder');
    }
  }, [onLocalProviderReady]);

  const openExternalLink = (url: string) => {
    if (typeof window === 'undefined') {
      return;
    }
    window.open(url, '_blank', 'noopener,noreferrer');
  };

  const handleValidate = (providerId: string) => {
    // Check rate limiting - reduced from 20s to 5s for better UX
    const now = Date.now();
    const lastAttempt = rateLimit[providerId] || 0;
    const timeSinceLastAttempt = now - lastAttempt;

    if (timeSinceLastAttempt < 5000) {
      // 5 seconds between attempts (reduced from 20)
      alert('Too many attempts. Please wait a moment before trying again.');
      return;
    }

    setRateLimit({ ...rateLimit, [providerId]: now });
    onValidateApiKey(providerId);
  };

  const hasAtLeastOneValidKey = Object.values(validationStatus).some(
    (status) => status === 'valid'
  );

  const configuredKeys = Object.entries(apiKeys).filter(([_, key]) => key && key.trim().length > 0);
  const invalidKeys = configuredKeys.filter(
    ([provider, _]) => validationStatus[provider] === 'invalid'
  );
  const hasInvalidKeys = invalidKeys.length > 0;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Configure API Keys (Optional)</Title2>
        <Text>
          Add API keys for any premium services you want to use. Pick and choose - add only what you
          have!
        </Text>
      </div>

      <div className={styles.infoCard}>
        <Text size={200}>
          💡 <strong>Mix and Match Freely:</strong> You don&apos;t need all of these, or even any!
          You can use OpenAI for scripts with local TTS for voices, or Ollama for scripts with
          ElevenLabs for voices - any combination works. The app will use what you configure and
          fall back to free tools for anything else. All changes can be made in Settings later.
        </Text>
      </div>

      <div className={styles.quickStartCard}>
        <Title3>Quick start options</Title3>
        <Text size={200}>
          Don&apos;t have anything configured yet? Grab an OpenAI key or install Ollama locally,
          then return here to validate.
        </Text>
        <div className={styles.quickStartActions}>
          <Button
            appearance="secondary"
            onClick={() => openExternalLink('https://platform.openai.com/signup')}
          >
            Get an OpenAI Key
          </Button>
          <Button
            appearance="secondary"
            onClick={() => openExternalLink('https://ollama.ai/download')}
          >
            Install Ollama
          </Button>
        </div>
        <Text size={200} className={styles.quickStartHint}>
          Tip: if you plan to use Ollama, make sure the Ollama service is running before clicking
          &quot;Validate&quot;.
        </Text>
      </div>

      {sections.map((section, index) => {
        const sectionProviders = providers.filter((p) => p.category === section.category);
        if (sectionProviders.length === 0) return null;

        return (
          <div key={section.id} className={styles.section}>
            {index > 0 && <Divider />}
            <div className={styles.sectionHeader}>
              <Title3 className={styles.sectionTitle}>{section.title}</Title3>
              <Text size={200} className={styles.sectionDescription}>
                {section.description}
              </Text>
            </div>

            <Accordion className={styles.providerAccordion} collapsible multiple>
              {sectionProviders.map((provider) => (
                <AccordionItem key={provider.id} value={provider.id}>
                  <AccordionHeader>
                    <div className={styles.providerHeader}>
                      <span className={styles.providerLogo}>{provider.logo}</span>
                      <div className={styles.providerInfo}>
                        <Text weight="semibold">{provider.name}</Text>
                        <Text size={200}>{provider.description}</Text>
                      </div>
                      <Badge
                        appearance="filled"
                        color={
                          validationStatus[provider.id] === 'valid'
                            ? 'success'
                            : validationStatus[provider.id] === 'invalid'
                              ? 'danger'
                              : 'informative'
                        }
                      >
                        {validationStatus[provider.id] === 'valid'
                          ? 'Valid'
                          : validationStatus[provider.id] === 'validating'
                            ? 'Validating...'
                            : validationStatus[provider.id] === 'invalid'
                              ? 'Invalid'
                              : 'Not Set'}
                      </Badge>
                    </div>
                  </AccordionHeader>
                  <AccordionPanel>
                    <div className={styles.providerContent}>
                      <ProviderHelpPanel
                        providerName={provider.name}
                        signupUrl={provider.signupUrl}
                        steps={provider.steps}
                        usedFor={provider.usedFor}
                        pricingInfo={provider.pricingInfo}
                        keyFormat={provider.keyFormat}
                      />

                      {provider.requiresApiKey === false ? (
                        <div className={styles.localProviderCard}>
                          <Text size={200} style={{ marginBottom: tokens.spacingVerticalS }}>
                            {provider.localSetup?.readyHint ??
                              'This provider runs locally and does not require an API key.'}
                          </Text>

                          {provider.localSetup?.instructions && (
                            <ul className={styles.localInstructions}>
                              {provider.localSetup.instructions.map((instruction) => (
                                <li key={instruction}>
                                  <Text size={200}>{instruction}</Text>
                                </li>
                              ))}
                            </ul>
                          )}

                          {/* Status check for local providers */}
                          {(provider.id === 'windows' ||
                            provider.id === 'piper' ||
                            provider.id === 'mimic3' ||
                            provider.id === 'placeholder') && (
                            <div
                              style={{
                                marginTop: tokens.spacingVerticalM,
                                padding: tokens.spacingVerticalS,
                                backgroundColor: tokens.colorNeutralBackground2,
                                borderRadius: tokens.borderRadiusSmall,
                                display: 'flex',
                                alignItems: 'center',
                                gap: tokens.spacingHorizontalS,
                              }}
                            >
                              {checkingTts[
                                provider.id as 'windows' | 'piper' | 'mimic3'
                              ] ? (
                                <>
                                  <Spinner size="tiny" />
                                  <Text size={200}>Checking status...</Text>
                                </>
                              ) : provider.id === 'placeholder' ? (
                                <>
                                  <Checkmark24Regular
                                    style={{ color: tokens.colorPaletteGreenForeground1 }}
                                  />
                                  <Text size={200} className={styles.successText}>
                                    Always available - generates solid color backgrounds
                                  </Text>
                                </>
                              ) : localTtsStatus[provider.id as 'windows' | 'piper' | 'mimic3'] ? (
                                <>
                                  {localTtsStatus[provider.id as 'windows' | 'piper' | 'mimic3']
                                    ?.isAvailable ? (
                                    <>
                                      <Checkmark24Regular
                                        style={{ color: tokens.colorPaletteGreenForeground1 }}
                                      />
                                      <Text size={200} className={styles.successText}>
                                        {localTtsStatus[provider.id as 'windows' | 'piper' | 'mimic3']
                                          ?.message || 'Available'}
                                      </Text>
                                    </>
                                  ) : (
                                    <>
                                      <Warning24Regular
                                        style={{ color: tokens.colorPaletteYellowForeground1 }}
                                      />
                                      <Text size={200}>
                                        {localTtsStatus[provider.id as 'windows' | 'piper' | 'mimic3']
                                          ?.message || 'Not detected'}
                                      </Text>
                                    </>
                                  )}
                                  {provider.id !== 'windows' && (
                                    <Button
                                      appearance="subtle"
                                      size="small"
                                      onClick={() =>
                                        checkLocalTtsStatus(
                                          provider.id as 'windows' | 'piper' | 'mimic3'
                                        )
                                      }
                                    >
                                      Refresh
                                    </Button>
                                  )}
                                </>
                              ) : null}
                            </div>
                          )}

                          <div className={styles.localActions}>
                            <Button
                              appearance="primary"
                              onClick={() => onLocalProviderReady?.(provider.id)}
                              disabled={
                                (provider.id === 'piper' || provider.id === 'mimic3') &&
                                !localTtsStatus[provider.id as 'piper' | 'mimic3']?.isAvailable &&
                                !checkingTts[provider.id as 'piper' | 'mimic3']
                              }
                            >
                              {provider.id === 'windows' || provider.id === 'placeholder'
                                ? 'Mark as Ready (Always Available)'
                                : 'Mark as Ready'}
                            </Button>
                            {provider.localSetup?.downloadUrl && (
                              <Button
                                appearance="secondary"
                                onClick={() => openExternalLink(provider.localSetup!.downloadUrl!)}
                              >
                                {provider.id === 'piper'
                                  ? 'View Piper Releases'
                                  : provider.id === 'mimic3'
                                    ? 'View Mimic3 GitHub'
                                    : 'Download Ollama'}
                              </Button>
                            )}
                          </div>

                          {validationStatus[provider.id] === 'valid' ? (
                            <Text size={200} className={styles.successText}>
                              ✓ {provider.name} marked as ready
                            </Text>
                          ) : (
                            <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                              {provider.id === 'windows'
                                ? 'Windows TTS is built into Windows and always available. Click "Mark as Ready" to confirm.'
                                : provider.id === 'placeholder'
                                  ? 'Placeholder images are built into Aura and always available. Click "Mark as Ready" to confirm.'
                                  : provider.id === 'piper' || provider.id === 'mimic3'
                                    ? 'Install the provider locally and ensure it is detected, then mark as ready.'
                                    : 'No API key required. Install the provider locally and mark it as ready.'}
                            </Text>
                          )}
                        </div>
                      ) : (
                        <div style={{ marginTop: tokens.spacingVerticalM }}>
                          <Title3 style={{ marginBottom: tokens.spacingVerticalS }}>
                            Enter API Key
                          </Title3>
                          <EnhancedApiKeyInput
                            providerDisplayName={provider.name}
                            value={apiKeys[provider.id] || ''}
                            onChange={(value) => onApiKeyChange(provider.id, value)}
                            onValidate={() => handleValidate(provider.id)}
                            validationStatus={validationStatus[provider.id] || 'idle'}
                            fieldErrors={fieldErrors[provider.id]}
                            accountInfo={accountInfo[provider.id]}
                            onSkipValidation={
                              onSkipValidation ? () => onSkipValidation(provider.id) : undefined
                            }
                          />
                          {provider.requiresMultipleKeys && (
                            <Text size={200} style={{ marginTop: tokens.spacingVerticalS }}>
                              Note: {provider.name} requires both User ID and Secret Key (enter as
                              &quot;userId:secretKey&quot;)
                            </Text>
                          )}
                        </div>
                      )}
                    </div>
                  </AccordionPanel>
                </AccordionItem>
              ))}
            </Accordion>
          </div>
        );
      })}

      {hasInvalidKeys && (
        <MessageBar intent="warning">
          <MessageBarBody>
            <MessageBarTitle>Some API keys are invalid</MessageBarTitle>
            <Text style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}>
              The following providers have invalid API keys:{' '}
              {invalidKeys.map(([p]) => p).join(', ')}. You can continue setup and fix them later in
              Settings.
            </Text>
            {onAllowInvalidKeysChange && (
              <Checkbox
                checked={allowInvalidKeys}
                onChange={(_, data) => onAllowInvalidKeysChange(data.checked as boolean)}
                label="Allow me to continue with invalid API keys (I'll fix them later)"
              />
            )}
          </MessageBarBody>
        </MessageBar>
      )}

      <div className={styles.buttonRow}>
        <Button appearance="subtle" onClick={onSkipAll}>
          Skip All (Add Later)
        </Button>
        <Text>
          {hasAtLeastOneValidKey
            ? '✓ Ready to continue'
            : 'Add at least one API key or skip to continue'}
        </Text>
      </div>
    </div>
  );
}
