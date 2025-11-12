import {
  makeStyles,
  shorthands,
  tokens,
  Card,
  CardHeader,
  CardPreview,
  Image,
  Text,
  Badge,
  Checkbox,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Button,
  Tooltip,
} from '@fluentui/react-components';
import {
  MoreVertical24Regular,
  Delete24Regular,
  Edit24Regular,
  Eye24Regular,
  CloudArrowDown24Regular,
} from '@fluentui/react-icons';
import React from 'react';
import type { MediaItemResponse } from '../../../types/mediaLibrary';
import { formatFileSize, formatDate } from '../../../utils/format';

const useStyles = makeStyles({
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    ...shorthands.gap(tokens.spacingVerticalM, tokens.spacingHorizontalM),
  },
  card: {
    position: 'relative',
    cursor: 'pointer',
    ':hover': {
      boxShadow: tokens.shadow16,
    },
  },
  preview: {
    height: '200px',
    backgroundColor: tokens.colorNeutralBackground3,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  checkbox: {
    position: 'absolute',
    top: '8px',
    left: '8px',
    zIndex: 1,
  },
  menu: {
    position: 'absolute',
    top: '8px',
    right: '8px',
    zIndex: 1,
  },
  info: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalXS),
    ...shorthands.padding(tokens.spacingVerticalS),
  },
  badges: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalXS),
    flexWrap: 'wrap',
  },
  metadata: {
    display: 'flex',
    justifyContent: 'space-between',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
});

interface MediaGridProps {
  items: MediaItemResponse[];
  selectedIds: Set<string>;
  onSelectItem: (id: string, selected: boolean) => void;
  onDelete: (id: string) => void;
  onPreview?: (media: MediaItemResponse) => void;
}

export const MediaGrid: React.FC<MediaGridProps> = ({
  items,
  selectedIds,
  onSelectItem,
  onDelete,
  onPreview,
}) => {
  const styles = useStyles();

  return (
    <div className={styles.grid}>
      {items.map((item) => (
        <Card key={item.id} className={styles.card}>
          <div className={styles.checkbox}>
            <Checkbox
              checked={selectedIds.has(item.id)}
              onChange={(_, data) => onSelectItem(item.id, !!data.checked)}
            />
          </div>

          <div className={styles.menu}>
            <Menu>
              <MenuTrigger disableButtonEnhancement>
                <Button
                  appearance="subtle"
                  icon={<MoreVertical24Regular />}
                  size="small"
                />
              </MenuTrigger>
              <MenuPopover>
                <MenuList>
                  <MenuItem 
                    icon={<Eye24Regular />}
                    onClick={() => onPreview?.(item)}
                  >
                    View
                  </MenuItem>
                  <MenuItem icon={<Edit24Regular />}>Edit</MenuItem>
                  <MenuItem icon={<CloudArrowDown24Regular />}>Download</MenuItem>
                  <MenuItem
                    icon={<Delete24Regular />}
                    onClick={() => onDelete(item.id)}
                  >
                    Delete
                  </MenuItem>
                </MenuList>
              </MenuPopover>
            </Menu>
          </div>

          <CardPreview className={styles.preview}>
            {item.thumbnailUrl ? (
              <Image
                src={item.thumbnailUrl}
                alt={item.fileName}
                fit="cover"
                style={{ width: '100%', height: '100%' }}
              />
            ) : (
              <Text size={500}>{item.type}</Text>
            )}
          </CardPreview>

          <div className={styles.info}>
            <Tooltip content={item.fileName} relationship="label">
              <Text weight="semibold" truncate>
                {item.fileName}
              </Text>
            </Tooltip>

            {item.description && (
              <Text size={200} truncate>
                {item.description}
              </Text>
            )}

            <div className={styles.badges}>
              <Badge appearance="tint" color="brand">
                {item.type}
              </Badge>
              {item.collectionName && (
                <Badge appearance="outline">{item.collectionName}</Badge>
              )}
              {item.tags.slice(0, 2).map((tag) => (
                <Badge key={tag} appearance="outline" size="small">
                  {tag}
                </Badge>
              ))}
              {item.tags.length > 2 && (
                <Badge appearance="outline" size="small">
                  +{item.tags.length - 2}
                </Badge>
              )}
            </div>

            <div className={styles.metadata}>
              <Text size={200}>{formatFileSize(item.fileSize)}</Text>
              <Text size={200}>{formatDate(item.createdAt)}</Text>
            </div>
          </div>
        </Card>
      ))}
    </div>
  );
};
