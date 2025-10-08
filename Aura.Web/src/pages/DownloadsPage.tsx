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
} from '@fluentui/react-components';
import { CloudArrowDown24Regular } from '@fluentui/react-icons';
import type { DownloadItem } from '../types';

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
  },
});

export function DownloadsPage() {
  const styles = useStyles();
  const [_manifest, _setManifest] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchManifest();
  }, []);

  const fetchManifest = async () => {
    try {
      const response = await fetch('/api/downloads/manifest');
      if (response.ok) {
        await response.json();
        // Manifest loaded successfully
      }
    } catch (error) {
      console.error('Error fetching manifest:', error);
    } finally {
      setLoading(false);
    }
  };

  const mockItems: DownloadItem[] = [
    {
      name: 'FFmpeg',
      version: '6.1',
      url: 'https://github.com/BtbN/FFmpeg-Builds/releases',
      sha256: 'abc123...',
      sizeBytes: 89000000,
      installPath: 'C:\\Aura\\ffmpeg\\bin',
      required: true,
    },
    {
      name: 'Ollama',
      version: '0.1.20',
      url: 'https://ollama.ai/download',
      sha256: 'def456...',
      sizeBytes: 450000000,
      installPath: 'C:\\Aura\\ollama',
      required: false,
    },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Download Center</Title1>
        <Text>Manage dependencies and resources</Text>
      </div>

      {loading ? (
        <Spinner label="Loading manifest..." />
      ) : (
        <Card>
          <Title2>Available Downloads</Title2>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>Name</TableHeaderCell>
                <TableHeaderCell>Version</TableHeaderCell>
                <TableHeaderCell>Size</TableHeaderCell>
                <TableHeaderCell>Status</TableHeaderCell>
                <TableHeaderCell>Actions</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {mockItems.map((item) => (
                <TableRow key={item.name}>
                  <TableCell>
                    <Text weight="semibold">{item.name}</Text>
                    {item.required && <Text> (Required)</Text>}
                  </TableCell>
                  <TableCell>{item.version}</TableCell>
                  <TableCell>{(item.sizeBytes / 1024 / 1024).toFixed(1)} MB</TableCell>
                  <TableCell>
                    <Text>Not installed</Text>
                  </TableCell>
                  <TableCell>
                    <Button
                      size="small"
                      icon={<CloudArrowDown24Regular />}
                    >
                      Install
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}
    </div>
  );
}
