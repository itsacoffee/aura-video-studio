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
  Tooltip,
} from '@fluentui/react-components';
import {
  DocumentAdd24Regular,
  Delete24Regular,
  Info24Regular,
  DocumentText24Regular,
  Dismiss24Regular,
} from '@fluentui/react-icons';
import React, { useState, useCallback, useEffect } from 'react';
import type { FC } from 'react';
import { ragClient } from '../../api/ragClient';
import type { DocumentInfo, IndexStatistics } from '../../types';

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
  errorBanner: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteRedBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
});

const RagDocumentManager: FC = () => {
  const styles = useStyles();
  const [statistics, setStatistics] = useState<IndexStatistics | null>(null);
  const [documents, setDocuments] = useState<DocumentInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadDocuments = useCallback(async () => {
    try {
      const docs = await ragClient.getDocuments();
      setDocuments(docs);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load documents';
      setError(errorMessage);
    }
  }, []);

  const loadStatistics = useCallback(async () => {
    try {
      const stats = await ragClient.getStatistics();
      setStatistics(stats);
    } catch (err: unknown) {
      console.error('Failed to load statistics:', err);
    }
  }, []);

  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      await Promise.all([loadDocuments(), loadStatistics()]);
      setLoading(false);
    };
    loadData();
  }, [loadDocuments, loadStatistics]);

  const handleFileUpload = useCallback(
    async (event: React.ChangeEvent<HTMLInputElement>) => {
      const files = event.target.files;
      if (!files || files.length === 0) return;

      const file = files[0];

      try {
        setUploading(true);
        setError(null);

        await ragClient.uploadDocument(file, 'Semantic', 512);
        await loadDocuments();
        await loadStatistics();

        // Reset input so the same file can be uploaded again if needed
        event.target.value = '';
      } catch (err: unknown) {
        const errorMessage = err instanceof Error ? err.message : 'Upload failed';
        setError(errorMessage);
      } finally {
        setUploading(false);
      }
    },
    [loadDocuments, loadStatistics]
  );

  const handleDeleteDocument = useCallback(
    async (documentId: string) => {
      try {
        setLoading(true);
        await ragClient.deleteDocument(documentId);
        await loadDocuments();
        await loadStatistics();
      } catch (err: unknown) {
        const errorMessage = err instanceof Error ? err.message : 'Delete failed';
        setError(errorMessage);
      } finally {
        setLoading(false);
      }
    },
    [loadDocuments, loadStatistics]
  );

  const handleClearAll = useCallback(async () => {
    try {
      setLoading(true);
      await ragClient.clearAll();
      await loadDocuments();
      await loadStatistics();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Clear failed';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [loadDocuments, loadStatistics]);

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title3>RAG Document Manager</Title3>
        <Body1>Upload and manage documents for Retrieval-Augmented Generation</Body1>
      </div>

      {error && (
        <div className={styles.errorBanner}>
          <Text style={{ color: tokens.colorPaletteRedForeground1 }}>❌ {error}</Text>
          <Button
            appearance="subtle"
            icon={<Dismiss24Regular />}
            onClick={() => setError(null)}
            aria-label="Dismiss error"
          />
        </div>
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
        <Card className={styles.statCard}>
          <Body1>Last Updated</Body1>
          <Title3 style={{ fontSize: tokens.fontSizeBase300 }}>
            {statistics?.lastUpdated ? formatDate(statistics.lastUpdated) : 'Never'}
          </Title3>
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
                  <TableHeaderCell>Source</TableHeaderCell>
                  <TableHeaderCell>Title</TableHeaderCell>
                  <TableHeaderCell>Chunks</TableHeaderCell>
                  <TableHeaderCell>Created</TableHeaderCell>
                  <TableHeaderCell>Actions</TableHeaderCell>
                </TableRow>
              </TableHeader>
              <TableBody>
                {documents.map((doc) => (
                  <TableRow key={doc.documentId}>
                    <TableCell>
                      <TableCellLayout>{doc.source}</TableCellLayout>
                    </TableCell>
                    <TableCell>
                      <TableCellLayout>{doc.title || '—'}</TableCellLayout>
                    </TableCell>
                    <TableCell>{doc.chunkCount}</TableCell>
                    <TableCell>{formatDate(doc.createdAt)}</TableCell>
                    <TableCell>
                      <Button
                        appearance="subtle"
                        icon={<Delete24Regular />}
                        onClick={() => handleDeleteDocument(doc.documentId)}
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
