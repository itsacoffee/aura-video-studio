import {
  Button,
  Input,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Text,
  makeStyles,
  shorthands,
  tokens,
} from '@fluentui/react-components';
import { Add24Regular, FolderRegular } from '@fluentui/react-icons';
import React, { useState, useEffect } from 'react';
import { assetService } from '../../services/assetService';
import { AssetCollection } from '../../types/assets';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalS),
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  collectionButton: {
    width: '100%',
    justifyContent: 'flex-start',
    marginBottom: tokens.spacingVerticalXS,
  },
  badge: {
    marginLeft: 'auto',
    ...shorthands.padding(tokens.spacingVerticalXXS, tokens.spacingHorizontalXS),
    backgroundColor: tokens.colorBrandBackground2,
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    fontSize: tokens.fontSizeBase200,
  },
  formField: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalS),
    marginBottom: tokens.spacingVerticalM,
  },
});

interface CollectionsPanelProps {
  onCollectionSelect?: (collectionId: string) => void;
  selectedCollectionId?: string;
}

export const CollectionsPanel: React.FC<CollectionsPanelProps> = ({
  onCollectionSelect,
  selectedCollectionId,
}) => {
  const styles = useStyles();
  const [collections, setCollections] = useState<AssetCollection[]>([]);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [newCollectionName, setNewCollectionName] = useState('');
  const [newCollectionDescription, setNewCollectionDescription] = useState('');

  useEffect(() => {
    loadCollections();
  }, []);

  const loadCollections = async () => {
    try {
      const result = await assetService.getCollections();
      setCollections(result);
    } catch (error) {
      console.error('Failed to load collections:', error);
    }
  };

  const handleCreateCollection = async () => {
    if (!newCollectionName.trim()) return;

    try {
      await assetService.createCollection({
        name: newCollectionName,
        description: newCollectionDescription || undefined,
      });
      setNewCollectionName('');
      setNewCollectionDescription('');
      setShowCreateDialog(false);
      loadCollections();
    } catch (error) {
      console.error('Failed to create collection:', error);
    }
  };

  return (
    <>
      <div className={styles.container}>
        <div className={styles.header}>
          <Text size={400} weight="semibold">
            Collections
          </Text>
          <Button
            appearance="subtle"
            icon={<Add24Regular />}
            size="small"
            onClick={() => setShowCreateDialog(true)}
          />
        </div>

        <Button
          className={styles.collectionButton}
          appearance={!selectedCollectionId ? 'primary' : 'subtle'}
          icon={<FolderRegular />}
          onClick={() => onCollectionSelect?.('')}
        >
          All Assets
        </Button>

        {collections.map((collection) => (
          <Button
            key={collection.id}
            className={styles.collectionButton}
            appearance={selectedCollectionId === collection.id ? 'primary' : 'subtle'}
            icon={<FolderRegular />}
            onClick={() => onCollectionSelect?.(collection.id)}
          >
            <span style={{ flexGrow: 1, textAlign: 'left' }}>{collection.name}</span>
            <span className={styles.badge}>{collection.assetIds.length}</span>
          </Button>
        ))}
      </div>

      <Dialog open={showCreateDialog} onOpenChange={(_, data) => setShowCreateDialog(data.open)}>
        <DialogSurface>
          <DialogTitle>Create Collection</DialogTitle>
          <DialogBody>
            <DialogContent>
              <div className={styles.formField}>
                <Text weight="semibold">Name</Text>
                <Input
                  placeholder="Collection name..."
                  value={newCollectionName}
                  onChange={(e) => setNewCollectionName(e.target.value)}
                />
              </div>
              <div className={styles.formField}>
                <Text weight="semibold">Description (optional)</Text>
                <Input
                  placeholder="Collection description..."
                  value={newCollectionDescription}
                  onChange={(e) => setNewCollectionDescription(e.target.value)}
                />
              </div>
            </DialogContent>
          </DialogBody>
          <DialogActions>
            <Button appearance="secondary" onClick={() => setShowCreateDialog(false)}>
              Cancel
            </Button>
            <Button appearance="primary" onClick={handleCreateCollection}>
              Create
            </Button>
          </DialogActions>
        </DialogSurface>
      </Dialog>
    </>
  );
};
