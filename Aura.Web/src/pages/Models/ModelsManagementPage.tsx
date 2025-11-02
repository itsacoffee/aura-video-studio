import {
  Button,
  Card,
  Text,
  Title1,
  Title2,
  makeStyles,
  tokens,
  Badge,
  Table,
  TableBody,
  TableCell,
  TableRow,
  TableHeader,
  TableHeaderCell,
} from '@fluentui/react-components';
import {
  Database24Regular,
  ArrowDownload24Regular,
  Checkmark24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { ErrorState, SkeletonCard } from '../../components/Loading';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1400px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  headerIcon: {
    fontSize: '32px',
    color: tokens.colorBrandForeground1,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalS,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  modelCard: {
    padding: tokens.spacingVerticalL,
  },
  modelHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
});

interface Model {
  id: string;
  name: string;
  provider: string;
  type: string;
  status: 'available' | 'downloading' | 'not-installed';
  size?: string;
  version?: string;
}

export const ModelsManagementPage: React.FC = () => {
  const styles = useStyles();
  const [models, setModels] = useState<Model[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadModels = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/models/list');
      if (!response.ok) {
        throw new Error('Failed to load models');
      }

      const data = await response.json();
      setModels(data.models || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadModels();
  }, [loadModels]);

  const handleDownload = useCallback(
    async (modelId: string) => {
      try {
        const response = await fetch(`/api/models/${modelId}/download`, {
          method: 'POST',
        });

        if (!response.ok) {
          throw new Error('Failed to start download');
        }

        await loadModels();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Download failed');
      }
    },
    [loadModels]
  );

  if (loading && models.length === 0) {
    return (
      <div className={styles.container}>
        <SkeletonCard />
        <SkeletonCard />
        <SkeletonCard />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Database24Regular className={styles.headerIcon} />
        <div>
          <Title1>AI Models Management</Title1>
          <Text className={styles.subtitle}>
            Manage AI models for text generation, image generation, and other features
          </Text>
        </div>
      </div>

      {error && <ErrorState message={error} />}

      <div className={styles.content}>
        <Card className={styles.modelCard}>
          <Title2>Available Models</Title2>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>Model Name</TableHeaderCell>
                <TableHeaderCell>Provider</TableHeaderCell>
                <TableHeaderCell>Type</TableHeaderCell>
                <TableHeaderCell>Status</TableHeaderCell>
                <TableHeaderCell>Actions</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {models.map((model) => (
                <TableRow key={model.id}>
                  <TableCell>{model.name}</TableCell>
                  <TableCell>{model.provider}</TableCell>
                  <TableCell>{model.type}</TableCell>
                  <TableCell>
                    {model.status === 'available' && (
                      <Badge appearance="filled" color="success">
                        Installed
                      </Badge>
                    )}
                    {model.status === 'downloading' && (
                      <Badge appearance="filled" color="informative">
                        Downloading...
                      </Badge>
                    )}
                    {model.status === 'not-installed' && (
                      <Badge appearance="outline">Not Installed</Badge>
                    )}
                  </TableCell>
                  <TableCell>
                    <div className={styles.actions}>
                      {model.status === 'not-installed' && (
                        <Button
                          size="small"
                          icon={<ArrowDownload24Regular />}
                          onClick={() => handleDownload(model.id)}
                        >
                          Download
                        </Button>
                      )}
                      {model.status === 'available' && (
                        <Button size="small" icon={<Checkmark24Regular />} disabled>
                          Installed
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      </div>
    </div>
  );
};

export default ModelsManagementPage;
