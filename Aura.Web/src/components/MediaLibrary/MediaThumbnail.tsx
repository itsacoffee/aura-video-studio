import {
  makeStyles,
  tokens,
  Text,
  Card,
  CardPreview,
  CardHeader,
  Button,
  Menu,
  MenuTrigger,
  MenuList,
  MenuItem,
  MenuPopover,
} from '@fluentui/react-components';
import {
  VideoClip24Regular,
  MusicNote224Regular,
  Image24Regular,
  MoreVertical24Regular,
  Folder24Regular,
  Delete24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  card: {
    cursor: 'grab',
    position: 'relative',
    '&:hover': {
      boxShadow: tokens.shadow8,
    },
    '&:active': {
      cursor: 'grabbing',
    },
  },
  preview: {
    height: '80px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: tokens.colorNeutralBackground4,
    fontSize: '32px',
    position: 'relative',
    overflow: 'hidden',
  },
  thumbnailImage: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  header: {
    padding: tokens.spacingVerticalXS,
  },
  name: {
    fontSize: tokens.fontSizeBase200,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  metadata: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    marginTop: '2px',
  },
  menuButton: {
    position: 'absolute',
    top: '4px',
    right: '4px',
    minWidth: '24px',
    minHeight: '24px',
    padding: '4px',
    backgroundColor: tokens.colorNeutralBackground1,
    opacity: 0,
    transition: 'opacity 0.2s ease',
  },
  cardHover: {
    '&:hover $menuButton': {
      opacity: 1,
    },
  },
});

interface MediaThumbnailProps {
  id: string;
  name: string;
  type: 'video' | 'audio' | 'image';
  preview?: string;
  duration?: number;
  fileSize?: number;
  onDragStart?: (e: React.DragEvent) => void;
  onDragEnd?: () => void;
  onRemove?: () => void;
  onRevealInFinder?: () => void;
}

const getMediaIcon = (type: 'video' | 'audio' | 'image') => {
  switch (type) {
    case 'video':
      return <VideoClip24Regular />;
    case 'audio':
      return <MusicNote224Regular />;
    case 'image':
      return <Image24Regular />;
  }
};

const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
};

export const MediaThumbnail: React.FC<MediaThumbnailProps> = ({
  name,
  type,
  preview,
  duration,
  fileSize,
  onDragStart,
  onDragEnd,
  onRemove,
  onRevealInFinder,
}) => {
  const styles = useStyles();

  return (
    <Menu>
      <Card
        className={styles.card}
        draggable
        onDragStart={onDragStart}
        onDragEnd={onDragEnd}
      >
        <MenuTrigger disableButtonEnhancement>
          <Button
            className={styles.menuButton}
            appearance="subtle"
            size="small"
            icon={<MoreVertical24Regular />}
            aria-label="More options"
          />
        </MenuTrigger>
        <CardPreview className={styles.preview}>
          {preview ? (
            <img src={preview} alt={name} className={styles.thumbnailImage} />
          ) : (
            getMediaIcon(type)
          )}
        </CardPreview>
        <CardHeader
          className={styles.header}
          header={
            <div>
              <Text className={styles.name} title={name}>
                {name}
              </Text>
              <Text className={styles.metadata}>{type}</Text>
              {duration && (
                <Text className={styles.metadata}>{Math.floor(duration)}s</Text>
              )}
              {fileSize && (
                <Text className={styles.metadata}>{formatFileSize(fileSize)}</Text>
              )}
            </div>
          }
        />
      </Card>
      <MenuPopover>
        <MenuList>
          {onRevealInFinder && (
            <MenuItem icon={<Folder24Regular />} onClick={onRevealInFinder}>
              Reveal in Finder
            </MenuItem>
          )}
          {onRemove && (
            <MenuItem icon={<Delete24Regular />} onClick={onRemove}>
              Remove
            </MenuItem>
          )}
        </MenuList>
      </MenuPopover>
    </Menu>
  );
};
