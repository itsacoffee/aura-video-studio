import {
  makeStyles,
  tokens,
  Text,
  Field,
  RadioGroup,
  Radio,
  Badge,
  Tooltip,
  Card,
} from '@fluentui/react-components';
import {
  Cloud20Regular,
  Desktop20Regular,
  Money20Regular,
  Clock20Regular,
  Info16Regular,
} from '@fluentui/react-icons';
import { ScriptProviders } from '../../state/providers';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  providerOption: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  providerInfo: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  providerHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  providerBadges: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  providerMeta: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalXXS,
  },
  metaItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  description: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  autoOption: {
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalS,
  },
});

export interface LlmProviderSelectorProps {
  value: string | undefined;
  onChange: (provider: string | undefined) => void;
  showAutoOption?: boolean;
}

export function LlmProviderSelector({
  value,
  onChange,
  showAutoOption = true,
}: LlmProviderSelectorProps) {
  const styles = useStyles();

  const formatCost = (cost: number | null): string => {
    if (cost === null) return 'Free';
    return `$${cost}/1K tokens`;
  };

  const formatLatency = (ms: number): string => {
    if (ms < 1000) return `~${ms}ms`;
    return `~${(ms / 1000).toFixed(1)}s`;
  };

  const getProviderIcon = (isLocal: boolean) => {
    return isLocal ? <Desktop20Regular /> : <Cloud20Regular />;
  };

  const getProviderTypeColor = (
    isLocal: boolean
  ): 'success' | 'warning' | 'danger' | 'important' | 'informative' | 'subtle' | 'brand' => {
    return isLocal ? 'success' : 'brand';
  };

  const getTierColor = (
    tier: string
  ): 'success' | 'warning' | 'danger' | 'important' | 'informative' | 'subtle' | 'brand' => {
    if (tier === 'Free') return 'success';
    if (tier === 'Pro') return 'warning';
    return 'important';
  };

  const getTierFromLabel = (label: string): string => {
    if (label.includes('Free')) return 'Free';
    if (label.includes('Pro')) return 'Pro';
    if (label.includes('Enterprise')) return 'Enterprise';
    return 'Standard';
  };

  return (
    <Field label="LLM Provider">
      <div className={styles.container}>
        {showAutoOption && (
          <Card className={styles.autoOption}>
            <Radio
              value="Auto"
              label={
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
                >
                  <Text weight="semibold">Auto (Recommended)</Text>
                  <Tooltip
                    content="Automatically selects the best available provider with fallback support"
                    relationship="label"
                  >
                    <Info16Regular />
                  </Tooltip>
                </div>
              }
              checked={!value || value === 'Auto'}
              onChange={() => onChange(undefined)}
            />
            <Text size={200} className={styles.description} style={{ marginLeft: '24px' }}>
              Uses Ollama → RuleBased fallback chain for free, reliable generation
            </Text>
          </Card>
        )}

        <RadioGroup value={value || 'Auto'} onChange={(_, data) => onChange(data.value)}>
          {ScriptProviders.map((provider) => {
            const tier = getTierFromLabel(provider.label);
            return (
              <div key={provider.value} className={styles.providerOption}>
                <Radio value={provider.value} />
                <div className={styles.providerInfo}>
                  <div className={styles.providerHeader}>
                    {getProviderIcon(provider.isLocal)}
                    <Text weight="semibold">{provider.label}</Text>
                    <div className={styles.providerBadges}>
                      <Badge color={getProviderTypeColor(provider.isLocal)} size="small">
                        {provider.isLocal ? 'Local' : 'Cloud'}
                      </Badge>
                      <Badge color={getTierColor(tier)} size="small">
                        {tier}
                      </Badge>
                    </div>
                  </div>
                  <Text size={200} className={styles.description}>
                    {provider.description}
                  </Text>
                  <div className={styles.providerMeta}>
                    <div className={styles.metaItem}>
                      <Money20Regular />
                      <Text>{formatCost(provider.cost)}</Text>
                    </div>
                    <div className={styles.metaItem}>
                      <Clock20Regular />
                      <Text>{formatLatency(provider.expectedLatency)}</Text>
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </RadioGroup>
      </div>
    </Field>
  );
}
