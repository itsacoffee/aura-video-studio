import { Tooltip, Badge } from '@fluentui/react-components';
import { DatabaseRegular, ClockRegular } from '@fluentui/react-icons';
import React from 'react';
import type { CacheMetadata } from '../../types/cache';

interface CacheIndicatorProps {
  metadata?: CacheMetadata;
}

/**
 * Displays a subtle indicator when a response was cached
 */
const CacheIndicator: React.FC<CacheIndicatorProps> = ({ metadata }) => {
  if (!metadata?.fromCache) {
    return null;
  }

  const formatCacheAge = (ageMs?: number): string => {
    if (!ageMs) return 'just now';

    const seconds = Math.floor(ageMs / 1000);
    if (seconds < 60) return `${seconds}s ago`;

    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes}m ago`;

    const hours = Math.floor(minutes / 60);
    return `${hours}h ago`;
  };

  const tooltipContent = (
    <div style={{ padding: '8px' }}>
      <div style={{ marginBottom: '4px', fontWeight: 600 }}>
        <DatabaseRegular style={{ marginRight: '4px', verticalAlign: 'middle' }} />
        Cached Response
      </div>
      {metadata.cacheAge !== undefined && (
        <div style={{ fontSize: '12px', marginBottom: '4px' }}>
          <ClockRegular style={{ marginRight: '4px', verticalAlign: 'middle', fontSize: '12px' }} />
          {formatCacheAge(metadata.cacheAge)}
        </div>
      )}
      {metadata.accessCount !== undefined && (
        <div style={{ fontSize: '12px', marginBottom: '4px' }}>
          Accessed {metadata.accessCount} time{metadata.accessCount !== 1 ? 's' : ''}
        </div>
      )}
      {metadata.cacheKey && (
        <div style={{ fontSize: '11px', color: '#999', marginTop: '4px' }}>
          Key: {metadata.cacheKey.substring(0, 16)}...
        </div>
      )}
    </div>
  );

  return (
    <Tooltip content={tooltipContent} relationship="label">
      <Badge appearance="filled" color="informative" size="small" icon={<DatabaseRegular />}>
        Cached
      </Badge>
    </Tooltip>
  );
};

export default CacheIndicator;
