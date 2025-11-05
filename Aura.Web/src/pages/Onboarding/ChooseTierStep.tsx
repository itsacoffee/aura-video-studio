import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Card,
  Button,
  Badge,
  Tooltip,
} from '@fluentui/react-components';
import { Checkmark20Regular, Info16Regular, Lightbulb20Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import {
  recommendProfile,
  getTierGuidance,
  type HardwareProfile,
} from '../../services/profileRecommendationService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    textAlign: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  introCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
  },
  cardsContainer: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingHorizontalL,
    marginBottom: tokens.spacingVerticalL,
  },
  tierCard: {
    padding: tokens.spacingVerticalXL,
    cursor: 'pointer',
    transition: 'all 0.3s ease-in-out',
    position: 'relative',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow16,
    },
  },
  selectedCard: {
    outline: `2px solid ${tokens.colorBrandBackground}`,
    outlineOffset: '-2px',
  },
  providerCategoriesSection: {
    marginTop: tokens.spacingVerticalXL,
  },
  categoryCard: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
  },
  categoryHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
  },
  categoryIcon: {
    fontSize: '28px',
  },
  providersList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  providerRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusSmall,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  providerInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    flex: 1,
  },
  checkIcon: {
    color: tokens.colorPaletteGreenForeground1,
  },
});

export interface ChooseTierStepProps {
  selectedTier: 'free' | 'pro' | null;
  onSelectTier: (tier: 'free' | 'pro') => void;
  hardware?: HardwareProfile | null;
}

interface ProviderOption {
  name: string;
  type: 'free' | 'api';
  description?: string;
}

interface ProviderCategory {
  icon: string;
  title: string;
  description: string;
  providers: ProviderOption[];
}

export function ChooseTierStep({ selectedTier, onSelectTier, hardware }: ChooseTierStepProps) {
  const styles = useStyles();
  const [showRecommendation, setShowRecommendation] = useState(false);
  const [recommendation, setRecommendation] = useState<ReturnType<
    typeof recommendProfile
  > | null>(null);

  const handleChooseBestProfile = () => {
    const rec = recommendProfile(hardware || null);
    setRecommendation(rec);
    setShowRecommendation(true);
    onSelectTier(rec.tier);
  };

  const providerCategories: ProviderCategory[] = [
    {
      icon: '✍️',
      title: 'Script Generation (LLM)',
      description: 'Create video scripts with AI',
      providers: [
        {
          name: 'Ollama (Local AI)',
          type: 'free',
          description: 'Privacy-focused, runs on your PC',
        },
        {
          name: 'Rule-based Generator',
          type: 'free',
          description: 'Simple template-based scripts',
        },
        { name: 'OpenAI GPT-4', type: 'api', description: 'Most advanced, ~$0.15/script' },
        { name: 'Anthropic Claude', type: 'api', description: 'Creative content, ~$0.12/script' },
        { name: 'Google Gemini', type: 'api', description: 'Free tier available' },
      ],
    },
    {
      icon: '🎙️',
      title: 'Text-to-Speech (TTS)',
      description: 'Convert text to natural voice',
      providers: [
        { name: 'Windows SAPI', type: 'free', description: 'Built-in Windows voices' },
        { name: 'Piper TTS', type: 'free', description: 'Offline neural voices' },
        { name: 'Mimic3', type: 'free', description: 'Open-source quality voices' },
        { name: 'ElevenLabs', type: 'api', description: 'Professional quality, 10min free/month' },
        { name: 'PlayHT', type: 'api', description: 'Voice cloning, 10min free trial' },
      ],
    },
    {
      icon: '🎨',
      title: 'Visual Content (Images)',
      description: 'Generate or select images for videos',
      providers: [
        { name: 'Stock Images', type: 'free', description: 'Pexels/Unsplash free photos' },
        {
          name: 'Stable Diffusion (Local)',
          type: 'free',
          description: 'Custom AI images (requires GPU)',
        },
        { name: 'Replicate', type: 'api', description: 'Cloud AI generation, ~$0.10/video' },
      ],
    },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Configure Your Providers</Title2>
        <Text>Choose which services you want to use. Mix and match free and paid options!</Text>
      </div>

      <Card className={styles.introCard}>
        <div style={{ display: 'flex', alignItems: 'flex-start', gap: tokens.spacingHorizontalM }}>
          <Info16Regular style={{ marginTop: '2px', flexShrink: 0 }} />
          <div>
            <Text
              weight="semibold"
              style={{ display: 'block', marginBottom: tokens.spacingVerticalXS }}
            >
              🎯 You have full flexibility!
            </Text>
            <Text size={200} style={{ display: 'block' }}>
              This application lets you combine free local tools with any API services you have
              access to. You can use completely free options, add premium APIs for better quality,
              or mix both based on your needs. All providers can be configured or changed later in
              Settings.
            </Text>
          </div>
        </div>
      </Card>

      <Card
        style={{
          padding: tokens.spacingVerticalL,
          backgroundColor: tokens.colorBrandBackground2,
          textAlign: 'center',
          marginBottom: tokens.spacingVerticalL,
        }}
      >
        <Lightbulb20Regular style={{ fontSize: '24px', marginBottom: tokens.spacingVerticalS }} />
        <Text
          weight="semibold"
          style={{ display: 'block', marginBottom: tokens.spacingVerticalM }}
        >
          Not sure which to choose?
        </Text>
        <Button appearance="primary" onClick={handleChooseBestProfile}>
          Choose Best Profile for Me
        </Button>
        <Text
          size={200}
          style={{
            display: 'block',
            marginTop: tokens.spacingVerticalS,
            color: tokens.colorNeutralForeground2,
          }}
        >
          We&apos;ll analyze your system and recommend the optimal setup
        </Text>
      </Card>

      {showRecommendation && recommendation && (
        <Card
          style={{
            padding: tokens.spacingVerticalL,
            backgroundColor: tokens.colorPaletteGreenBackground1,
            marginBottom: tokens.spacingVerticalL,
            border: `2px solid ${tokens.colorPaletteGreenBorder1}`,
          }}
        >
          <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>
            💡 Recommendation for Your System
          </Title3>
          <Text weight="semibold" style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}>
            {getTierGuidance(recommendation.tier, recommendation.confidence)}
          </Text>
          <div style={{ marginTop: tokens.spacingVerticalM }}>
            <Text weight="semibold" style={{ display: 'block', marginBottom: tokens.spacingVerticalXS }}>
              Why we recommend this:
            </Text>
            <ul style={{ marginTop: tokens.spacingVerticalXS, paddingLeft: tokens.spacingHorizontalL }}>
              {recommendation.reasoning.map((reason, idx) => (
                <li key={idx}>
                  <Text size={200}>{reason}</Text>
                </li>
              ))}
            </ul>
          </div>
          <div style={{ marginTop: tokens.spacingVerticalM }}>
            <Text size={200} style={{ fontStyle: 'italic', color: tokens.colorNeutralForeground2 }}>
              💡 Tip: {recommendation.explanations.llm}
            </Text>
          </div>
        </Card>
      )}

      <div className={styles.cardsContainer}>
        <Card
          className={`${styles.tierCard} ${selectedTier === 'free' ? styles.selectedCard : ''}`}
          onClick={() => onSelectTier('free')}
        >
          <Title3>🆓 Start with Free Tools</Title3>
          <Text style={{ marginTop: tokens.spacingVerticalS, display: 'block' }}>
            Get started immediately with zero cost and no API keys required
          </Text>

          <div style={{ marginTop: tokens.spacingVerticalM }}>
            <Text
              weight="semibold"
              size={200}
              style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}
            >
              What you&apos;ll use:
            </Text>
            <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground2 }}>
              • Windows SAPI or Piper TTS for voices
              <br />
              • Rule-based or Ollama for scripts
              <br />
              • Stock images from free sources
              <br />
              <br />
              Perfect for testing and learning. You can add API keys anytime later!
            </Text>
          </div>

          <Button
            appearance={selectedTier === 'free' ? 'primary' : 'secondary'}
            style={{ width: '100%', marginTop: tokens.spacingVerticalL }}
            onClick={(e) => {
              e.stopPropagation();
              onSelectTier('free');
            }}
          >
            {selectedTier === 'free' ? '✓ Selected' : 'Use Free Tools Only'}
          </Button>
        </Card>

        <Card
          className={`${styles.tierCard} ${selectedTier === 'pro' ? styles.selectedCard : ''}`}
          onClick={() => onSelectTier('pro')}
        >
          <Title3>⭐ Configure API Keys</Title3>
          <Text style={{ marginTop: tokens.spacingVerticalS, display: 'block' }}>
            Add your API keys for premium services (you choose which ones)
          </Text>

          <div style={{ marginTop: tokens.spacingVerticalM }}>
            <Text
              weight="semibold"
              size={200}
              style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}
            >
              Available services:
            </Text>
            <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground2 }}>
              • OpenAI, Claude, Gemini for scripts
              <br />
              • ElevenLabs, PlayHT for pro voices
              <br />
              • Replicate for AI-generated images
              <br />
              <br />
              Add only the API keys you have. Free tools remain available as fallbacks!
            </Text>
          </div>

          <Button
            appearance={selectedTier === 'pro' ? 'primary' : 'secondary'}
            style={{ width: '100%', marginTop: tokens.spacingVerticalL }}
            onClick={(e) => {
              e.stopPropagation();
              onSelectTier('pro');
            }}
          >
            {selectedTier === 'pro' ? '✓ Selected' : 'I Have API Keys'}
          </Button>
        </Card>
      </div>

      <div className={styles.providerCategoriesSection}>
        <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>
          Available Provider Options
        </Title3>
        <Text
          style={{
            display: 'block',
            marginBottom: tokens.spacingVerticalL,
            color: tokens.colorNeutralForeground2,
          }}
        >
          Here&apos;s what you can choose from in each category. You can mix free and paid options
          as needed.
        </Text>

        {providerCategories.map((category, index) => (
          <Card key={index} className={styles.categoryCard}>
            <div className={styles.categoryHeader}>
              <span className={styles.categoryIcon}>{category.icon}</span>
              <div>
                <Title3 style={{ marginBottom: tokens.spacingVerticalXXS }}>
                  {category.title}
                </Title3>
                <Text size={200} style={{ color: tokens.colorNeutralForeground2 }}>
                  {category.description}
                </Text>
              </div>
            </div>

            <div className={styles.providersList}>
              {category.providers.map((provider, providerIndex) => (
                <div key={providerIndex} className={styles.providerRow}>
                  <div className={styles.providerInfo}>
                    <Checkmark20Regular className={styles.checkIcon} />
                    <div>
                      <Text weight="semibold" size={200}>
                        {provider.name}
                      </Text>
                      {provider.description && (
                        <Text
                          size={100}
                          style={{ display: 'block', color: tokens.colorNeutralForeground2 }}
                        >
                          {provider.description}
                        </Text>
                      )}
                    </div>
                  </div>
                  <Badge
                    appearance="filled"
                    color={provider.type === 'free' ? 'success' : 'informative'}
                  >
                    {provider.type === 'free' ? 'FREE' : 'API'}
                  </Badge>
                </div>
              ))}
            </div>
          </Card>
        ))}
      </div>

      <Card
        style={{
          padding: tokens.spacingVerticalL,
          backgroundColor: tokens.colorNeutralBackground3,
        }}
      >
        <Text size={200} style={{ display: 'block', textAlign: 'center' }}>
          💡 <strong>Remember:</strong> You can start with free tools and add API keys later, or mix
          and match any combination. The application automatically uses available providers and
          falls back to free options when needed.
        </Text>
      </Card>
    </div>
  );
}
