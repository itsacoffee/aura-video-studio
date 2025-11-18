import {
  makeStyles,
  shorthands,
  tokens,
  Button,
  Text,
  Dropdown,
  Option,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Input,
} from '@fluentui/react-components';
import {
  Delete24Regular,
  Folder24Regular,
  Tag24Regular,
  Dismiss24Regular,
} from '@fluentui/react-icons';
import { useMutation } from '@tanstack/react-query';
import React, { useState } from 'react';
import { mediaLibraryApi } from '../../../api/mediaLibraryApi';
import type {
  MediaCollectionResponse,
  BulkMediaOperationRequest,
} from '../../../types/mediaLibrary';

const useStyles = makeStyles({
  bar: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingHorizontalM),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    backgroundColor: tokens.colorBrandBackground2,
    marginBottom: tokens.spacingVerticalM,
  },
  actions: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalS),
  },
});

interface BulkOperationsBarProps {
  selectedCount: number;
  selectedIds: string[];
  collections?: MediaCollectionResponse[];
  onComplete: () => void;
  onCancel: () => void;
}

export const BulkOperationsBar: React.FC<BulkOperationsBarProps> = ({
  selectedCount,
  selectedIds,
  collections,
  onComplete,
  onCancel,
}) => {
  const styles = useStyles();
  const [showDialog, setShowDialog] = useState(false);
  const [dialogType, setDialogType] = useState<'move' | 'addTags' | 'removeTags'>();
  const [inputValue, setInputValue] = useState('');

  const bulkMutation = useMutation({
    mutationFn: (request: BulkMediaOperationRequest) => mediaLibraryApi.bulkOperation(request),
    onSuccess: () => {
      setShowDialog(false);
      onComplete();
    },
  });

  const handleDelete = () => {
    if (confirm(`Delete ${selectedCount} items?`)) {
      bulkMutation.mutate({
        mediaIds: selectedIds,
        operation: 'Delete',
      });
    }
  };

  const handleMove = (collectionId: string) => {
    bulkMutation.mutate({
      mediaIds: selectedIds,
      operation: 'ChangeCollection',
      targetCollectionId: collectionId,
    });
  };

  const handleAddTags = () => {
    const tags = inputValue.split(',').map((t) => t.trim());
    bulkMutation.mutate({
      mediaIds: selectedIds,
      operation: 'AddTags',
      tags,
    });
  };

  const handleRemoveTags = () => {
    const tags = inputValue.split(',').map((t) => t.trim());
    bulkMutation.mutate({
      mediaIds: selectedIds,
      operation: 'RemoveTags',
      tags,
    });
  };

  return (
    <>
      <div className={styles.bar}>
        <Text weight="semibold">{selectedCount} items selected</Text>

        <div className={styles.actions}>
          {collections && collections.length > 0 && (
            <Dropdown
              placeholder="Move to..."
              onOptionSelect={(_, data) => handleMove(data.optionValue!)}
            >
              {collections.map((collection) => (
                <Option key={collection.id} value={collection.id}>
                  {collection.name}
                </Option>
              ))}
            </Dropdown>
          )}

          <Button
            icon={<Tag24Regular />}
            onClick={() => {
              setDialogType('addTags');
              setShowDialog(true);
            }}
          >
            Add Tags
          </Button>

          <Button
            icon={<Tag24Regular />}
            onClick={() => {
              setDialogType('removeTags');
              setShowDialog(true);
            }}
          >
            Remove Tags
          </Button>

          <Button icon={<Delete24Regular />} appearance="subtle" onClick={handleDelete}>
            Delete
          </Button>

          <Button icon={<Dismiss24Regular />} appearance="subtle" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </div>

      <Dialog open={showDialog} onOpenChange={(_, data) => setShowDialog(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{dialogType === 'addTags' ? 'Add Tags' : 'Remove Tags'}</DialogTitle>
            <DialogContent>
              <Input
                placeholder="Enter tags (comma-separated)"
                value={inputValue}
                onChange={(_, data) => setInputValue(data.value)}
              />
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setShowDialog(false)}>
                Cancel
              </Button>
              <Button
                appearance="primary"
                onClick={dialogType === 'addTags' ? handleAddTags : handleRemoveTags}
                disabled={!inputValue}
              >
                {dialogType === 'addTags' ? 'Add' : 'Remove'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </>
  );
};
