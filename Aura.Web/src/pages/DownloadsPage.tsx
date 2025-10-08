import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Text,
  Button,
  Card,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Spinner,
  Badge,
} from '@fluentui/react-components';
import { 
  CloudArrowDown24Regular, 
  CheckmarkCircle24Filled,
  ErrorCircle24Filled,
} from '@fluentui/react-icons';

interface DependencyComponent {
  name: string;
  version: string;
  isRequired: boolean;
  files: Array<{
    filename: string;
    url: string;
    sha256: string;
    extractPath: string;
    sizeBytes: number;
  }>;
}

interface ComponentStatus {
  [key: string]: {
    isInstalled: boolean;
    isInstalling: boolean;
    error?: string;
  };
}

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalS,
  },
  card: {
    padding: tokens.spacingVerticalXL,
  },
  tableContainer: {
    marginTop: tokens.spacingVerticalL,
  },
  statusCell: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  nameCell: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
});

export function DownloadsPage() {
  const styles = useStyles();
  const [manifest, setManifest] = useState<DependencyComponent[]>([]);
  const [loading, setLoading] = useState(true);
  const [componentStatus, setComponentStatus] = useState<ComponentStatus>({});

  useEffect(() => {
    fetchManifest();
  }, []);

  const fetchManifest = async () => {
    try {
      const response = await fetch('/api/downloads/manifest');
      if (response.ok) {
        const data = await response.json();
        setManifest(data.components || []);
        
        // Check installation status for each component
        if (data.components) {
          for (const component of data.components) {
            checkComponentStatus(component.name);
          }
        }
      }
    } catch (error) {
      console.error('Error fetching manifest:', error);
    } finally {
      setLoading(false);
    }
  };

  const checkComponentStatus = async (componentName: string) => {
    try {
      const response = await fetch(`/api/downloads/${componentName}/status`);
      if (response.ok) {
        const data = await response.json();
        setComponentStatus(prev => ({
          ...prev,
          [componentName]: {
            isInstalled: data.isInstalled,
            isInstalling: false,
          },
        }));
      }
    } catch (error) {
      console.error(`Error checking status for ${componentName}:`, error);
    }
  };

  const installComponent = async (componentName: string) => {
    try {
      // Update status to installing
      setComponentStatus(prev => ({
        ...prev,
        [componentName]: {
          isInstalled: false,
          isInstalling: true,
        },
      }));

      const response = await fetch(`/api/downloads/${componentName}/install`, {
        method: 'POST',
      });

      if (response.ok) {
        // Update status to installed
        setComponentStatus(prev => ({
          ...prev,
          [componentName]: {
            isInstalled: true,
            isInstalling: false,
          },
        }));
      } else {
        const errorData = await response.json();
        setComponentStatus(prev => ({
          ...prev,
          [componentName]: {
            isInstalled: false,
            isInstalling: false,
            error: errorData.message || 'Installation failed',
          },
        }));
      }
    } catch (error) {
      console.error(`Error installing ${componentName}:`, error);
      setComponentStatus(prev => ({
        ...prev,
        [componentName]: {
          isInstalled: false,
          isInstalling: false,
          error: 'Network error',
        },
      }));
    }
  };

  const getStatusDisplay = (componentName: string) => {
    const status = componentStatus[componentName];
    
    if (!status) {
      return <Spinner size="tiny" />;
    }
    
    if (status.isInstalling) {
      return (
        <div className={styles.statusCell}>
          <Spinner size="tiny" />
          <Text>Installing...</Text>
        </div>
      );
    }
    
    if (status.error) {
      return (
        <div className={styles.statusCell}>
          <ErrorCircle24Filled color={tokens.colorPaletteRedForeground1} />
          <Text>{status.error}</Text>
        </div>
      );
    }
    
    if (status.isInstalled) {
      return (
        <div className={styles.statusCell}>
          <CheckmarkCircle24Filled color={tokens.colorPaletteGreenForeground1} />
          <Badge color="success" appearance="filled">Installed</Badge>
        </div>
      );
    }
    
    return (
      <Badge color="warning" appearance="outline">Not installed</Badge>
    );
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Download Center</Title1>
        <Text className={styles.subtitle}>
          Manage dependencies and external tools required for video production
        </Text>
      </div>

      <Card className={styles.card} style={{ marginBottom: tokens.spacingVerticalL, backgroundColor: tokens.colorNeutralBackground3 }}>
        <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, alignItems: 'flex-start' }}>
          <div style={{ flex: 1 }}>
            <Title2>Need to configure local AI tools?</Title2>
            <Text>
              After downloading components here, configure their paths and URLs in <strong>Settings → Local Providers</strong>.
              You can test connections and set custom paths for Stable Diffusion, Ollama, and FFmpeg.
            </Text>
          </div>
        </div>
      </Card>

      {loading ? (
        <Card className={styles.card}>
          <Spinner label="Loading dependencies..." />
        </Card>
      ) : (
        <Card className={styles.card}>
          <Title2>Available Components</Title2>
          <div className={styles.tableContainer}>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHeaderCell>Component</TableHeaderCell>
                  <TableHeaderCell>Version</TableHeaderCell>
                  <TableHeaderCell>Size</TableHeaderCell>
                  <TableHeaderCell>Status</TableHeaderCell>
                  <TableHeaderCell>Actions</TableHeaderCell>
                </TableRow>
              </TableHeader>
              <TableBody>
                {manifest.map((component) => {
                  const totalSize = component.files.reduce((sum, file) => sum + file.sizeBytes, 0);
                  const status = componentStatus[component.name];
                  
                  return (
                    <TableRow key={component.name}>
                      <TableCell>
                        <div className={styles.nameCell}>
                          <Text weight="semibold">{component.name}</Text>
                          {component.isRequired && (
                            <Badge color="danger" appearance="outline" size="small">
                              Required
                            </Badge>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Text>{component.version}</Text>
                      </TableCell>
                      <TableCell>
                        <Text>{(totalSize / 1024 / 1024).toFixed(1)} MB</Text>
                      </TableCell>
                      <TableCell>
                        {getStatusDisplay(component.name)}
                      </TableCell>
                      <TableCell>
                        {status?.isInstalled ? (
                          <Button
                            size="small"
                            appearance="subtle"
                            disabled
                          >
                            Installed
                          </Button>
                        ) : (
                          <Button
                            size="small"
                            appearance="primary"
                            icon={<CloudArrowDown24Regular />}
                            onClick={() => installComponent(component.name)}
                            disabled={status?.isInstalling}
                          >
                            {status?.isInstalling ? 'Installing...' : 'Install'}
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </div>
        </Card>
      )}
    </div>
  );
}
