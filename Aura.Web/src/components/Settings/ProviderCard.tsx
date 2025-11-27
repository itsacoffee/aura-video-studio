/**
 * ProviderCard - Card component for displaying AI provider information
 *
 * Displays provider details including name, type, status, and test results.
 * Supports right-click context menu for provider management actions.
 */

import {
  Card,
  CardHeader,
  Text,
  Badge,
  Spinner,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { CheckmarkCircle20Regular, ErrorCircle20Regular } from '@fluentui/react-icons';
import type { FC, MouseEvent } from 'react';

const useStyles = makeStyles({
  providerCard: {
    marginBottom: tokens.spacingVerticalM,
    cursor: 'context-menu',
    transition: 'background-color 0.2s ease',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  providerHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    width: '100%',
  },
  providerName: {
    fontWeight: 600,
  },
  providerBadges: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
  providerInfo: {
    marginTop: tokens.spacingVerticalS,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  testResult: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalS,
  },
  successIcon: {
    color: tokens.colorPaletteGreenForeground1,
  },
  errorIcon: {
    color: tokens.colorPaletteRedForeground1,
  },
});

export interface AIProvider {
  id: string;
  name: string;
  type: 'llm' | 'tts' | 'image';
  isDefault: boolean;
  isEnabled: boolean;
  model?: string;
  hasFallback?: boolean;
}

export interface TestResult {
  success: boolean;
  message: string;
  latency: number;
  timestamp: number;
}

interface ProviderCardProps {
  provider: AIProvider;
  isTesting: boolean;
  testResult?: TestResult;
  onContextMenu: (e: MouseEvent, provider: AIProvider) => void;
}

const getProviderTypeLabel = (type: string): string => {
  switch (type) {
    case 'llm':
      return 'Language Model';
    case 'tts':
      return 'Text-to-Speech';
    case 'image':
      return 'Image Generation';
    default:
      return type;
  }
};

export const ProviderCard: FC<ProviderCardProps> = ({
  provider,
  isTesting,
  testResult,
  onContextMenu,
}) => {
  const styles = useStyles();

  const handleContextMenu = (e: MouseEvent<HTMLDivElement>) => {
    e.preventDefault();
    onContextMenu(e, provider);
  };

  const getTestResultIcon = () => {
    if (!testResult) return null;
    if (testResult.success) {
      return <CheckmarkCircle20Regular className={styles.successIcon} />;
    }
    return <ErrorCircle20Regular className={styles.errorIcon} />;
  };

  return (
    <Card className={styles.providerCard} onContextMenu={handleContextMenu}>
      <CardHeader
        header={
          <div className={styles.providerHeader}>
            <Text className={styles.providerName}>{provider.name}</Text>
            <div className={styles.providerBadges}>
              {isTesting && <Spinner size="tiny" />}
              {provider.isDefault && <Badge color="success">Default</Badge>}
              {!provider.isEnabled && <Badge color="warning">Disabled</Badge>}
              <Badge color="informative">{getProviderTypeLabel(provider.type)}</Badge>
            </div>
          </div>
        }
      />
      <div className={styles.providerInfo}>
        {provider.model && <Text size={200}>Model: {provider.model}</Text>}
        {testResult && (
          <div className={styles.testResult}>
            {getTestResultIcon()}
            <Text size={200}>
              {testResult.success
                ? `Connected (${testResult.latency}ms)`
                : `Failed: ${testResult.message}`}
            </Text>
          </div>
        )}
      </div>
    </Card>
  );
};

export default ProviderCard;
