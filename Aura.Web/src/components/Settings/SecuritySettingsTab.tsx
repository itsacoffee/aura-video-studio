import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Card,
  Spinner,
  Badge,
  Divider,
} from '@fluentui/react-components';
import {
  ShieldCheckmark24Regular,
  LockClosed24Regular,
  Key24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { apiUrl } from '../../config/api';
import { ResetButton } from './ResetButton';

interface KeyVaultEncryptionInfo {
  platform: string;
  method: string;
  scope: string;
}

interface KeyVaultStorageInfo {
  location: string;
  encrypted: boolean;
  fileExists: boolean;
}

interface KeyVaultMetadata {
  configuredKeysCount: number;
  lastModified?: string;
}

interface KeyVaultInfoResponse {
  success: boolean;
  encryption: KeyVaultEncryptionInfo;
  storage: KeyVaultStorageInfo;
  metadata: KeyVaultMetadata;
  status: string;
}

interface KeyVaultDiagnosticsResponse {
  success: boolean;
  redactionCheckPassed: boolean;
  checks: string[];
  message?: string;
}

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  infoCard: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  infoRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  badge: {
    marginLeft: tokens.spacingHorizontalS,
  },
  detailsSection: {
    marginTop: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  checksList: {
    listStyleType: 'none',
    padding: 0,
    margin: 0,
    marginTop: tokens.spacingVerticalS,
  },
  checkItem: {
    padding: tokens.spacingVerticalXS,
    fontFamily: 'monospace',
    fontSize: '0.9em',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
    flexWrap: 'wrap',
  },
  iconContainer: {
    fontSize: '24px',
    marginRight: tokens.spacingHorizontalS,
    color: tokens.colorBrandForeground1,
  },
  statusBadge: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
});

export function SecuritySettingsTab() {
  const styles = useStyles();
  const [loading, setLoading] = useState(true);
  const [keyVaultInfo, setKeyVaultInfo] = useState<KeyVaultInfoResponse | null>(null);
  const [showDetails, setShowDetails] = useState(false);
  const [diagnostics, setDiagnostics] = useState<KeyVaultDiagnosticsResponse | null>(null);
  const [runningDiagnostics, setRunningDiagnostics] = useState(false);

  useEffect(() => {
    fetchKeyVaultInfo();
  }, []);

  const fetchKeyVaultInfo = async () => {
    setLoading(true);
    try {
      const response = await fetch(apiUrl('/api/keys/info'));
      if (response.ok) {
        const data = await response.json();
        setKeyVaultInfo(data);
      } else {
        console.error(
          `Failed to fetch KeyVault info: HTTP ${response.status} ${response.statusText}`
        );
      }
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      console.error('Error fetching KeyVault info:', errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const runDiagnostics = async () => {
    setRunningDiagnostics(true);
    setDiagnostics(null);
    try {
      const response = await fetch(apiUrl('/api/keys/diagnostics'), {
        method: 'POST',
      });
      if (response.ok) {
        const data = await response.json();
        setDiagnostics(data);
      } else {
        console.error(`Failed to run diagnostics: HTTP ${response.status} ${response.statusText}`);
      }
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      console.error('Error running diagnostics:', errorMessage);
    } finally {
      setRunningDiagnostics(false);
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'Never';
    const date = new Date(dateString);
    return date.toLocaleString();
  };

  if (loading) {
    return (
      <Card className={styles.section}>
        <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
          <Spinner size="medium" />
          <Text>Loading security information...</Text>
        </div>
      </Card>
    );
  }

  if (!keyVaultInfo) {
    return (
      <Card className={styles.section}>
        <Text role="alert" aria-live="polite">
          Failed to load security information. Please try again or check your connection.
        </Text>
        <Button
          appearance="primary"
          onClick={fetchKeyVaultInfo}
          style={{ marginTop: tokens.spacingVerticalM }}
        >
          Retry
        </Button>
      </Card>
    );
  }

  const isHealthy = keyVaultInfo.status === 'healthy';

  return (
    <Card className={styles.section}>
      <Title2>
        <span className={styles.iconContainer}>
          <ShieldCheckmark24Regular />
        </span>
        Security Status
      </Title2>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        Encryption and secure storage information for API keys and sensitive data
      </Text>

      <div className={styles.form}>
        <Card className={styles.infoCard}>
          <Text weight="semibold" size={300} style={{ marginBottom: tokens.spacingVerticalM }}>
            <span className={styles.iconContainer}>
              <LockClosed24Regular />
            </span>
            Encryption
          </Text>

          <div className={styles.infoRow}>
            <Text>Platform:</Text>
            <Text weight="semibold">{keyVaultInfo.encryption.platform}</Text>
          </div>

          <div className={styles.infoRow}>
            <Text>Encryption Method:</Text>
            <Text weight="semibold">{keyVaultInfo.encryption.method}</Text>
          </div>

          <div className={styles.infoRow}>
            <Text>Scope:</Text>
            <Text weight="semibold">{keyVaultInfo.encryption.scope}</Text>
          </div>

          <div className={styles.infoRow}>
            <Text>Storage Location:</Text>
            <Text weight="semibold" style={{ fontFamily: 'monospace', fontSize: '0.9em' }}>
              {keyVaultInfo.storage.location}
            </Text>
          </div>

          <div className={styles.infoRow}>
            <Text>Status:</Text>
            <div className={styles.statusBadge}>
              {keyVaultInfo.storage.encrypted && (
                <Badge appearance="filled" color="success" className={styles.badge}>
                  Encrypted
                </Badge>
              )}
              {keyVaultInfo.storage.fileExists && (
                <Badge appearance="filled" color="informative" className={styles.badge}>
                  Storage Active
                </Badge>
              )}
              {isHealthy ? (
                <Badge appearance="filled" color="success" className={styles.badge}>
                  Healthy
                </Badge>
              ) : (
                <Badge appearance="filled" color="subtle" className={styles.badge}>
                  Empty
                </Badge>
              )}
            </div>
          </div>
        </Card>

        <Card className={styles.infoCard}>
          <Text weight="semibold" size={300} style={{ marginBottom: tokens.spacingVerticalM }}>
            <span className={styles.iconContainer}>
              <Key24Regular />
            </span>
            Key Management
          </Text>

          <div className={styles.infoRow}>
            <Text>Configured API Keys:</Text>
            <Text weight="semibold">{keyVaultInfo.metadata.configuredKeysCount}</Text>
          </div>

          {keyVaultInfo.metadata.lastModified && (
            <div className={styles.infoRow}>
              <Text>Last Modified:</Text>
              <Text weight="semibold">{formatDate(keyVaultInfo.metadata.lastModified)}</Text>
            </div>
          )}
        </Card>

        <div className={styles.actions}>
          <Button
            appearance="secondary"
            icon={<Info24Regular />}
            onClick={() => setShowDetails(!showDetails)}
            aria-expanded={showDetails}
            aria-controls="security-details"
          >
            {showDetails ? 'Hide Details' : 'View Details'}
          </Button>
          <Button appearance="primary" onClick={runDiagnostics} disabled={runningDiagnostics}>
            {runningDiagnostics ? 'Running...' : 'Run Redaction Self-Check'}
          </Button>
        </div>

        {showDetails && (
          <div id="security-details" className={styles.detailsSection}>
            <Text weight="semibold" size={300} style={{ marginBottom: tokens.spacingVerticalS }}>
              Technical Details
            </Text>
            <Text
              size={200}
              style={{
                fontFamily: 'monospace',
                display: 'block',
                marginBottom: tokens.spacingVerticalXS,
              }}
            >
              Platform: {keyVaultInfo.encryption.platform}
            </Text>
            <Text
              size={200}
              style={{
                fontFamily: 'monospace',
                display: 'block',
                marginBottom: tokens.spacingVerticalXS,
              }}
            >
              Method: {keyVaultInfo.encryption.method}
            </Text>
            <Text
              size={200}
              style={{
                fontFamily: 'monospace',
                display: 'block',
                marginBottom: tokens.spacingVerticalXS,
              }}
            >
              Scope: {keyVaultInfo.encryption.scope}
            </Text>
            <Text
              size={200}
              style={{
                fontFamily: 'monospace',
                display: 'block',
                marginBottom: tokens.spacingVerticalXS,
              }}
            >
              File Exists: {keyVaultInfo.storage.fileExists ? 'Yes' : 'No'}
            </Text>
            <Text
              size={200}
              style={{
                fontFamily: 'monospace',
                display: 'block',
                marginBottom: tokens.spacingVerticalXS,
              }}
            >
              Keys Count: {keyVaultInfo.metadata.configuredKeysCount}
            </Text>
            <Text
              size={200}
              style={{
                fontFamily: 'monospace',
                display: 'block',
                marginBottom: tokens.spacingVerticalXS,
              }}
            >
              Last Modified: {formatDate(keyVaultInfo.metadata.lastModified)}
            </Text>
          </div>
        )}

        {diagnostics && (
          <Card className={styles.infoCard}>
            <Text weight="semibold" size={300} style={{ marginBottom: tokens.spacingVerticalM }}>
              Diagnostics Results
            </Text>

            <div className={styles.infoRow}>
              <Text>Redaction Check:</Text>
              <Badge
                appearance="filled"
                color={diagnostics.redactionCheckPassed ? 'success' : 'danger'}
                className={styles.badge}
              >
                {diagnostics.redactionCheckPassed ? 'PASSED' : 'FAILED'}
              </Badge>
            </div>

            {diagnostics.message && (
              <Text
                size={200}
                style={{
                  marginTop: tokens.spacingVerticalS,
                  marginBottom: tokens.spacingVerticalS,
                }}
              >
                {diagnostics.message}
              </Text>
            )}

            {diagnostics.checks.length > 0 && (
              <>
                <Text
                  weight="semibold"
                  size={200}
                  style={{
                    marginTop: tokens.spacingVerticalM,
                    marginBottom: tokens.spacingVerticalXS,
                  }}
                >
                  Checks:
                </Text>
                <ul className={styles.checksList}>
                  {diagnostics.checks.map((check, index) => (
                    <li key={index} className={styles.checkItem}>
                      <Text size={200}>{check}</Text>
                    </li>
                  ))}
                </ul>
              </>
            )}
          </Card>
        )}

        <Card
          style={{
            padding: tokens.spacingVerticalM,
            backgroundColor: tokens.colorNeutralBackground3,
            marginTop: tokens.spacingVerticalL,
          }}
        >
          <Text weight="semibold" size={300}>
            ℹ️ About Encryption
          </Text>
          <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
            <strong>Windows:</strong> Uses DPAPI (Data Protection API) with CurrentUser scope. Keys
            are protected by your Windows user account.
          </Text>
          <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
            <strong>Linux/macOS:</strong> Uses AES-256 encryption with a machine-specific key stored
            securely with 600 permissions.
          </Text>
          <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
            All API keys are encrypted at rest and never stored in plain text. The redaction
            self-check verifies that keys are properly masked in logs and API responses.
          </Text>
        </Card>

        <Divider
          style={{ marginTop: tokens.spacingVerticalXL, marginBottom: tokens.spacingVerticalL }}
        />

        <Card
          style={{
            padding: tokens.spacingVerticalL,
            backgroundColor: tokens.colorNeutralBackground2,
            marginTop: tokens.spacingVerticalL,
          }}
        >
          <Text weight="semibold" size={400} style={{ marginBottom: tokens.spacingVerticalS }}>
            ⚠️ Reset Application
          </Text>
          <Text size={200} style={{ marginBottom: tokens.spacingVerticalM }}>
            Completely reset the application to its initial state. This will clear all settings,
            cached data, API keys, and stored preferences. Use this if you&apos;re experiencing
            persistent issues or want to start fresh.
          </Text>
          <ResetButton />
        </Card>
      </div>
    </Card>
  );
}
