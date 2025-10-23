import React, { useState } from 'react';
import {
  Button,
  Input,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Card,
  Text,
  Spinner,
  makeStyles,
  shorthands,
  tokens,
} from '@fluentui/react-components';
import { Search24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import { assetService } from '../../services/assetService';
import { StockImage } from '../../types/assets';

const useStyles = makeStyles({
  searchBar: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalM),
    marginBottom: tokens.spacingVerticalL,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    ...shorthands.gap(tokens.spacingHorizontalM),
    maxHeight: '500px',
    overflowY: 'auto',
  },
  imageCard: {
    cursor: 'pointer',
    ':hover': {
      boxShadow: tokens.shadow8,
    },
  },
  image: {
    width: '100%',
    height: '150px',
    objectFit: 'cover',
  },
  imageInfo: {
    ...shorthands.padding(tokens.spacingVerticalS),
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    ...shorthands.padding(tokens.spacingVerticalXXL),
    ...shorthands.gap(tokens.spacingVerticalM),
  },
});

interface StockImageSearchProps {
  isOpen: boolean;
  onClose: () => void;
  onImageAdded?: () => void;
}

export const StockImageSearch: React.FC<StockImageSearchProps> = ({
  isOpen,
  onClose,
  onImageAdded,
}) => {
  const styles = useStyles();
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<StockImage[]>([]);
  const [loading, setLoading] = useState(false);
  const [downloading, setDownloading] = useState<string | null>(null);

  const handleSearch = async () => {
    if (!query.trim()) return;

    setLoading(true);
    try {
      const images = await assetService.searchStockImages(query, 20);
      setResults(images);
    } catch (error) {
      console.error('Failed to search stock images:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleDownload = async (image: StockImage) => {
    setDownloading(image.fullSizeUrl);
    try {
      await assetService.downloadStockImage({
        imageUrl: image.fullSizeUrl,
        source: image.source,
        photographer: image.photographer,
      });
      onImageAdded?.();
    } catch (error) {
      console.error('Failed to download image:', error);
    } finally {
      setDownloading(null);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface style={{ maxWidth: '800px', maxHeight: '80vh' }}>
        <DialogTitle
          action={<Button appearance="subtle" icon={<Dismiss24Regular />} onClick={onClose} />}
        >
          Stock Image Search
        </DialogTitle>
        <DialogBody>
          <DialogContent>
            <div className={styles.searchBar}>
              <Input
                placeholder="Search for images..."
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                style={{ flexGrow: 1 }}
                contentBefore={<Search24Regular />}
              />
              <Button appearance="primary" onClick={handleSearch} disabled={loading}>
                Search
              </Button>
            </div>

            {loading ? (
              <div className={styles.emptyState}>
                <Spinner size="large" />
                <Text>Searching stock images...</Text>
              </div>
            ) : results.length === 0 ? (
              <div className={styles.emptyState}>
                <Search24Regular style={{ fontSize: '48px' }} />
                <Text>Search for stock images from Pexels and Pixabay</Text>
                <Text size={200}>Enter a search term above to get started</Text>
              </div>
            ) : (
              <div className={styles.grid}>
                {results.map((image, idx) => (
                  <Card
                    key={idx}
                    className={styles.imageCard}
                    onClick={() => handleDownload(image)}
                  >
                    <img
                      src={image.thumbnailUrl}
                      alt={image.photographer || `Photo ${idx + 1}`}
                      className={styles.image}
                    />
                    <div className={styles.imageInfo}>
                      {image.photographer && (
                        <Text size={200} truncate>
                          {image.photographer}
                        </Text>
                      )}
                      <Text size={100} style={{ color: tokens.colorNeutralForeground3 }}>
                        {image.source} • {image.width}x{image.height}
                      </Text>
                    </div>
                    {downloading === image.fullSizeUrl && (
                      <div
                        style={{
                          position: 'absolute',
                          top: 0,
                          left: 0,
                          right: 0,
                          bottom: 0,
                          backgroundColor: 'rgba(0,0,0,0.5)',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                        }}
                      >
                        <Spinner size="small" />
                      </div>
                    )}
                  </Card>
                ))}
              </div>
            )}
          </DialogContent>
        </DialogBody>
        <DialogActions>
          <Button appearance="secondary" onClick={onClose}>
            Close
          </Button>
        </DialogActions>
      </DialogSurface>
    </Dialog>
  );
};
