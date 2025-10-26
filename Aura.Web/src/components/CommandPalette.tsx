import {
  makeStyles,
  tokens,
  Dialog,
  DialogSurface,
  DialogBody,
  Input,
  Text,
  mergeClasses,
} from '@fluentui/react-components';
import {
  Search24Regular,
  Settings24Regular,
  VideoClip24Regular,
  Play24Regular,
  Share24Regular,
  CloudArrowDown24Regular,
  Document24Regular,
  Folder24Regular,
  Timeline24Regular,
  Image24Regular,
  Lightbulb24Regular,
  HeartPulse24Regular,
  Keyboard24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';

const useStyles = makeStyles({
  surface: {
    maxWidth: '600px',
    width: '90vw',
    maxHeight: '70vh',
    display: 'flex',
    flexDirection: 'column',
  },
  searchBox: {
    padding: tokens.spacingVerticalL,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  results: {
    flex: 1,
    overflowY: 'auto',
    padding: 0,
  },
  commandItem: {
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalL}`,
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    transition: 'background-color 0.2s',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3,
    },
  },
  selectedItem: {
    backgroundColor: tokens.colorNeutralBackground3,
  },
  icon: {
    fontSize: '20px',
    color: tokens.colorNeutralForeground2,
  },
  commandContent: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  commandName: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
  },
  commandDescription: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  shortcut: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    fontFamily: 'monospace',
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalXS}`,
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusSmall,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
});

interface Command {
  id: string;
  name: string;
  description: string;
  category: string;
  icon?: React.ComponentType;
  shortcut?: string;
  action: () => void;
}

interface CommandPaletteProps {
  isOpen: boolean;
  onClose: () => void;
}

export function CommandPalette({ isOpen, onClose }: CommandPaletteProps) {
  const styles = useStyles();
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedIndex, setSelectedIndex] = useState(0);

  // Define all available commands
  const allCommands: Command[] = [
    // Navigation
    {
      id: 'nav-create',
      name: 'Create Video',
      description: 'Start creating a new video',
      category: 'Navigation',
      icon: VideoClip24Regular,
      action: () => navigate('/create'),
    },
    {
      id: 'nav-projects',
      name: 'Projects',
      description: 'View all projects',
      category: 'Navigation',
      icon: Folder24Regular,
      action: () => navigate('/projects'),
    },
    {
      id: 'nav-timeline',
      name: 'Timeline Editor',
      description: 'Open timeline editor',
      category: 'Navigation',
      icon: Timeline24Regular,
      action: () => navigate('/timeline'),
    },
    {
      id: 'nav-render',
      name: 'Render & Export',
      description: 'Configure and render videos',
      category: 'Navigation',
      icon: Play24Regular,
      action: () => navigate('/render'),
    },
    {
      id: 'nav-publish',
      name: 'Publish',
      description: 'Publish to platforms',
      category: 'Navigation',
      icon: Share24Regular,
      action: () => navigate('/publish'),
    },
    {
      id: 'nav-assets',
      name: 'Asset Library',
      description: 'Manage video assets',
      category: 'Navigation',
      icon: Image24Regular,
      action: () => navigate('/assets'),
    },
    {
      id: 'nav-downloads',
      name: 'Program Dependencies',
      description: 'Manage program dependencies and engines',
      category: 'Navigation',
      icon: CloudArrowDown24Regular,
      action: () => navigate('/downloads'),
    },
    {
      id: 'nav-health',
      name: 'Provider Health',
      description: 'Check provider status',
      category: 'Navigation',
      icon: HeartPulse24Regular,
      action: () => navigate('/health'),
    },
    {
      id: 'nav-ideation',
      name: 'Ideation',
      description: 'Generate video ideas',
      category: 'Navigation',
      icon: Lightbulb24Regular,
      action: () => navigate('/ideation'),
    },
    {
      id: 'nav-settings',
      name: 'Settings',
      description: 'Open settings',
      category: 'Navigation',
      icon: Settings24Regular,
      shortcut: 'Ctrl+,',
      action: () => navigate('/settings'),
    },

    // Settings
    {
      id: 'settings-output',
      name: 'Output Settings',
      description: 'Configure video output parameters',
      category: 'Settings',
      icon: Settings24Regular,
      action: () => {
        navigate('/settings');
        setTimeout(() => {
          const tab = document.querySelector('[value="output"]') as HTMLElement;
          tab?.click();
        }, 100);
      },
    },
    {
      id: 'settings-performance',
      name: 'Performance Settings',
      description: 'Optimize rendering performance',
      category: 'Settings',
      icon: Settings24Regular,
      action: () => {
        navigate('/settings');
        setTimeout(() => {
          const tab = document.querySelector('[value="performance"]') as HTMLElement;
          tab?.click();
        }, 100);
      },
    },
    {
      id: 'settings-shortcuts',
      name: 'Keyboard Shortcuts',
      description: 'Customize keyboard shortcuts',
      category: 'Settings',
      icon: Keyboard24Regular,
      action: () => {
        navigate('/settings');
        setTimeout(() => {
          const tab = document.querySelector('[value="shortcuts"]') as HTMLElement;
          tab?.click();
        }, 100);
      },
    },
    {
      id: 'settings-providers',
      name: 'Provider Settings',
      description: 'Configure AI providers',
      category: 'Settings',
      icon: Settings24Regular,
      action: () => {
        navigate('/settings');
        setTimeout(() => {
          const tab = document.querySelector('[value="providers"]') as HTMLElement;
          tab?.click();
        }, 100);
      },
    },
    {
      id: 'settings-apikeys',
      name: 'API Keys',
      description: 'Manage API keys',
      category: 'Settings',
      icon: Settings24Regular,
      action: () => {
        navigate('/settings');
        setTimeout(() => {
          const tab = document.querySelector('[value="apikeys"]') as HTMLElement;
          tab?.click();
        }, 100);
      },
    },

    // Actions
    {
      id: 'action-new-project',
      name: 'New Project',
      description: 'Create a new video project',
      category: 'Actions',
      shortcut: 'Ctrl+N',
      action: () => navigate('/create'),
    },
    {
      id: 'action-open-project',
      name: 'Open Project',
      description: 'Open an existing project',
      category: 'Actions',
      shortcut: 'Ctrl+O',
      action: () => navigate('/projects'),
    },
    {
      id: 'action-export',
      name: 'Export Video',
      description: 'Render and export current video',
      category: 'Actions',
      shortcut: 'Ctrl+M',
      action: () => navigate('/render'),
    },
    {
      id: 'action-validate-providers',
      name: 'Validate Providers',
      description: 'Test all provider connections',
      category: 'Actions',
      action: async () => {
        await fetch('/api/providers/validate', { method: 'POST' });
        alert('Provider validation complete');
      },
    },
    {
      id: 'action-run-benchmark',
      name: 'Run Benchmark',
      description: 'Test system performance',
      category: 'Actions',
      action: async () => {
        await fetch('/api/hardware/benchmark', { method: 'POST' });
      },
    },
    {
      id: 'action-clear-cache',
      name: 'Clear Cache',
      description: 'Clear preview and render cache',
      category: 'Actions',
      action: async () => {
        if (confirm('Clear all cache?')) {
          await fetch('/api/cache/clear', { method: 'POST' });
          alert('Cache cleared');
        }
      },
    },

    // Quick Generators
    {
      id: 'quick-youtube',
      name: 'Quick YouTube Video',
      description: 'Generate a video optimized for YouTube',
      category: 'Quick Actions',
      action: () => {
        navigate('/create'); /* Preset selection not yet implemented */
      },
    },
    {
      id: 'quick-shorts',
      name: 'Quick YouTube Shorts',
      description: 'Generate a vertical short video',
      category: 'Quick Actions',
      action: () => {
        navigate('/create'); /* Preset selection not yet implemented */
      },
    },
    {
      id: 'quick-social',
      name: 'Quick Social Media Post',
      description: 'Generate a square social media video',
      category: 'Quick Actions',
      action: () => {
        navigate('/create'); /* Preset selection not yet implemented */
      },
    },
  ];

  // Filter commands based on search query
  const filteredCommands = allCommands.filter((command) => {
    const query = searchQuery.toLowerCase();
    return (
      command.name.toLowerCase().includes(query) ||
      command.description.toLowerCase().includes(query) ||
      command.category.toLowerCase().includes(query)
    );
  });

  // Reset selected index when search query changes
  useEffect(() => {
    setSelectedIndex(0);
  }, [searchQuery]);

  // Handle keyboard navigation
  const handleKeyDown = useCallback(
    (e: KeyboardEvent) => {
      if (!isOpen) return;

      switch (e.key) {
        case 'ArrowDown':
          e.preventDefault();
          setSelectedIndex((prev) => Math.min(prev + 1, filteredCommands.length - 1));
          break;
        case 'ArrowUp':
          e.preventDefault();
          setSelectedIndex((prev) => Math.max(prev - 1, 0));
          break;
        case 'Enter':
          e.preventDefault();
          if (filteredCommands[selectedIndex]) {
            filteredCommands[selectedIndex].action();
            onClose();
            setSearchQuery('');
          }
          break;
        case 'Escape':
          e.preventDefault();
          onClose();
          setSearchQuery('');
          break;
      }
    },
    [isOpen, selectedIndex, filteredCommands, onClose]
  );

  useEffect(() => {
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [handleKeyDown]);

  // Clear search when dialog closes
  useEffect(() => {
    if (!isOpen) {
      setSearchQuery('');
      setSelectedIndex(0);
    }
  }, [isOpen]);

  return (
    <Dialog open={isOpen} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.surface}>
        <DialogBody>
          <div className={styles.searchBox}>
            <Input
              appearance="filled-lighter"
              size="large"
              placeholder="Type a command or search..."
              contentBefore={<Search24Regular />}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              // autoFocus is intentional for command palette UX
              // eslint-disable-next-line jsx-a11y/no-autofocus
              autoFocus
            />
          </div>
          <div className={styles.results}>
            {filteredCommands.length === 0 ? (
              <div className={styles.emptyState}>
                <Text>No commands found for &quot;{searchQuery}&quot;</Text>
              </div>
            ) : (
              filteredCommands.map((command, index) => {
                const Icon = command.icon || Document24Regular;
                return (
                  <div
                    key={command.id}
                    className={mergeClasses(
                      styles.commandItem,
                      index === selectedIndex && styles.selectedItem
                    )}
                    role="button"
                    tabIndex={0}
                    onClick={() => {
                      command.action();
                      onClose();
                      setSearchQuery('');
                    }}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ' ') {
                        e.preventDefault();
                        command.action();
                        onClose();
                        setSearchQuery('');
                      }
                    }}
                    onMouseEnter={() => setSelectedIndex(index)}
                  >
                    <Icon className={styles.icon} />
                    <div className={styles.commandContent}>
                      <Text className={styles.commandName}>{command.name}</Text>
                      <Text className={styles.commandDescription}>{command.description}</Text>
                    </div>
                    {command.shortcut && (
                      <Text className={styles.shortcut}>{command.shortcut}</Text>
                    )}
                  </div>
                );
              })
            )}
          </div>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
