import {
  makeStyles,
  shorthands,
  tokens,
  Card,
  Text,
  Checkbox,
  Dropdown,
  Option,
  Button,
  Label,
} from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';
import React from 'react';
import type {
  MediaSearchRequest,
  MediaCollectionResponse,
  MediaType,
  MediaSource,
} from '../../../types/mediaLibrary';

const useStyles = makeStyles({
  panel: {
    marginBottom: tokens.spacingVerticalM,
    ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingHorizontalM),
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  content: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    ...shorthands.gap(tokens.spacingVerticalM, tokens.spacingHorizontalM),
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalS),
  },
});

interface MediaFilterPanelProps {
  filters: MediaSearchRequest;
  collections?: MediaCollectionResponse[];
  tags?: string[];
  onFilterChange: (filters: Partial<MediaSearchRequest>) => void;
  onClose: () => void;
}

export const MediaFilterPanel: React.FC<MediaFilterPanelProps> = ({
  filters,
  collections,
  tags,
  onFilterChange,
  onClose,
}) => {
  const styles = useStyles();

  const mediaTypes: MediaType[] = ['Video', 'Image', 'Audio', 'Document', 'Other'];
  const mediaSources: MediaSource[] = [
    'UserUpload',
    'Generated',
    'StockMedia',
    'Imported',
  ];

  return (
    <Card className={styles.panel}>
      <div className={styles.header}>
        <Text weight="semibold" size={400}>
          Filters
        </Text>
        <Button
          appearance="subtle"
          icon={<Dismiss24Regular />}
          onClick={onClose}
        />
      </div>

      <div className={styles.content}>
        <div className={styles.section}>
          <Label>Media Type</Label>
          {mediaTypes.map((type) => (
            <Checkbox
              key={type}
              label={type}
              checked={filters.types?.includes(type)}
              onChange={(_, data) => {
                const types = data.checked
                  ? [...(filters.types || []), type]
                  : (filters.types || []).filter((t) => t !== type);
                onFilterChange({ types: types.length > 0 ? types : undefined });
              }}
            />
          ))}
        </div>

        <div className={styles.section}>
          <Label>Source</Label>
          {mediaSources.map((source) => (
            <Checkbox
              key={source}
              label={source}
              checked={filters.sources?.includes(source)}
              onChange={(_, data) => {
                const sources = data.checked
                  ? [...(filters.sources || []), source]
                  : (filters.sources || []).filter((s) => s !== source);
                onFilterChange({
                  sources: sources.length > 0 ? sources : undefined,
                });
              }}
            />
          ))}
        </div>

        {collections && collections.length > 0 && (
          <div className={styles.section}>
            <Label>Collection</Label>
            <Dropdown
              placeholder="All collections"
              value={
                collections.find((c) => c.id === filters.collectionId)?.name
              }
              onOptionSelect={(_, data) =>
                onFilterChange({
                  collectionId: data.optionValue || undefined,
                })
              }
            >
              <Option value="">All collections</Option>
              {collections.map((collection) => (
                <Option key={collection.id} value={collection.id}>
                  {collection.name}
                </Option>
              ))}
            </Dropdown>
          </div>
        )}

        {tags && tags.length > 0 && (
          <div className={styles.section}>
            <Label>Tags</Label>
            {tags.slice(0, 10).map((tag) => (
              <Checkbox
                key={tag}
                label={tag}
                checked={filters.tags?.includes(tag)}
                onChange={(_, data) => {
                  const selectedTags = data.checked
                    ? [...(filters.tags || []), tag]
                    : (filters.tags || []).filter((t) => t !== tag);
                  onFilterChange({
                    tags: selectedTags.length > 0 ? selectedTags : undefined,
                  });
                }}
              />
            ))}
          </div>
        )}
      </div>

      <Button
        appearance="subtle"
        onClick={() =>
          onFilterChange({
            types: undefined,
            sources: undefined,
            collectionId: undefined,
            tags: undefined,
          })
        }
      >
        Clear all filters
      </Button>
    </Card>
  );
};
