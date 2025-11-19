import { makeStyles, Button } from '@fluentui/react-components';
import { ArrowUpload24Regular, Video24Regular } from '@fluentui/react-icons';
import React from 'react';
import '../../styles/editor-design-tokens.css';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
    backgroundColor: '#000000',
    color: 'var(--color-text-primary)',
    padding: 'var(--space-2xl)',
    textAlign: 'center',
  },
  icon: {
    marginBottom: 'var(--space-xl)',
    color: 'var(--color-text-muted)',
    opacity: 0.6,
    '& > svg': {
      width: '64px',
      height: '64px',
    },
  },
  title: {
    fontSize: 'var(--font-size-xl)',
    fontWeight: 'var(--font-weight-medium)',
    color: 'var(--color-text-primary)',
    marginBottom: 'var(--space-md)',
  },
  subtitle: {
    fontSize: 'var(--font-size-md)',
    color: 'var(--color-text-secondary)',
    maxWidth: '400px',
    marginBottom: 'var(--space-xl)',
    lineHeight: 'var(--line-height-relaxed)',
  },
  button: {
    minHeight: 'var(--target-size-large)',
    fontSize: 'var(--font-size-md)',
    fontWeight: 'var(--font-weight-medium)',
  },
  dropZone: {
    border: '2px dashed var(--color-border-strong)',
    borderRadius: 'var(--radius-md)',
    padding: 'var(--space-2xl)',
  },
  dropZoneActive: {
    backgroundColor: '#3d82f619',
  },
});

export interface ViewerEmptyStateProps {
  /** Callback when import media button is clicked */
  onImportMedia?: () => void;
  /** Whether the user is currently dragging files over the viewer */
  isDraggingOver?: boolean;
  /** Custom title text */
  title?: string;
  /** Custom subtitle text */
  subtitle?: string;
  /** Whether to show the import button */
  showImportButton?: boolean;
}

/**
 * ViewerEmptyState Component
 *
 * Displays a proper empty state in the video viewer when no video is loaded.
 * Provides clear guidance and call-to-action for users to import media.
 *
 * Features:
 * - Clear title and descriptive subtitle
 * - Call-to-action button
 * - Visual feedback during drag-and-drop
 * - Professional, centered layout
 */
export function ViewerEmptyState({
  onImportMedia,
  isDraggingOver = false,
  title = 'No video loaded',
  subtitle = 'Import media from the left panel or drag files here to begin editing.',
  showImportButton = true,
}: ViewerEmptyStateProps) {
  const styles = useStyles();

  const containerClassName = isDraggingOver
    ? `${styles.container} ${styles.dropZone} ${styles.dropZoneActive}`
    : `${styles.container} ${styles.dropZone}`;

  return (
    <div className={containerClassName}>
      <div className={styles.icon}>
        {isDraggingOver ? <ArrowUpload24Regular /> : <Video24Regular />}
      </div>
      <h2 className={styles.title}>{title}</h2>
      <p className={styles.subtitle}>{subtitle}</p>
      {showImportButton && onImportMedia && (
        <Button
          appearance="primary"
          icon={<ArrowUpload24Regular />}
          onClick={onImportMedia}
          className={styles.button}
          size="large"
        >
          Import Media
        </Button>
      )}
    </div>
  );
}
