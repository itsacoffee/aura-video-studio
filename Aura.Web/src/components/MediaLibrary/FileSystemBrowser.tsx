import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Text,
  Tree,
  TreeItem,
  TreeItemLayout,
  Spinner,
} from '@fluentui/react-components';
import {
  Folder24Regular,
  Document24Regular,
  VideoClip24Regular,
  MusicNote224Regular,
  Image24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
    overflow: 'hidden',
  },
  header: {
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingVerticalS,
  },
  treeItem: {
    cursor: 'pointer',
  },
  loadingContainer: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
  },
});

export interface FileSystemItem {
  id: string;
  name: string;
  path: string;
  type: 'folder' | 'file';
  mimeType?: string;
  size?: number;
  children?: FileSystemItem[];
}

interface FileSystemBrowserProps {
  onFileSelect?: (file: FileSystemItem) => void;
  onFileDragStart?: (file: FileSystemItem) => void;
}

const getFileIcon = (mimeType?: string) => {
  if (!mimeType) return <Document24Regular />;
  if (mimeType.startsWith('video/')) return <VideoClip24Regular />;
  if (mimeType.startsWith('audio/')) return <MusicNote224Regular />;
  if (mimeType.startsWith('image/')) return <Image24Regular />;
  return <Document24Regular />;
};

export const FileSystemBrowser: React.FC<FileSystemBrowserProps> = ({
  onFileSelect,
  onFileDragStart,
}) => {
  const styles = useStyles();
  const [items, setItems] = useState<FileSystemItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Simulate loading common folders
    // In a real implementation, this would interface with a file system API
    const commonFolders: FileSystemItem[] = [
      {
        id: 'desktop',
        name: 'Desktop',
        path: '/Desktop',
        type: 'folder',
      },
      {
        id: 'documents',
        name: 'Documents',
        path: '/Documents',
        type: 'folder',
      },
      {
        id: 'downloads',
        name: 'Downloads',
        path: '/Downloads',
        type: 'folder',
      },
      {
        id: 'videos',
        name: 'Videos',
        path: '/Videos',
        type: 'folder',
      },
    ];

    setTimeout(() => {
      setItems(commonFolders);
      setIsLoading(false);
    }, 500);
  }, []);

  const handleItemClick = (item: FileSystemItem) => {
    if (item.type === 'file') {
      onFileSelect?.(item);
    }
  };

  const handleDragStart = (e: React.DragEvent, item: FileSystemItem) => {
    if (item.type === 'file') {
      e.dataTransfer.effectAllowed = 'copy';
      e.dataTransfer.setData('application/json', JSON.stringify(item));
      onFileDragStart?.(item);
    }
  };

  const renderTreeItem = (item: FileSystemItem) => (
    <TreeItem
      key={item.id}
      itemType={item.type === 'folder' ? 'branch' : 'leaf'}
      value={item.id}
      className={styles.treeItem}
      draggable={item.type === 'file'}
      onDragStart={(e) => handleDragStart(e, item)}
      onClick={() => handleItemClick(item)}
    >
      <TreeItemLayout
        iconBefore={item.type === 'folder' ? <Folder24Regular /> : getFileIcon(item.mimeType)}
      >
        {item.name}
      </TreeItemLayout>
      {item.children && item.children.map(renderTreeItem)}
    </TreeItem>
  );

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text className={styles.title}>File Browser</Text>
      </div>
      <div className={styles.content}>
        {isLoading ? (
          <div className={styles.loadingContainer}>
            <Spinner size="medium" label="Loading folders..." />
          </div>
        ) : (
          <Tree aria-label="File system browser">
            {items.map(renderTreeItem)}
          </Tree>
        )}
      </div>
    </div>
  );
};
