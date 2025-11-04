import { Card, Button, Text, Badge, makeStyles, tokens, Divider } from '@fluentui/react-components';
import {
  ArrowDownload24Regular,
  Warning24Regular,
  Checkmark24Regular,
  Document24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';

const useStyles = makeStyles({
  panel: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  summaryGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingVerticalM,
  },
  summaryItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  licenseTypesList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  licenseTypeItem: {
    display: 'flex',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
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
  exportButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  statusBadge: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
});

interface LicenseTypeCount {
  licenseType: string;
  count: number;
}

interface SourceCount {
  source: string;
  count: number;
}

interface LicensingSummary {
  totalScenes: number;
  scenesWithSelection: number;
  commercialUseAllowed: boolean;
  requiresAttribution: boolean;
  licenseTypes: LicenseTypeCount[];
  sources: SourceCount[];
  warnings: string[];
}

interface LicensingInfoPanelProps {
  summary?: LicensingSummary;
  onExportCsv: () => void;
  onExportJson: () => void;
  onExportAttribution: () => void;
  isLoading?: boolean;
}

const LicensingInfoPanel: FC<LicensingInfoPanelProps> = ({
  summary,
  onExportCsv,
  onExportJson,
  onExportAttribution,
  isLoading = false,
}) => {
  const styles = useStyles();

  if (!summary) {
    return (
      <Card className={styles.panel}>
        <Text size={400} weight="semibold">
          Licensing Information
        </Text>
        <Text size={300}>No licensing information available yet.</Text>
      </Card>
    );
  }

  return (
    <Card className={styles.panel}>
      <div className={styles.header}>
        <Text size={500} weight="semibold">
          Licensing Summary
        </Text>
        <div className={styles.statusBadge}>
          {summary.commercialUseAllowed ? (
            <>
              <Checkmark24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
              <Badge appearance="filled" color="success">
                Commercial OK
              </Badge>
            </>
          ) : (
            <>
              <Warning24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
              <Badge appearance="filled" color="danger">
                Commercial Restricted
              </Badge>
            </>
          )}
        </div>
      </div>

      <Divider />

      <div className={styles.summaryGrid}>
        <div className={styles.summaryItem}>
          <Text size={300} weight="semibold">
            Total Scenes
          </Text>
          <Text size={400}>{summary.totalScenes}</Text>
        </div>
        <div className={styles.summaryItem}>
          <Text size={300} weight="semibold">
            Scenes with Selection
          </Text>
          <Text size={400}>{summary.scenesWithSelection}</Text>
        </div>
        <div className={styles.summaryItem}>
          <Text size={300} weight="semibold">
            Commercial Use
          </Text>
          <Text size={400}>{summary.commercialUseAllowed ? 'Allowed' : 'Restricted'}</Text>
        </div>
        <div className={styles.summaryItem}>
          <Text size={300} weight="semibold">
            Attribution Required
          </Text>
          <Text size={400}>{summary.requiresAttribution ? 'Yes' : 'No'}</Text>
        </div>
      </div>

      {summary.licenseTypes.length > 0 && (
        <>
          <Divider />
          <div>
            <Text size={400} weight="semibold" style={{ marginBottom: tokens.spacingVerticalS }}>
              License Types
            </Text>
            <div className={styles.licenseTypesList}>
              {summary.licenseTypes.map((licenseType) => (
                <div key={licenseType.licenseType} className={styles.licenseTypeItem}>
                  <Text size={300}>{licenseType.licenseType}</Text>
                  <Badge>
                    {licenseType.count} scene{licenseType.count > 1 ? 's' : ''}
                  </Badge>
                </div>
              ))}
            </div>
          </div>
        </>
      )}

      {summary.sources.length > 0 && (
        <>
          <Divider />
          <div>
            <Text size={400} weight="semibold" style={{ marginBottom: tokens.spacingVerticalS }}>
              Image Sources
            </Text>
            <div className={styles.licenseTypesList}>
              {summary.sources.map((source) => (
                <div key={source.source} className={styles.licenseTypeItem}>
                  <Text size={300}>{source.source}</Text>
                  <Badge>
                    {source.count} scene{source.count > 1 ? 's' : ''}
                  </Badge>
                </div>
              ))}
            </div>
          </div>
        </>
      )}

      {summary.warnings.length > 0 && (
        <>
          <Divider />
          <div>
            <Text size={400} weight="semibold" style={{ marginBottom: tokens.spacingVerticalS }}>
              Warnings
            </Text>
            <div className={styles.warningsList}>
              {summary.warnings.map((warning, index) => (
                <div key={index} className={styles.warningItem}>
                  <Warning24Regular style={{ flexShrink: 0 }} />
                  <Text size={300}>{warning}</Text>
                </div>
              ))}
            </div>
          </div>
        </>
      )}

      <Divider />

      <div>
        <Text size={400} weight="semibold" style={{ marginBottom: tokens.spacingVerticalS }}>
          Export Licensing Information
        </Text>
        <div className={styles.exportButtons}>
          <Button
            appearance="secondary"
            icon={<ArrowDownload24Regular />}
            onClick={onExportCsv}
            disabled={isLoading}
          >
            Export CSV
          </Button>
          <Button
            appearance="secondary"
            icon={<ArrowDownload24Regular />}
            onClick={onExportJson}
            disabled={isLoading}
          >
            Export JSON
          </Button>
          <Button
            appearance="secondary"
            icon={<Document24Regular />}
            onClick={onExportAttribution}
            disabled={isLoading}
          >
            Attribution Text
          </Button>
        </div>
      </div>
    </Card>
  );
};

export default LicensingInfoPanel;
