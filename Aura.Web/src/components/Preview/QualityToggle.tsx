import { Button, Switch, Badge, Tooltip, makeStyles, tokens } from '@fluentui/react-components';
import { VideoClip24Regular, VideoClip24Filled, bundleIcon } from '@fluentui/react-icons';
import { FC, useState, useEffect } from 'react';
import { proxyMediaService } from '../../services/proxyMediaService';

const VideoClipIcon = bundleIcon(VideoClip24Filled, VideoClip24Regular);

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  switchContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  label: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
  },
  badge: {
    marginLeft: tokens.spacingHorizontalXS,
  },
});

interface QualityToggleProps {
  onToggle?: (useProxy: boolean) => void;
  showStats?: boolean;
}

export const QualityToggle: FC<QualityToggleProps> = ({ onToggle, showStats = false }) => {
  const styles = useStyles();
  const [useProxy, setUseProxy] = useState(proxyMediaService.isProxyModeEnabled());
  const [stats, setStats] = useState<{ totalProxies: number; compressionRatio: number } | null>(
    null
  );

  useEffect(() => {
    if (showStats) {
      loadStats();
    }
  }, [showStats]);

  const loadStats = async () => {
    try {
      const cacheStats = await proxyMediaService.getCacheStats();
      setStats({
        totalProxies: cacheStats.totalProxies,
        compressionRatio: cacheStats.compressionRatio,
      });
    } catch (error) {
      console.error('Error loading cache stats:', error);
    }
  };

  const handleToggle = () => {
    const newValue = proxyMediaService.toggleProxyMode();
    setUseProxy(newValue);
    onToggle?.(newValue);
  };

  return (
    <div className={styles.container}>
      <VideoClipIcon />

      <div className={styles.switchContainer}>
        <span className={styles.label}>Preview Quality</span>
        <Tooltip
          content={
            useProxy
              ? 'Using proxy media for faster preview (lower quality)'
              : 'Using source media for high quality preview (slower)'
          }
          relationship="label"
        >
          <Switch
            checked={useProxy}
            onChange={handleToggle}
            label={useProxy ? 'Fast' : 'High Quality'}
          />
        </Tooltip>

        {useProxy && (
          <Badge appearance="tint" color="brand" className={styles.badge}>
            Proxy Active
          </Badge>
        )}
        {!useProxy && (
          <Badge appearance="tint" color="success" className={styles.badge}>
            Source Quality
          </Badge>
        )}
      </div>

      {showStats && stats && (
        <Tooltip
          content={`${stats.totalProxies} proxies cached, ${(stats.compressionRatio * 100).toFixed(1)}% space saved`}
          relationship="label"
        >
          <Button size="small" appearance="subtle" onClick={loadStats}>
            {stats.totalProxies} cached
          </Button>
        </Tooltip>
      )}
    </div>
  );
};

export default QualityToggle;
