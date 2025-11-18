import {
  makeStyles,
  shorthands,
  tokens,
  Table,
  TableBody,
  TableCell,
  TableRow,
  TableHeader,
  TableHeaderCell,
  Checkbox,
  Badge,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Button,
  Avatar,
  Text,
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
  container: {
    width: '100%',
    overflowX: 'auto',
  },
  thumbnail: {
    width: '48px',
    height: '48px',
  },
  nameCell: {
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingHorizontalM),
  },
  badges: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalXS),
  },
});

interface MediaListProps {
  items: MediaItemResponse[];
  selectedIds: Set<string>;
  onSelectItem: (id: string, selected: boolean) => void;
  onDelete: (id: string) => void;
  onPreview?: (media: MediaItemResponse) => void;
}

export const MediaList: React.FC<MediaListProps> = ({
  items,
  selectedIds,
  onSelectItem,
  onDelete,
  onPreview,
}) => {
  const styles = useStyles();

  return (
    <div className={styles.container}>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHeaderCell>
              <Checkbox />
            </TableHeaderCell>
            <TableHeaderCell>Name</TableHeaderCell>
            <TableHeaderCell>Type</TableHeaderCell>
            <TableHeaderCell>Size</TableHeaderCell>
            <TableHeaderCell>Collection</TableHeaderCell>
            <TableHeaderCell>Tags</TableHeaderCell>
            <TableHeaderCell>Created</TableHeaderCell>
            <TableHeaderCell>Actions</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((item) => (
            <TableRow key={item.id}>
              <TableCell>
                <Checkbox
                  checked={selectedIds.has(item.id)}
                  onChange={(_, data) => onSelectItem(item.id, !!data.checked)}
                />
              </TableCell>
              <TableCell>
                <div className={styles.nameCell}>
                  <Avatar
                    image={item.thumbnailUrl ? { src: item.thumbnailUrl } : undefined}
                    size={48}
                    shape="square"
                  />
                  <div>
                    <Text weight="semibold">{item.fileName}</Text>
                    {item.description && (
                      <Text size={200} block>
                        {item.description}
                      </Text>
                    )}
                  </div>
                </div>
              </TableCell>
              <TableCell>
                <Badge appearance="tint" color="brand">
                  {item.type}
                </Badge>
              </TableCell>
              <TableCell>{formatFileSize(item.fileSize)}</TableCell>
              <TableCell>{item.collectionName || '-'}</TableCell>
              <TableCell>
                <div className={styles.badges}>
                  {item.tags.slice(0, 3).map((tag) => (
                    <Badge key={tag} appearance="outline" size="small">
                      {tag}
                    </Badge>
                  ))}
                  {item.tags.length > 3 && (
                    <Badge appearance="outline" size="small">
                      +{item.tags.length - 3}
                    </Badge>
                  )}
                </div>
              </TableCell>
              <TableCell>{formatDate(item.createdAt)}</TableCell>
              <TableCell>
                <Menu>
                  <MenuTrigger disableButtonEnhancement>
                    <Button appearance="subtle" icon={<MoreVertical24Regular />} size="small" />
                  </MenuTrigger>
                  <MenuPopover>
                    <MenuList>
                      <MenuItem icon={<Eye24Regular />} onClick={() => onPreview?.(item)}>
                        View
                      </MenuItem>
                      <MenuItem icon={<Edit24Regular />}>Edit</MenuItem>
                      <MenuItem icon={<CloudArrowDown24Regular />}>Download</MenuItem>
                      <MenuItem icon={<Delete24Regular />} onClick={() => onDelete(item.id)}>
                        Delete
                      </MenuItem>
                    </MenuList>
                  </MenuPopover>
                </Menu>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
};
