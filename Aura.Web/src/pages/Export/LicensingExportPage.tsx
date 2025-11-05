import { useState, useEffect, useCallback } from 'react';
import {
  Card,
  Button,
  Text,
  Badge,
  Divider,
  Checkbox,
  Textarea,
  Spinner,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import {
  ArrowDownload24Regular,
  Warning24Regular,
  Checkmark24Regular,
  Document24Regular,
  Shield24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import type {
  ProjectLicensingManifest,
  LicensingSignOffRequest,
} from '../../types/licensing';
import {
  getLicensingManifest,
  exportLicensingManifest,
  recordLicensingSignOff,
  downloadManifestFile,
} from '../../services/api/licensingApi';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  statusBadge: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  summaryGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  summaryItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  assetsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    maxHeight: '400px',
    overflowY: 'auto',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  assetItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
  },
  warningsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  warningItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorPaletteYellowBorder1}`,
  },
  signOffSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  checkboxGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  exportButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
  loadingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalXXL,
  },
});

interface LicensingExportPageProps {
  projectId: string;
}

const LicensingExportPage: FC<LicensingExportPageProps> = ({ projectId }) => {
  const styles = useStyles();
  const [manifest, setManifest] = useState<ProjectLicensingManifest | null>(null);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);
  const [signedOff, setSignedOff] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [acknowledgedCommercial, setAcknowledgedCommercial] = useState(false);
  const [acknowledgedAttribution, setAcknowledgedAttribution] = useState(false);
  const [acknowledgedWarnings, setAcknowledgedWarnings] = useState(false);
  const [signOffNotes, setSignOffNotes] = useState('');

  useEffect(() => {
    loadManifest();
  }, [projectId]);

  const loadManifest = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getLicensingManifest(projectId);
      setManifest(data);
    } catch (err) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setError(`Failed to load licensing manifest: ${errorObj.message}`);
    } finally {
      setLoading(false);
    }
  }, [projectId]);

  const handleExport = useCallback(
    async (format: 'json' | 'csv' | 'html' | 'text') => {
      if (!manifest) return;

      try {
        setExporting(true);
        setError(null);
        const result = await exportLicensingManifest({
          projectId: manifest.projectId,
          format,
        });

        downloadManifestFile(result.content, result.filename, result.contentType);
      } catch (err) {
        const errorObj = err instanceof Error ? err : new Error(String(err));
        setError(`Failed to export manifest: ${errorObj.message}`);
      } finally {
        setExporting(false);
      }
    },
    [manifest]
  );

  const handleSignOff = useCallback(async () => {
    if (!manifest) return;

    try {
      setExporting(true);
      setError(null);

      const request: LicensingSignOffRequest = {
        projectId: manifest.projectId,
        acknowledgedCommercialRestrictions: acknowledgedCommercial,
        acknowledgedAttributionRequirements: acknowledgedAttribution,
        acknowledgedWarnings: acknowledgedWarnings,
        notes: signOffNotes || undefined,
      };

      await recordLicensingSignOff(request);
      setSignedOff(true);
    } catch (err) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setError(`Failed to record sign-off: ${errorObj.message}`);
    } finally {
      setExporting(false);
    }
  }, [
    manifest,
    acknowledgedCommercial,
    acknowledgedAttribution,
    acknowledgedWarnings,
    signOffNotes,
  ]);

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner size="large" label="Loading licensing information..." />
      </div>
    );
  }

  if (error && !manifest) {
    return (
      <div className={styles.container}>
        <Card>
          <div className={styles.warningItem}>
            <Warning24Regular />
            <Text>{error}</Text>
          </div>
          <Button onClick={loadManifest}>Retry</Button>
        </Card>
      </div>
    );
  }

  if (!manifest) {
    return null;
  }

  const canSignOff =
    acknowledgedCommercial && acknowledgedAttribution && acknowledgedWarnings;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text size={900} weight="bold">
          Licensing & Provenance Export
        </Text>
        <Text size={400}>Project: {manifest.projectName}</Text>
        <div className={styles.statusBadge}>
          {manifest.allCommercialUseAllowed ? (
            <>
              <Checkmark24Regular
                style={{ color: tokens.colorPaletteGreenForeground1 }}
              />
              <Badge appearance="filled" color="success">
                Commercial Use Allowed
              </Badge>
            </>
          ) : (
            <>
              <Warning24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
              <Badge appearance="filled" color="danger">
                Commercial Restrictions Apply
              </Badge>
            </>
          )}
        </div>
      </div>

      <Card>
        <Text size={500} weight="semibold">
          Summary
        </Text>
        <Divider />
        <div className={styles.summaryGrid}>
          <div className={styles.summaryItem}>
            <Text size={300} weight="semibold">
              Total Assets
            </Text>
            <Text size={600}>{manifest.summary.totalAssets}</Text>
          </div>
          <div className={styles.summaryItem}>
            <Text size={300} weight="semibold">
              Require Attribution
            </Text>
            <Text size={600}>{manifest.summary.assetsRequiringAttribution}</Text>
          </div>
          <div className={styles.summaryItem}>
            <Text size={300} weight="semibold">
              Commercial Restrictions
            </Text>
            <Text size={600}>{manifest.summary.assetsWithCommercialRestrictions}</Text>
          </div>
        </div>
      </Card>

      {manifest.warnings.length > 0 && (
        <Card>
          <Text size={500} weight="semibold">
            Warnings
          </Text>
          <Divider />
          <div className={styles.warningsList}>
            {manifest.warnings.map((warning, index) => (
              <div key={index} className={styles.warningItem}>
                <Warning24Regular style={{ flexShrink: 0 }} />
                <Text size={300}>{warning}</Text>
              </div>
            ))}
          </div>
        </Card>
      )}

      <Card>
        <Text size={500} weight="semibold">
          Assets ({manifest.assets.length})
        </Text>
        <Divider />
        <div className={styles.assetsList}>
          {manifest.assets.map((asset) => (
            <div key={asset.assetId} className={styles.assetItem}>
              <div>
                <Text size={400} weight="semibold">
                  {asset.name}
                </Text>
                <Text size={300}>
                  {asset.assetType} • Scene {asset.sceneIndex} • {asset.source}
                </Text>
                {asset.attributionRequired && (
                  <Text size={200} style={{ color: tokens.colorPaletteBlueForeground1 }}>
                    Attribution: {asset.attributionText || 'Required'}
                  </Text>
                )}
              </div>
              <div>
                {asset.commercialUseAllowed ? (
                  <Badge appearance="tint" color="success">
                    Commercial OK
                  </Badge>
                ) : (
                  <Badge appearance="tint" color="danger">
                    Restricted
                  </Badge>
                )}
              </div>
            </div>
          ))}
        </div>
      </Card>

      <Card>
        <div className={styles.signOffSection}>
          <div>
            <Shield24Regular style={{ fontSize: '32px', marginBottom: tokens.spacingVerticalS }} />
            <Text size={500} weight="semibold">
              Pre-Export Sign-Off
            </Text>
            <Text size={300}>
              Please review and acknowledge the licensing requirements before exporting.
            </Text>
          </div>
          <Divider />
          <div className={styles.checkboxGroup}>
            <Checkbox
              label="I acknowledge that some assets have commercial use restrictions"
              checked={acknowledgedCommercial}
              onChange={(_, data) => setAcknowledgedCommercial(data.checked === true)}
              disabled={signedOff}
            />
            <Checkbox
              label="I acknowledge the attribution requirements for assets"
              checked={acknowledgedAttribution}
              onChange={(_, data) => setAcknowledgedAttribution(data.checked === true)}
              disabled={signedOff}
            />
            <Checkbox
              label="I have reviewed all warnings and understand the implications"
              checked={acknowledgedWarnings}
              onChange={(_, data) => setAcknowledgedWarnings(data.checked === true)}
              disabled={signedOff}
            />
          </div>
          <Textarea
            label="Additional Notes (Optional)"
            placeholder="Enter any additional notes about licensing compliance..."
            value={signOffNotes}
            onChange={(_, data) => setSignOffNotes(data.value)}
            disabled={signedOff}
            rows={3}
          />
          <Button
            appearance="primary"
            icon={<Shield24Regular />}
            onClick={handleSignOff}
            disabled={!canSignOff || signedOff || exporting}
          >
            {signedOff ? 'Signed Off' : 'Record Sign-Off'}
          </Button>
          {signedOff && (
            <div className={styles.statusBadge}>
              <Checkmark24Regular
                style={{ color: tokens.colorPaletteGreenForeground1 }}
              />
              <Text size={300} style={{ color: tokens.colorPaletteGreenForeground1 }}>
                Sign-off recorded successfully
              </Text>
            </div>
          )}
        </div>
      </Card>

      <Card>
        <Text size={500} weight="semibold">
          Export Licensing Information
        </Text>
        <Divider />
        <div className={styles.exportButtons}>
          <Button
            appearance="secondary"
            icon={<ArrowDownload24Regular />}
            onClick={() => handleExport('json')}
            disabled={exporting}
          >
            Export JSON
          </Button>
          <Button
            appearance="secondary"
            icon={<ArrowDownload24Regular />}
            onClick={() => handleExport('csv')}
            disabled={exporting}
          >
            Export CSV
          </Button>
          <Button
            appearance="secondary"
            icon={<ArrowDownload24Regular />}
            onClick={() => handleExport('html')}
            disabled={exporting}
          >
            Export HTML
          </Button>
          <Button
            appearance="secondary"
            icon={<Document24Regular />}
            onClick={() => handleExport('text')}
            disabled={exporting}
          >
            Export Text
          </Button>
        </div>
        {error && (
          <div className={styles.warningItem}>
            <Warning24Regular />
            <Text>{error}</Text>
          </div>
        )}
      </Card>
    </div>
  );
};

export default LicensingExportPage;
