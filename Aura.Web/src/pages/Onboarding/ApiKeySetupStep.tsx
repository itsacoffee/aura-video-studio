import { useState } from 'react';
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
} from '@fluentui/react-components';
import { ApiKeyInput } from '../../components/ApiKeyInput';
import { ProviderHelpPanel } from '../../components/ProviderHelpPanel';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    textAlign: 'center',
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
});

export interface ApiKeySetupStepProps {
  apiKeys: Record<string, string>;
  validationStatus: Record<string, 'idle' | 'validating' | 'valid' | 'invalid'>;
  validationErrors: Record<string, string>;
  onApiKeyChange: (provider: string, value: string) => void;
  onValidateApiKey: (provider: string) => void;
  onSkipAll: () => void;
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
}

const providers: ProviderConfig[] = [
  {
    id: 'openai',
    name: 'OpenAI',
    logo: '🤖',
    description: 'GPT-4 for high-quality script generation',
    usedFor: 'Script generation with GPT-4, the most advanced AI language model',
    signupUrl: 'https://platform.openai.com/signup',
    steps: [
      'Create an account at platform.openai.com',
      'Add a payment method to your account',
      'Go to the API Keys section',
      'Create a new secret key',
      'Copy the key (starts with "sk-")',
    ],
    pricingInfo: {
      freeTier: '$5 credit for new users',
      costEstimate: '~$0.15 per 5-minute video script',
    },
    keyFormat: 'start with "sk-"',
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
  },
  {
    id: 'gemini',
    name: 'Google Gemini',
    logo: '✨',
    description: 'Gemini Pro for script generation',
    usedFor: 'Script generation with Google\'s Gemini Pro model',
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
  },
];

export function ApiKeySetupStep({
  apiKeys,
  validationStatus,
  validationErrors,
  onApiKeyChange,
  onValidateApiKey,
  onSkipAll,
}: ApiKeySetupStepProps) {
  const styles = useStyles();
  const [rateLimit, setRateLimit] = useState<Record<string, number>>({});

  const handleValidate = (providerId: string) => {
    // Check rate limiting
    const now = Date.now();
    const lastAttempt = rateLimit[providerId] || 0;
    const timeSinceLastAttempt = now - lastAttempt;

    if (timeSinceLastAttempt < 20000) {
      // 20 seconds between attempts
      alert('Too many attempts. Please wait a moment before trying again.');
      return;
    }

    setRateLimit({ ...rateLimit, [providerId]: now });
    onValidateApiKey(providerId);
  };

  const hasAtLeastOneValidKey = Object.values(validationStatus).some(
    (status) => status === 'valid'
  );

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Configure API Keys</Title2>
        <Text>
          Add your API keys for the services you want to use. You can skip this step and add them
          later.
        </Text>
      </div>

      <div className={styles.infoCard}>
        <Text size={200}>
          💡 <strong>Tip:</strong> You don&apos;t need all of these. Add just the ones you plan to
          use. Each provider has a free tier to get started!
        </Text>
      </div>

      <Accordion className={styles.providerAccordion} collapsible multiple>
        {providers.map((provider) => (
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

                <div style={{ marginTop: tokens.spacingVerticalM }}>
                  <Title3 style={{ marginBottom: tokens.spacingVerticalS }}>Enter API Key</Title3>
                  <ApiKeyInput
                    provider={provider.name}
                    value={apiKeys[provider.id] || ''}
                    onChange={(value) => onApiKeyChange(provider.id, value)}
                    onValidate={() => handleValidate(provider.id)}
                    validationStatus={validationStatus[provider.id] || 'idle'}
                    error={validationErrors[provider.id]}
                  />
                  {provider.requiresMultipleKeys && (
                    <Text size={200} style={{ marginTop: tokens.spacingVerticalS }}>
                      Note: {provider.name} requires both User ID and Secret Key (enter as
                      &quot;userId:secretKey&quot;)
                    </Text>
                  )}
                </div>
              </div>
            </AccordionPanel>
          </AccordionItem>
        ))}
      </Accordion>

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
