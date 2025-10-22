import { useEffect, useState } from 'react';
import {
  makeStyles,
  tokens,
  Table,
  TableBody,
  TableCell,
  TableRow,
  TableHeader,
  TableHeaderCell,
  TableCellLayout,
  Badge,
  Tooltip,
  Text,
  Spinner,
} from '@fluentui/react-components';
import {
  CheckmarkCircle20Regular,
  DismissCircle20Regular,
  Info20Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    marginTop: tokens.spacingVerticalL,
  },
  table: {
    backgroundColor: tokens.colorNeutralBackground1,
  },
  reasonsList: {
    margin: 0,
    paddingLeft: tokens.spacingHorizontalM,
    fontSize: tokens.fontSizeBase200,
  },
  requirementText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  tooltipContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  infoIcon: {
    marginLeft: tokens.spacingHorizontalXS,
    cursor: 'help',
  },
});

interface ProviderCapability {
  name: string;
  available: boolean;
  reasonCodes: string[];
  requirements: {
    needsKey?: string[];
    needsGPU?: string;
    minVRAMMB?: number;
    os?: string[];
  };
}

interface ProviderRequirements {
  needsKey?: string[];
  needsGPU?: string;
  minVRAMMB?: number;
  os?: string[];
}

const reasonCodeDescriptions: Record<string, string> = {
  RequiresNvidiaGPU: 'This provider requires an NVIDIA GPU',
  'MissingApiKey:STABLE_KEY': 'STABLE_KEY API key is not configured. Add it in the API Keys tab.',
  InsufficientVRAM: 'GPU does not meet minimum VRAM requirement',
  UnsupportedOS: 'Operating system is not supported by this provider',
};

const getReasonCodeDescription = (code: string): string => {
  if (code in reasonCodeDescriptions) {
    return reasonCodeDescriptions[code];
  }
  // Handle dynamic keys like MissingApiKey:KEY_NAME
  if (code.startsWith('MissingApiKey:')) {
    const keyName = code.split(':')[1];
    return `${keyName} API key is not configured. Add it in the API Keys tab.`;
  }
  return code;
};

const formatRequirements = (requirements: ProviderRequirements): string => {
  const parts: string[] = [];

  if (requirements.needsKey && requirements.needsKey.length > 0) {
    parts.push(`API Keys: ${requirements.needsKey.join(', ')}`);
  }

  if (requirements.needsGPU) {
    parts.push(`GPU: ${requirements.needsGPU.toUpperCase()}`);
  }

  if (requirements.minVRAMMB) {
    const vramGB = (requirements.minVRAMMB / 1024).toFixed(0);
    parts.push(`Min VRAM: ${vramGB}GB`);
  }

  if (requirements.os && requirements.os.length > 0) {
    parts.push(
      `OS: ${requirements.os.map((o) => o.charAt(0).toUpperCase() + o.slice(1)).join(', ')}`
    );
  }

  return parts.join(' • ');
};

export function ProvidersTable() {
  const styles = useStyles();
  const [capabilities, setCapabilities] = useState<ProviderCapability[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchCapabilities();
  }, []);

  const fetchCapabilities = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/providers/capabilities');
      if (!response.ok) {
        throw new Error('Failed to fetch provider capabilities');
      }

      const data = await response.json();
      setCapabilities(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error occurred');
      console.error('Error fetching provider capabilities:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <Spinner label="Loading provider capabilities..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <Text style={{ color: tokens.colorPaletteRedForeground1 }}>Error: {error}</Text>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <Table className={styles.table} aria-label="Provider capabilities table">
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Provider</TableHeaderCell>
            <TableHeaderCell>Status</TableHeaderCell>
            <TableHeaderCell>Requirements</TableHeaderCell>
            <TableHeaderCell>Notes</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {capabilities.map((provider) => (
            <TableRow key={provider.name}>
              <TableCell>
                <TableCellLayout>
                  <Text weight="semibold">{provider.name}</Text>
                </TableCellLayout>
              </TableCell>
              <TableCell>
                <TableCellLayout>
                  {provider.available ? (
                    <Badge appearance="filled" color="success" icon={<CheckmarkCircle20Regular />}>
                      Available
                    </Badge>
                  ) : (
                    <Tooltip
                      content={
                        <div className={styles.tooltipContent}>
                          <Text weight="semibold">Unavailable reasons:</Text>
                          <ul className={styles.reasonsList}>
                            {provider.reasonCodes.map((code, idx) => (
                              <li key={idx}>{getReasonCodeDescription(code)}</li>
                            ))}
                          </ul>
                        </div>
                      }
                      relationship="description"
                    >
                      <Badge appearance="outline" color="danger" icon={<DismissCircle20Regular />}>
                        Unavailable
                      </Badge>
                    </Tooltip>
                  )}
                </TableCellLayout>
              </TableCell>
              <TableCell>
                <TableCellLayout>
                  <Text className={styles.requirementText}>
                    {formatRequirements(provider.requirements)}
                  </Text>
                </TableCellLayout>
              </TableCell>
              <TableCell>
                <TableCellLayout>
                  {!provider.available && provider.reasonCodes.length > 0 && (
                    <Tooltip
                      content={
                        <div className={styles.tooltipContent}>
                          <Text weight="semibold">How to fix:</Text>
                          <ul className={styles.reasonsList}>
                            {provider.reasonCodes.map((code, idx) => (
                              <li key={idx}>
                                {code.includes('ApiKey')
                                  ? 'Add the required API key in the API Keys tab above'
                                  : code.includes('GPU')
                                    ? 'Use a system with an NVIDIA GPU or select alternative providers'
                                    : code.includes('VRAM')
                                      ? 'Use a GPU with more VRAM or reduce quality settings'
                                      : 'Check system requirements'}
                              </li>
                            ))}
                          </ul>
                        </div>
                      }
                      relationship="label"
                    >
                      <Info20Regular className={styles.infoIcon} />
                    </Tooltip>
                  )}
                </TableCellLayout>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
