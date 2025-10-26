/**
 * StatusFooter Component
 * Professional status bar showing project information and system stats
 */

import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Text,
  Button,
  Tooltip,
} from '@fluentui/react-components';
import {
  ChevronUp20Regular,
  ChevronDown20Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  footer: {
    position: 'fixed',
    bottom: 0,
    left: 0,
    right: 0,
    height: '24px',
    backgroundColor: 'var(--panel-header-bg, var(--color-surface))',
    borderTop: `1px solid var(--panel-border, ${tokens.colorNeutralStroke1})`,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '0 var(--space-2)',
    fontSize: '11px',
    color: 'var(--color-text-secondary)',
    zIndex: 900,
    userSelect: 'none',
    transition: 'transform var(--transition-panel)',
  },
  hidden: {
    transform: 'translateY(100%)',
  },
  leftSection: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-3)',
  },
  rightSection: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-3)',
  },
  infoItem: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-0)',
  },
  label: {
    color: 'var(--color-text-secondary)',
    fontWeight: 500,
  },
  value: {
    color: 'var(--color-text-primary)',
    fontWeight: 600,
  },
  separator: {
    width: '1px',
    height: '12px',
    backgroundColor: 'var(--panel-border, var(--color-border))',
  },
  toggleButton: {
    minWidth: 'auto',
    width: '20px',
    height: '20px',
    padding: 0,
    borderRadius: 'var(--border-radius-sm)',
  },
});

interface StatusFooterProps {
  projectName?: string | null;
  resolution?: string;
  frameRate?: number;
  timecode?: string;
  defaultVisible?: boolean;
}

export function StatusFooter({
  projectName,
  resolution = '1920x1080',
  frameRate = 30,
  timecode = '00:00:00:00',
  defaultVisible = true,
}: StatusFooterProps) {
  const styles = useStyles();
  const [visible, setVisible] = useState(defaultVisible);
  const [diskSpace, setDiskSpace] = useState<string>('--');

  // Load visibility preference from localStorage
  useEffect(() => {
    try {
      const saved = localStorage.getItem('aura-status-footer-visible');
      if (saved !== null) {
        setVisible(JSON.parse(saved));
      }
    } catch {
      // Use default
    }
  }, []);

  // Save visibility preference
  useEffect(() => {
    try {
      localStorage.setItem('aura-status-footer-visible', JSON.stringify(visible));
    } catch {
      // Ignore errors
    }
  }, [visible]);

  // Get disk space information (mocked for now)
  useEffect(() => {
    // In a real implementation, this would query the system
    // For now, we'll mock it
    const updateDiskSpace = () => {
      // Mock available disk space
      setDiskSpace('142.5 GB');
    };

    updateDiskSpace();
    const interval = setInterval(updateDiskSpace, 30000); // Update every 30s

    return () => clearInterval(interval);
  }, []);

  const toggleVisible = () => {
    setVisible(!visible);
  };

  return (
    <>
      <div className={`${styles.footer} ${!visible ? styles.hidden : ''}`}>
        <div className={styles.leftSection}>
          {projectName && (
            <>
              <div className={styles.infoItem}>
                <Text className={styles.label}>Project:&nbsp;</Text>
                <Text className={styles.value}>{projectName}</Text>
              </div>
              <div className={styles.separator} />
            </>
          )}
          
          <div className={styles.infoItem}>
            <Text className={styles.label}>Resolution:&nbsp;</Text>
            <Text className={styles.value}>{resolution}</Text>
          </div>
          
          <div className={styles.separator} />
          
          <div className={styles.infoItem}>
            <Text className={styles.label}>FPS:&nbsp;</Text>
            <Text className={styles.value}>{frameRate}</Text>
          </div>
          
          <div className={styles.separator} />
          
          <div className={styles.infoItem}>
            <Text className={styles.label}>Timecode:&nbsp;</Text>
            <Text className={styles.value}>{timecode}</Text>
          </div>
        </div>

        <div className={styles.rightSection}>
          <div className={styles.infoItem}>
            <Text className={styles.label}>Available:&nbsp;</Text>
            <Text className={styles.value}>{diskSpace}</Text>
          </div>
        </div>
      </div>

      {/* Toggle button - always visible */}
      <Tooltip
        content={visible ? 'Hide Status Bar' : 'Show Status Bar'}
        relationship="label"
      >
        <Button
          appearance="subtle"
          className={styles.toggleButton}
          icon={visible ? <ChevronDown20Regular /> : <ChevronUp20Regular />}
          onClick={toggleVisible}
          style={{
            position: 'fixed',
            bottom: visible ? '24px' : '0',
            right: 'var(--space-2)',
            zIndex: 901,
            transition: 'bottom var(--transition-panel)',
          }}
        />
      </Tooltip>
    </>
  );
}
