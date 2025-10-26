import { makeStyles, tokens, Text, Button, Badge } from '@fluentui/react-components';
import {
  VideoClip24Regular,
  MusicNote224Regular,
  Image24Regular,
  Document24Regular,
  Stack24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase200,
    marginBottom: tokens.spacingVerticalXS,
    color: tokens.colorNeutralForeground2,
  },
  collectionButton: {
    justifyContent: 'flex-start',
    width: '100%',
  },
  collectionContent: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    flex: 1,
  },
  collectionName: {
    flex: 1,
    textAlign: 'left',
  },
});

export interface MediaCollection {
  id: string;
  name: string;
  type: 'all' | 'video' | 'audio' | 'image' | 'graphics';
  count: number;
}

interface SmartCollectionsProps {
  collections: MediaCollection[];
  selectedCollectionId?: string;
  onCollectionSelect?: (collectionId: string) => void;
}

const getCollectionIcon = (type: string) => {
  switch (type) {
    case 'video':
      return <VideoClip24Regular />;
    case 'audio':
      return <MusicNote224Regular />;
    case 'image':
      return <Image24Regular />;
    case 'graphics':
      return <Document24Regular />;
    case 'all':
    default:
      return <Stack24Regular />;
  }
};

export const SmartCollections: React.FC<SmartCollectionsProps> = ({
  collections,
  selectedCollectionId,
  onCollectionSelect,
}) => {
  const styles = useStyles();

  return (
    <div className={styles.container}>
      <Text className={styles.title}>Smart Collections</Text>
      {collections.map((collection) => (
        <Button
          key={collection.id}
          appearance={selectedCollectionId === collection.id ? 'primary' : 'subtle'}
          className={styles.collectionButton}
          onClick={() => onCollectionSelect?.(collection.id)}
        >
          <div className={styles.collectionContent}>
            {getCollectionIcon(collection.type)}
            <Text className={styles.collectionName}>{collection.name}</Text>
            <Badge appearance="filled" size="small">
              {collection.count}
            </Badge>
          </div>
        </Button>
      ))}
    </div>
  );
};
