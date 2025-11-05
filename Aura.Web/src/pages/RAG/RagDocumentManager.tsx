import {
  Button,
  Card,
  Text,
  Title3,
  Body1,
  Spinner,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  TableCellLayout,
  tokens,
  makeStyles,
  Badge,
  Tooltip,
} from '@fluentui/react-components';
import {
  DocumentAdd24Regular,
  Delete24Regular,
  Info24Regular,
  DocumentText24Regular,
} from '@fluentui/react-icons';
import React, { useState, useCallback, useEffect } from 'react';
import type { FC } from 'react';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
  },
  uploadSection: {
    marginBottom: tokens.spacingVerticalXL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  uploadArea: {
    border: `2px dashed ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalXXL,
    textAlign: 'center',
    cursor: 'pointer',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground2Hover,
    },
  },
  statsSection: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalXL,
  },
  statCard: {
    padding: tokens.spacingVerticalL,
  },
  documentsSection: {
    marginTop: tokens.spacingVerticalXL,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXXL,
    color: tokens.colorNeutralForeground3,
  },
});

interface IndexStatistics {
  totalDocuments: number;
  totalChunks: number;
  totalSizeBytes: number;
  lastUpdated: string;
  documentsByFormat: Record<string, number>;
}

interface Document {
  id: string;
  filename: string;
  format: string;
  chunks: number;
  uploadedAt: string;
}

const RagDocumentManager: FC = () => {
  const styles = useStyles();
  const [statistics, setStatistics] = useState<IndexStatistics | null>(null);
  const [documents, setDocuments] = useState<Document[]>([]);
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchStatistics = useCallback(async () => {
    try {
      setLoading(true);
      const response = await fetch('/api/rag/statistics');
      if (!response.ok) {
        throw new Error('Failed to fetch statistics');
      }
      const data = await response.json();
      setStatistics(data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchStatistics();
  }, [fetchStatistics]);

  const handleFileUpload = useCallback(
    async (event: React.ChangeEvent<HTMLInputElement>) => {
      const files = event.target.files;
      if (!files || files.length === 0) return;

      const file = files[0];
      const formData = new FormData();
      formData.append('file', file);

      try {
        setUploading(true);
        setError(null);

        const response = await fetch('/api/rag/ingest?strategy=Semantic&maxChunkSize=512', {
          method: 'POST',
          body: formData,
        });

        if (!response.ok) {
          const errorData = await response.json();
          throw new Error(errorData.detail || 'Upload failed');
        }

        const result = await response.json();

        const newDocument: Document = {
          id: result.documentId,
          filename: file.name,
          format: file.name.split('.').pop()?.toUpperCase() || 'UNKNOWN',
          chunks: result.chunksCreated,
          uploadedAt: new Date().toISOString(),
        };

        setDocuments((prev) => [...prev, newDocument]);

        await fetchStatistics();
      } catch (err: unknown) {
        const errorMessage = err instanceof Error ? err.message : 'Upload failed';
        setError(errorMessage);
      } finally {
        setUploading(false);
      }
    },
    [fetchStatistics]
  );

  const handleDeleteDocument = useCallback(
    async (documentId: string) => {
      try {
        setLoading(true);
        const response = await fetch(`/api/rag/documents/${documentId}`, {
          method: 'DELETE',
        });

        if (!response.ok) {
          throw new Error('Failed to delete document');
        }

        setDocuments((prev) => prev.filter((doc) => doc.id !== documentId));
        await fetchStatistics();
      } catch (err: unknown) {
        const errorMessage = err instanceof Error ? err.message : 'Delete failed';
        setError(errorMessage);
      } finally {
        setLoading(false);
      }
    },
    [fetchStatistics]
  );

  const handleClearAll = useCallback(async () => {
    try {
      setLoading(true);
      const response = await fetch('/api/rag/clear', {
        method: 'DELETE',
      });

      if (!response.ok) {
        throw new Error('Failed to clear index');
      }

      setDocuments([]);
      await fetchStatistics();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Clear failed';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [fetchStatistics]);

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title3>RAG Document Manager</Title3>
        <Body1>Upload and manage documents for Retrieval-Augmented Generation</Body1>
      </div>

      {error && (
        <Card
          style={{
            marginBottom: tokens.spacingVerticalL,
            backgroundColor: tokens.colorPaletteRedBackground2,
          }}
        >
          <Text style={{ color: tokens.colorPaletteRedForeground1 }}>{error}</Text>
        </Card>
      )}

      <div className={styles.statsSection}>
        <Card className={styles.statCard}>
          <Body1>Total Documents</Body1>
          <Title3>{statistics?.totalDocuments ?? 0}</Title3>
        </Card>
        <Card className={styles.statCard}>
          <Body1>Total Chunks</Body1>
          <Title3>{statistics?.totalChunks ?? 0}</Title3>
        </Card>
        <Card className={styles.statCard}>
          <Body1>Index Size</Body1>
          <Title3>{statistics ? formatBytes(statistics.totalSizeBytes) : '0 Bytes'}</Title3>
        </Card>
      </div>

      <Card className={styles.uploadSection}>
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: tokens.spacingVerticalL,
          }}
        >
          <Text weight="semibold">Upload Document</Text>
          <Tooltip content="Supports PDF, DOCX, TXT, MD, HTML, JSON" relationship="label">
            <Info24Regular />
          </Tooltip>
        </div>
        <label className={styles.uploadArea}>
          <input
            type="file"
            accept=".pdf,.docx,.txt,.md,.html,.json"
            onChange={handleFileUpload}
            disabled={uploading}
            style={{ display: 'none' }}
          />
          {uploading ? (
            <Spinner label="Uploading and indexing..." />
          ) : (
            <>
              <DocumentAdd24Regular
                style={{ fontSize: '48px', marginBottom: tokens.spacingVerticalM }}
              />
              <Body1>Click to select a document or drag and drop</Body1>
              <Text size={200}>PDF, DOCX, TXT, MD, HTML, JSON (max 50MB)</Text>
            </>
          )}
        </label>
      </Card>

      <div className={styles.documentsSection}>
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: tokens.spacingVerticalL,
          }}
        >
          <Text weight="semibold" size={400}>
            Indexed Documents
          </Text>
          {documents.length > 0 && (
            <Dialog>
              <DialogTrigger disableButtonEnhancement>
                <Button appearance="secondary" icon={<Delete24Regular />}>
                  Clear All
                </Button>
              </DialogTrigger>
              <DialogSurface>
                <DialogBody>
                  <DialogTitle>Clear All Documents?</DialogTitle>
                  <DialogContent>
                    This will remove all documents from the RAG index. This action cannot be undone.
                  </DialogContent>
                  <DialogActions>
                    <DialogTrigger disableButtonEnhancement>
                      <Button appearance="secondary">Cancel</Button>
                    </DialogTrigger>
                    <Button appearance="primary" onClick={handleClearAll}>
                      Clear All
                    </Button>
                  </DialogActions>
                </DialogBody>
              </DialogSurface>
            </Dialog>
          )}
        </div>

        {loading && <Spinner label="Loading documents..." />}

        {!loading && documents.length === 0 && (
          <div className={styles.emptyState}>
            <DocumentText24Regular
              style={{ fontSize: '64px', marginBottom: tokens.spacingVerticalL }}
            />
            <Body1>No documents indexed yet</Body1>
            <Text size={200}>Upload documents to enable RAG for script generation</Text>
          </div>
        )}

        {!loading && documents.length > 0 && (
          <Card>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHeaderCell>Filename</TableHeaderCell>
                  <TableHeaderCell>Format</TableHeaderCell>
                  <TableHeaderCell>Chunks</TableHeaderCell>
                  <TableHeaderCell>Uploaded</TableHeaderCell>
                  <TableHeaderCell>Actions</TableHeaderCell>
                </TableRow>
              </TableHeader>
              <TableBody>
                {documents.map((doc) => (
                  <TableRow key={doc.id}>
                    <TableCell>
                      <TableCellLayout>{doc.filename}</TableCellLayout>
                    </TableCell>
                    <TableCell>
                      <Badge appearance="outline">{doc.format}</Badge>
                    </TableCell>
                    <TableCell>{doc.chunks}</TableCell>
                    <TableCell>{new Date(doc.uploadedAt).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <Button
                        appearance="subtle"
                        icon={<Delete24Regular />}
                        onClick={() => handleDeleteDocument(doc.id)}
                      >
                        Delete
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Card>
        )}
      </div>
    </div>
  );
};

export default RagDocumentManager;
