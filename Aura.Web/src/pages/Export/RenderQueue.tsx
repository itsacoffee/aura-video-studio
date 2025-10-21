import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Title1,
  Text,
  Button,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Badge,
  ProgressBar,
  Menu,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  Card,
  Body1,
  Body1Strong,
  Caption1,
  Divider,
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
} from '@fluentui/react-components';
import {
  Pause24Regular,
  Play24Regular,
  Delete24Regular,
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Clock24Regular,
  MoreVertical24Regular,
  ArrowRepeatAll24Regular,
  FolderOpen24Regular,
  ArrowExport24Regular,
  DocumentText24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    maxWidth: '1400px',
    margin: '0 auto',
    padding: tokens.spacingVerticalXXL,
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  headerActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  overallProgress: {
    marginBottom: tokens.spacingVerticalXL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  progressStats: {
    display: 'flex',
    gap: tokens.spacingHorizontalXXL,
    marginTop: tokens.spacingVerticalM,
  },
  stat: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  currentRender: {
    marginBottom: tokens.spacingVerticalXL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorBrandBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `2px solid ${tokens.colorBrandStroke1}`,
  },
  renderDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  detailRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  queueTable: {
    backgroundColor: tokens.colorNeutralBackground1,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXXL,
    color: tokens.colorNeutralForeground3,
  },
  statusBadge: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  thumbnail: {
    width: '80px',
    height: '45px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
  },
  logViewer: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: '12px',
    maxHeight: '200px',
    overflowY: 'auto',
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-all',
  },
});

interface QueueItem {
  id: string;
  projectName: string;
  presetName: string;
  status: 'queued' | 'rendering' | 'complete' | 'failed';
  progress: number;
  estimatedTimeRemaining: number;
  outputSize?: number;
  error?: string;
  createdAt: Date;
  startedAt?: Date;
  completedAt?: Date;
  renderTime?: number;
}

export function RenderQueue() {
  const styles = useStyles();
  const [isPaused, setIsPaused] = useState(false);
  const [queueItems, setQueueItems] = useState<QueueItem[]>([
    {
      id: '1',
      projectName: 'Marketing Video Q4',
      presetName: 'YouTube 1080p',
      status: 'rendering',
      progress: 45,
      estimatedTimeRemaining: 120,
      createdAt: new Date(),
      startedAt: new Date(),
    },
    {
      id: '2',
      projectName: 'Marketing Video Q4',
      presetName: 'Instagram Feed',
      status: 'queued',
      progress: 0,
      estimatedTimeRemaining: 180,
      createdAt: new Date(),
    },
    {
      id: '3',
      projectName: 'Product Demo',
      presetName: 'TikTok',
      status: 'complete',
      progress: 100,
      estimatedTimeRemaining: 0,
      outputSize: 45600000,
      createdAt: new Date(),
      completedAt: new Date(),
      renderTime: 156,
    },
  ]);

  const currentRender = queueItems.find((item) => item.status === 'rendering');
  const queuedCount = queueItems.filter((item) => item.status === 'queued').length;
  const completedCount = queueItems.filter((item) => item.status === 'complete').length;
  const failedCount = queueItems.filter((item) => item.status === 'failed').length;

  const overallProgress =
    queueItems.length > 0
      ? Math.round(
          (queueItems.filter((item) => item.status === 'complete').length / queueItems.length) * 100
        )
      : 0;

  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const formatFileSize = (bytes: number) => {
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  };

  const handlePauseResume = () => {
    setIsPaused(!isPaused);
  };

  const handleClearCompleted = () => {
    setQueueItems(queueItems.filter((item) => item.status !== 'complete'));
  };

  const handleClearAll = () => {
    if (confirm('Are you sure you want to clear all queue items?')) {
      setQueueItems([]);
    }
  };

  const handleRemoveItem = (id: string) => {
    setQueueItems(queueItems.filter((item) => item.id !== id));
  };

  const handleRetryItem = (id: string) => {
    setQueueItems(
      queueItems.map((item) =>
        item.id === id ? { ...item, status: 'queued' as const, progress: 0, error: undefined } : item
      )
    );
  };

  const renderStatusBadge = (status: QueueItem['status']) => {
    switch (status) {
      case 'queued':
        return (
          <Badge color="informative" className={styles.statusBadge}>
            <Clock24Regular fontSize="16px" />
            Queued
          </Badge>
        );
      case 'rendering':
        return (
          <Badge color="brand" className={styles.statusBadge}>
            <Play24Regular fontSize="16px" />
            Rendering
          </Badge>
        );
      case 'complete':
        return (
          <Badge color="success" className={styles.statusBadge}>
            <CheckmarkCircle24Regular fontSize="16px" />
            Complete
          </Badge>
        );
      case 'failed':
        return (
          <Badge color="danger" className={styles.statusBadge}>
            <ErrorCircle24Regular fontSize="16px" />
            Failed
          </Badge>
        );
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Render Queue</Title1>
        <Text>Manage your video export queue and monitor rendering progress</Text>
        <div className={styles.headerActions}>
          <Button
            appearance="secondary"
            icon={isPaused ? <Play24Regular /> : <Pause24Regular />}
            onClick={handlePauseResume}
          >
            {isPaused ? 'Resume Queue' : 'Pause Queue'}
          </Button>
          <Button appearance="secondary" onClick={handleClearCompleted}>
            Clear Completed
          </Button>
          <Button appearance="secondary" onClick={handleClearAll}>
            Clear All
          </Button>
        </div>
      </div>

      <Card className={styles.overallProgress}>
        <Body1Strong>Overall Queue Progress</Body1Strong>
        <Caption1>
          {completedCount} of {queueItems.length} exports complete, {overallProgress}% overall
        </Caption1>
        <ProgressBar value={overallProgress} max={100} style={{ marginTop: tokens.spacingVerticalM }} />
        <div className={styles.progressStats}>
          <div className={styles.stat}>
            <Caption1>Queued</Caption1>
            <Body1Strong>{queuedCount}</Body1Strong>
          </div>
          <div className={styles.stat}>
            <Caption1>Rendering</Caption1>
            <Body1Strong>{currentRender ? 1 : 0}</Body1Strong>
          </div>
          <div className={styles.stat}>
            <Caption1>Complete</Caption1>
            <Body1Strong>{completedCount}</Body1Strong>
          </div>
          <div className={styles.stat}>
            <Caption1>Failed</Caption1>
            <Body1Strong>{failedCount}</Body1Strong>
          </div>
        </div>
      </Card>

      {currentRender && (
        <Card className={styles.currentRender}>
          <Body1Strong>Currently Rendering</Body1Strong>
          <div className={styles.renderDetails}>
            <div className={styles.detailRow}>
              <Text>{currentRender.projectName}</Text>
              <Badge>{currentRender.presetName}</Badge>
            </div>
            <ProgressBar value={currentRender.progress} max={100} />
            <div className={styles.detailRow}>
              <Caption1>Progress: {currentRender.progress}%</Caption1>
              <Caption1>Time remaining: {formatTime(currentRender.estimatedTimeRemaining)}</Caption1>
            </div>

            <Accordion collapsible>
              <AccordionItem value="details">
                <AccordionHeader>Advanced Details</AccordionHeader>
                <AccordionPanel>
                  <Caption1>Scene 3/10: 45%</Caption1>
                  <Caption1>Encoding speed: 120 FPS (4.2x realtime)</Caption1>
                  <div className={styles.logViewer} style={{ marginTop: tokens.spacingVerticalM }}>
                    frame= 1234 fps=120 q=28.0 size= 12288kB time=00:00:41.13 bitrate=2446.2kbits/s speed=4.2x
                  </div>
                </AccordionPanel>
              </AccordionItem>
            </Accordion>
          </div>
        </Card>
      )}

      {queueItems.length === 0 ? (
        <div className={styles.emptyState}>
          <Body1>No items in queue</Body1>
          <Caption1>Add exports to the queue from the Export dialog</Caption1>
        </div>
      ) : (
        <Table className={styles.queueTable}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Thumbnail</TableHeaderCell>
              <TableHeaderCell>Project</TableHeaderCell>
              <TableHeaderCell>Preset</TableHeaderCell>
              <TableHeaderCell>Status</TableHeaderCell>
              <TableHeaderCell>Progress</TableHeaderCell>
              <TableHeaderCell>Time / Size</TableHeaderCell>
              <TableHeaderCell>Actions</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {queueItems.map((item) => (
              <TableRow key={item.id}>
                <TableCell>
                  <div className={styles.thumbnail} />
                </TableCell>
                <TableCell>{item.projectName}</TableCell>
                <TableCell>{item.presetName}</TableCell>
                <TableCell>{renderStatusBadge(item.status)}</TableCell>
                <TableCell>
                  {item.status === 'rendering' || item.status === 'queued' ? (
                    <ProgressBar value={item.progress} max={100} style={{ width: '100px' }} />
                  ) : (
                    <Caption1>-</Caption1>
                  )}
                </TableCell>
                <TableCell>
                  {item.status === 'rendering' && (
                    <Caption1>{formatTime(item.estimatedTimeRemaining)} remaining</Caption1>
                  )}
                  {item.status === 'complete' && item.outputSize && (
                    <Caption1>{formatFileSize(item.outputSize)}</Caption1>
                  )}
                  {item.status === 'failed' && <Caption1>-</Caption1>}
                  {item.status === 'queued' && <Caption1>Waiting...</Caption1>}
                </TableCell>
                <TableCell>
                  <div className={styles.actions}>
                    {item.status === 'complete' && (
                      <>
                        <Button
                          appearance="subtle"
                          size="small"
                          icon={<ArrowExport24Regular />}
                          title="Open File"
                        />
                        <Button
                          appearance="subtle"
                          size="small"
                          icon={<FolderOpen24Regular />}
                          title="Open Location"
                        />
                      </>
                    )}
                    {item.status === 'failed' && (
                      <Button
                        appearance="subtle"
                        size="small"
                        icon={<ArrowRepeatAll24Regular />}
                        onClick={() => handleRetryItem(item.id)}
                        title="Retry"
                      />
                    )}
                    <Menu>
                      <MenuTrigger disableButtonEnhancement>
                        <Button appearance="subtle" size="small" icon={<MoreVertical24Regular />} />
                      </MenuTrigger>
                      <MenuPopover>
                        <MenuList>
                          {item.status === 'failed' && (
                            <MenuItem icon={<DocumentText24Regular />}>Show Logs</MenuItem>
                          )}
                          <MenuItem
                            icon={<Delete24Regular />}
                            onClick={() => handleRemoveItem(item.id)}
                          >
                            Remove
                          </MenuItem>
                        </MenuList>
                      </MenuPopover>
                    </Menu>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
