import {
  Card,
  Text,
  Button,
  Badge,
  makeStyles,
  tokens,
  Title3,
} from '@fluentui/react-components';
import {
  Lightbulb24Regular,
  ArrowRight24Regular,
  Checkmark24Regular,
} from '@fluentui/react-icons';
import type { ProviderStatus } from '../hooks/useProviderStatus';
import { useNavigate } from 'react-router-dom';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  recommendationList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  recommendationCard: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  recommendationHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  recommendationContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  recommendationActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  priorityBadge: {
    marginLeft: 'auto',
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalL,
    color: tokens.colorNeutralForeground3,
  },
});

export interface ProviderRecommendationsProps {
  llm: ProviderStatus[];
  tts: ProviderStatus[];
  images: ProviderStatus[];
}

interface Recommendation {
  priority: 'high' | 'medium' | 'low';
  title: string;
  description: string;
  action: string;
  link: string;
  category: string;
}

export function ProviderRecommendations({
  llm,
  tts,
  images,
}: ProviderRecommendationsProps) {
  const styles = useStyles();
  const navigate = useNavigate();

  const recommendations: Recommendation[] = [];

  // Check for Ollama
  const ollamaAvailable = llm.find((p) => p.name === 'Ollama')?.available;
  if (!ollamaAvailable) {
    recommendations.push({
      priority: 'high',
      title: 'Enable Local AI with Ollama',
      description:
        'Ollama provides free, private AI script generation. Install Ollama to use local models without API keys.',
      action: 'Install Ollama',
      link: 'https://ollama.com',
      category: 'LLM',
    });
  }

  // Check for better TTS
  const hasElevenLabs = tts.find((p) => p.name === 'ElevenLabs')?.available;
  const hasPaidTts = tts.some((p) => p.tier === 'paid' && p.available);
  if (!hasPaidTts) {
    recommendations.push({
      priority: 'medium',
      title: 'Upgrade Voice Quality',
      description:
        'Premium TTS providers like ElevenLabs provide more natural-sounding voices. Add an API key to enable.',
      action: 'Add API Key',
      link: '/settings/providers',
      category: 'TTS',
    });
  }

  // Check for Stable Diffusion
  const sdAvailable = images.find((p) => p.name === 'StableDiffusion')?.available;
  if (!sdAvailable) {
    recommendations.push({
      priority: 'medium',
      title: 'Enable Local Image Generation',
      description:
        'Stable Diffusion WebUI enables local image generation without API costs. Install and configure to use.',
      action: 'Configure Stable Diffusion',
      link: '/settings/providers',
      category: 'Images',
    });
  }

  // Check if only RuleBased LLM is available
  const hasOnlyRuleBased =
    llm.filter((p) => p.available).length === 1 &&
    llm.find((p) => p.name === 'RuleBased' && p.available);
  if (hasOnlyRuleBased) {
    recommendations.push({
      priority: 'high',
      title: 'Configure an AI Provider',
      description:
        'You are currently using rule-based script generation. Configure OpenAI, Anthropic, or Ollama for AI-powered scripts.',
      action: 'Configure Provider',
      link: '/settings/providers',
      category: 'LLM',
    });
  }

  if (recommendations.length === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Lightbulb24Regular />
          <Title3>Provider Recommendations</Title3>
        </div>
        <div className={styles.emptyState}>
          <Checkmark24Regular style={{ fontSize: '32px', color: tokens.colorPaletteGreenForeground1 }} />
          <Text>All recommended providers are configured!</Text>
        </div>
      </div>
    );
  }

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'high':
        return 'danger';
      case 'medium':
        return 'warning';
      default:
        return 'informative';
    }
  };

  const handleAction = (link: string) => {
    if (link.startsWith('http')) {
      window.open(link, '_blank', 'noopener,noreferrer');
    } else {
      navigate(link);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Lightbulb24Regular />
        <Title3>Provider Recommendations</Title3>
      </div>
      <div className={styles.recommendationList}>
        {recommendations.map((rec, index) => (
          <div key={index} className={styles.recommendationCard}>
            <div className={styles.recommendationHeader}>
              <div className={styles.recommendationContent}>
                <Text weight="semibold">{rec.title}</Text>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  {rec.description}
                </Text>
              </div>
              <Badge
                className={styles.priorityBadge}
                appearance="filled"
                color={getPriorityColor(rec.priority)}
              >
                {rec.priority === 'high' ? 'High Priority' : rec.priority === 'medium' ? 'Medium' : 'Low'}
              </Badge>
            </div>
            <div className={styles.recommendationActions}>
              <Button
                appearance="primary"
                onClick={() => handleAction(rec.link)}
                icon={<ArrowRight24Regular />}
                iconPosition="after"
              >
                {rec.action}
              </Button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

