import { ReactNode, useState, useEffect } from 'react';
import { makeStyles, tokens } from '@fluentui/react-components';
import { MenuBar } from '../MenuBar/MenuBar';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    overflow: 'hidden',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  content: {
    display: 'flex',
    flex: 1,
    overflow: 'hidden',
  },
  mainArea: {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    overflow: 'hidden',
  },
  previewPanel: {
    flex: 6,
    minHeight: '300px',
    display: 'flex',
    flexDirection: 'column',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3,
    overflow: 'hidden',
  },
  timelinePanel: {
    flex: 4,
    minHeight: '200px',
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground2,
    overflow: 'hidden',
  },
  propertiesPanel: {
    width: '320px',
    minWidth: '280px',
    maxWidth: '400px',
    borderLeft: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground2,
    overflow: 'auto',
    display: 'flex',
    flexDirection: 'column',
  },
  mediaLibraryPanel: {
    width: '280px',
    minWidth: '240px',
    maxWidth: '350px',
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground2,
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
  },
  resizer: {
    width: '4px',
    cursor: 'ew-resize',
    backgroundColor: 'transparent',
    position: 'relative',
    '&:hover': {
      backgroundColor: tokens.colorBrandBackground,
    },
    '&:active': {
      backgroundColor: tokens.colorBrandBackground,
    },
    '&:focus': {
      outline: `2px solid ${tokens.colorBrandBackground}`,
      outlineOffset: '2px',
    },
  },
  horizontalResizer: {
    height: '4px',
    cursor: 'ns-resize',
    backgroundColor: 'transparent',
    position: 'relative',
    '&:hover': {
      backgroundColor: tokens.colorBrandBackground,
    },
    '&:active': {
      backgroundColor: tokens.colorBrandBackground,
    },
    '&:focus': {
      outline: `2px solid ${tokens.colorBrandBackground}`,
      outlineOffset: '2px',
    },
  },
});

interface EditorLayoutProps {
  preview?: ReactNode;
  timeline?: ReactNode;
  properties?: ReactNode;
  mediaLibrary?: ReactNode;
  onImportMedia?: () => void;
  onExportVideo?: () => void;
  onShowKeyboardShortcuts?: () => void;
}

// LocalStorage keys for panel sizes
const STORAGE_KEYS = {
  propertiesWidth: 'editor-properties-width',
  mediaLibraryWidth: 'editor-media-library-width',
  previewHeight: 'editor-preview-height',
};

// Helper to load from localStorage with default fallback
const loadPanelSize = (key: string, defaultValue: number): number => {
  try {
    const stored = localStorage.getItem(key);
    return stored ? parseFloat(stored) : defaultValue;
  } catch {
    return defaultValue;
  }
};

// Helper to save to localStorage
const savePanelSize = (key: string, value: number): void => {
  try {
    localStorage.setItem(key, value.toString());
  } catch {
    // Ignore localStorage errors
  }
};

export function EditorLayout({
  preview,
  timeline,
  properties,
  mediaLibrary,
  onImportMedia,
  onExportVideo,
  onShowKeyboardShortcuts,
}: EditorLayoutProps) {
  const styles = useStyles();
  const [propertiesWidth, setPropertiesWidth] = useState(() => 
    loadPanelSize(STORAGE_KEYS.propertiesWidth, 320)
  );
  const [mediaLibraryWidth, setMediaLibraryWidth] = useState(() =>
    loadPanelSize(STORAGE_KEYS.mediaLibraryWidth, 280)
  );
  const [previewHeight, setPreviewHeight] = useState(() =>
    loadPanelSize(STORAGE_KEYS.previewHeight, 60)
  ); // Percentage

  // Persist panel sizes to localStorage
  useEffect(() => {
    savePanelSize(STORAGE_KEYS.propertiesWidth, propertiesWidth);
  }, [propertiesWidth]);

  useEffect(() => {
    savePanelSize(STORAGE_KEYS.mediaLibraryWidth, mediaLibraryWidth);
  }, [mediaLibraryWidth]);

  useEffect(() => {
    savePanelSize(STORAGE_KEYS.previewHeight, previewHeight);
  }, [previewHeight]);

  // Handle resizing properties panel
  const handlePropertiesResize = (e: React.MouseEvent) => {
    const startX = e.clientX;
    const startWidth = propertiesWidth;

    const handleMouseMove = (moveEvent: MouseEvent) => {
      const delta = startX - moveEvent.clientX;
      const newWidth = Math.max(280, Math.min(400, startWidth + delta));
      setPropertiesWidth(newWidth);
    };

    const handleMouseUp = () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  };

  // Handle resizing preview panel
  const handlePreviewResize = (e: React.MouseEvent) => {
    const container = (e.target as HTMLElement).parentElement?.parentElement;
    if (!container) return;

    const startY = e.clientY;
    const containerHeight = container.clientHeight;
    const startHeight = previewHeight;

    const handleMouseMove = (moveEvent: MouseEvent) => {
      const delta = moveEvent.clientY - startY;
      const deltaPercent = (delta / containerHeight) * 100;
      const newHeight = Math.max(40, Math.min(80, startHeight + deltaPercent));
      setPreviewHeight(newHeight);
    };

    const handleMouseUp = () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  };

  // Handle resizing media library panel
  const handleMediaLibraryResize = (e: React.MouseEvent) => {
    const startX = e.clientX;
    const startWidth = mediaLibraryWidth;

    const handleMouseMove = (moveEvent: MouseEvent) => {
      const delta = moveEvent.clientX - startX;
      const newWidth = Math.max(240, Math.min(350, startWidth + delta));
      setMediaLibraryWidth(newWidth);
    };

    const handleMouseUp = () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  };

  return (
    <div className={styles.container}>
      <MenuBar
        onImportMedia={onImportMedia}
        onExportVideo={onExportVideo}
        onShowKeyboardShortcuts={onShowKeyboardShortcuts}
      />
      <div className={styles.content}>
        {mediaLibrary && (
          <>
            <div className={styles.mediaLibraryPanel} style={{ width: `${mediaLibraryWidth}px` }}>
              {mediaLibrary}
            </div>
            {/* Interactive resizer - intentionally uses mouse and keyboard events */}
            {/* eslint-disable-next-line jsx-a11y/no-noninteractive-element-interactions */}
            <div 
              className={styles.resizer} 
              onMouseDown={handleMediaLibraryResize} 
              role="separator" 
              aria-orientation="vertical"
              aria-label="Resize media library panel"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === 'ArrowLeft') {
                  e.preventDefault();
                  setMediaLibraryWidth((prev) => Math.max(240, prev - 10));
                  savePanelSize(STORAGE_KEYS.mediaLibraryWidth, Math.max(240, mediaLibraryWidth - 10));
                } else if (e.key === 'ArrowRight') {
                  e.preventDefault();
                  setMediaLibraryWidth((prev) => Math.min(350, prev + 10));
                  savePanelSize(STORAGE_KEYS.mediaLibraryWidth, Math.min(350, mediaLibraryWidth + 10));
                }
              }}
            />
          </>
        )}
        <div className={styles.mainArea}>
          <div className={styles.previewPanel} style={{ flex: previewHeight }}>
            {preview}
          </div>
          {/* Interactive resizer - intentionally uses mouse and keyboard events */}
          {/* eslint-disable-next-line jsx-a11y/no-noninteractive-element-interactions */}
          <div 
            className={styles.horizontalResizer} 
            onMouseDown={handlePreviewResize} 
            role="separator" 
            aria-orientation="horizontal"
            aria-label="Resize preview panel"
            tabIndex={0}
            onKeyDown={(e) => {
              if (e.key === 'ArrowUp') {
                e.preventDefault();
                setPreviewHeight((prev) => Math.min(80, prev + 5));
                savePanelSize(STORAGE_KEYS.previewHeight, Math.min(80, previewHeight + 5));
              } else if (e.key === 'ArrowDown') {
                e.preventDefault();
                setPreviewHeight((prev) => Math.max(40, prev - 5));
                savePanelSize(STORAGE_KEYS.previewHeight, Math.max(40, previewHeight - 5));
              }
            }}
          />
          <div className={styles.timelinePanel} style={{ flex: 100 - previewHeight }}>
            {timeline}
          </div>
        </div>
        {properties && (
          <>
            {/* Interactive resizer - intentionally uses mouse and keyboard events */}
            {/* eslint-disable-next-line jsx-a11y/no-noninteractive-element-interactions */}
            <div 
              className={styles.resizer} 
              onMouseDown={handlePropertiesResize} 
              role="separator" 
              aria-orientation="vertical"
              aria-label="Resize properties panel"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === 'ArrowLeft') {
                  e.preventDefault();
                  setPropertiesWidth((prev) => Math.min(400, prev + 10));
                  savePanelSize(STORAGE_KEYS.propertiesWidth, Math.min(400, propertiesWidth + 10));
                } else if (e.key === 'ArrowRight') {
                  e.preventDefault();
                  setPropertiesWidth((prev) => Math.max(280, prev - 10));
                  savePanelSize(STORAGE_KEYS.propertiesWidth, Math.max(280, propertiesWidth - 10));
                }
              }}
            />
            <div className={styles.propertiesPanel} style={{ width: `${propertiesWidth}px` }}>
              {properties}
            </div>
          </>
        )}
      </div>
    </div>
  );
}
