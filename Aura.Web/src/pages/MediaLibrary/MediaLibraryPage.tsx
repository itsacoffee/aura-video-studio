import React, { useState, useCallback } from 'react';
import {
  makeStyles,
  shorthands,
  tokens,
  Button,
  Input,
  SearchBox,
  Dropdown,
  Option,
  Spinner,
  Text,
  Title3,
  Card,
  Badge,
  Tooltip,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
} from '@fluentui/react-components';
import {
  Grid24Regular,
  List24Regular,
  CloudArrowUp24Regular,
  Filter24Regular,
  Delete24Regular,
  Folder24Regular,
  Tag24Regular,
  MoreVertical24Regular,
  Search24Regular,
} from '@fluentui/react-icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { MediaGrid } from './components/MediaGrid';
import { MediaList } from './components/MediaList';
import { MediaUploadDialog } from './components/MediaUploadDialog';
import { MediaFilterPanel } from './components/MediaFilterPanel';
import { BulkOperationsBar } from './components/BulkOperationsBar';
import { StorageStats } from './components/StorageStats';
import { mediaLibraryApi } from '../../api/mediaLibraryApi';
import type { MediaSearchRequest, MediaItemResponse } from '../../types/mediaLibrary';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    ...shorthands.padding(tokens.spacingVerticalXL, tokens.spacingHorizontalXL),
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  toolbar: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalM),
    marginBottom: tokens.spacingVerticalM,
    flexWrap: 'wrap',
    alignItems: 'center',
  },
  searchBox: {
    flexGrow: 1,
    minWidth: '300px',
  },
  content: {
    flexGrow: 1,
    overflowY: 'auto',
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    height: '400px',
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    height: '400px',
    ...shorthands.gap(tokens.spacingVerticalM),
  },
  pagination: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingHorizontalM),
    marginTop: tokens.spacingVerticalL,
  },
});

export const MediaLibraryPage: React.FC = () => {
  const styles = useStyles();
  const queryClient = useQueryClient();

  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
  const [searchTerm, setSearchTerm] = useState('');
  const [showFilters, setShowFilters] = useState(false);
  const [showUpload, setShowUpload] = useState(false);
  const [selectedItems, setSelectedItems] = useState<Set<string>>(new Set());
  const [filters, setFilters] = useState<MediaSearchRequest>({
    page: 1,
    pageSize: 50,
    sortBy: 'CreatedAt',
    sortDescending: true,
  });

  // Fetch media items
  const { data: mediaData, isLoading, error } = useQuery({
    queryKey: ['media', filters, searchTerm],
    queryFn: () => mediaLibraryApi.searchMedia({ ...filters, searchTerm }),
  });

  // Fetch storage stats
  const { data: stats } = useQuery({
    queryKey: ['media-stats'],
    queryFn: () => mediaLibraryApi.getStorageStats(),
  });

  // Fetch collections
  const { data: collections } = useQuery({
    queryKey: ['media-collections'],
    queryFn: () => mediaLibraryApi.getCollections(),
  });

  // Fetch tags
  const { data: tags } = useQuery({
    queryKey: ['media-tags'],
    queryFn: () => mediaLibraryApi.getTags(),
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: (id: string) => mediaLibraryApi.deleteMedia(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['media'] });
      queryClient.invalidateQueries({ queryKey: ['media-stats'] });
    },
  });

  const handleSearch = useCallback((value: string) => {
    setSearchTerm(value);
    setFilters((prev) => ({ ...prev, page: 1 }));
  }, []);

  const handleFilterChange = useCallback((newFilters: Partial<MediaSearchRequest>) => {
    setFilters((prev) => ({ ...prev, ...newFilters, page: 1 }));
  }, []);

  const handlePageChange = useCallback((page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  }, []);

  const handleSelectItem = useCallback((id: string, selected: boolean) => {
    setSelectedItems((prev) => {
      const newSet = new Set(prev);
      if (selected) {
        newSet.add(id);
      } else {
        newSet.delete(id);
      }
      return newSet;
    });
  }, []);

  const handleSelectAll = useCallback((selected: boolean) => {
    if (selected && mediaData?.items) {
      setSelectedItems(new Set(mediaData.items.map((item) => item.id)));
    } else {
      setSelectedItems(new Set());
    }
  }, [mediaData]);

  const handleDelete = useCallback(async (id: string) => {
    if (confirm('Are you sure you want to delete this media item?')) {
      await deleteMutation.mutateAsync(id);
    }
  }, [deleteMutation]);

  const handleBulkAction = useCallback(() => {
    setSelectedItems(new Set());
    queryClient.invalidateQueries({ queryKey: ['media'] });
  }, [queryClient]);

  const renderContent = () => {
    if (isLoading) {
      return (
        <div className={styles.loadingContainer}>
          <Spinner label="Loading media library..." />
        </div>
      );
    }

    if (error) {
      return (
        <div className={styles.emptyState}>
          <Text size={500}>Error loading media library</Text>
          <Text size={300}>{(error as Error).message}</Text>
        </div>
      );
    }

    if (!mediaData || mediaData.items.length === 0) {
      return (
        <div className={styles.emptyState}>
          <Text size={500}>No media found</Text>
          <Text size={300}>
            {searchTerm || filters.types || filters.tags
              ? 'Try adjusting your filters'
              : 'Upload media to get started'}
          </Text>
          <Button
            appearance="primary"
            icon={<CloudArrowUp24Regular />}
            onClick={() => setShowUpload(true)}
          >
            Upload Media
          </Button>
        </div>
      );
    }

    return (
      <>
        {viewMode === 'grid' ? (
          <MediaGrid
            items={mediaData.items}
            selectedIds={selectedItems}
            onSelectItem={handleSelectItem}
            onDelete={handleDelete}
          />
        ) : (
          <MediaList
            items={mediaData.items}
            selectedIds={selectedItems}
            onSelectItem={handleSelectItem}
            onDelete={handleDelete}
          />
        )}

        {mediaData.totalPages > 1 && (
          <div className={styles.pagination}>
            <Button
              disabled={filters.page === 1}
              onClick={() => handlePageChange(filters.page! - 1)}
            >
              Previous
            </Button>
            <Text>
              Page {filters.page} of {mediaData.totalPages}
            </Text>
            <Button
              disabled={filters.page === mediaData.totalPages}
              onClick={() => handlePageChange(filters.page! + 1)}
            >
              Next
            </Button>
          </div>
        )}
      </>
    );
  };

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <Title3>Media Library</Title3>
        <Button
          appearance="primary"
          icon={<CloudArrowUp24Regular />}
          onClick={() => setShowUpload(true)}
        >
          Upload Media
        </Button>
      </div>

      {stats && <StorageStats stats={stats} />}

      <div className={styles.toolbar}>
        <SearchBox
          className={styles.searchBox}
          placeholder="Search media..."
          value={searchTerm}
          onChange={(_, data) => handleSearch(data.value)}
        />

        <Button
          icon={<Filter24Regular />}
          onClick={() => setShowFilters(!showFilters)}
        >
          Filters
        </Button>

        <Button
          icon={viewMode === 'grid' ? <List24Regular /> : <Grid24Regular />}
          onClick={() => setViewMode(viewMode === 'grid' ? 'list' : 'grid')}
        >
          {viewMode === 'grid' ? 'List' : 'Grid'}
        </Button>

        <Dropdown
          placeholder="Sort by"
          value={filters.sortBy}
          onOptionSelect={(_, data) =>
            handleFilterChange({ sortBy: data.optionValue as string })
          }
        >
          <Option value="CreatedAt">Date Created</Option>
          <Option value="FileName">Name</Option>
          <Option value="FileSize">Size</Option>
          <Option value="Type">Type</Option>
        </Dropdown>

        <Button
          onClick={() =>
            handleFilterChange({ sortDescending: !filters.sortDescending })
          }
        >
          {filters.sortDescending ? 'Newest' : 'Oldest'}
        </Button>
      </div>

      {showFilters && (
        <MediaFilterPanel
          filters={filters}
          collections={collections}
          tags={tags}
          onFilterChange={handleFilterChange}
          onClose={() => setShowFilters(false)}
        />
      )}

      {selectedItems.size > 0 && (
        <BulkOperationsBar
          selectedCount={selectedItems.size}
          selectedIds={Array.from(selectedItems)}
          collections={collections}
          onComplete={handleBulkAction}
          onCancel={() => setSelectedItems(new Set())}
        />
      )}

      <div className={styles.content}>{renderContent()}</div>

      {showUpload && (
        <MediaUploadDialog
          collections={collections}
          tags={tags}
          onClose={() => setShowUpload(false)}
          onSuccess={() => {
            setShowUpload(false);
            queryClient.invalidateQueries({ queryKey: ['media'] });
            queryClient.invalidateQueries({ queryKey: ['media-stats'] });
          }}
        />
      )}
    </div>
  );
};
