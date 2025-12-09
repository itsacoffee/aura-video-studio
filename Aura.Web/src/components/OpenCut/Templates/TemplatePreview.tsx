/**
 * TemplatePreview Component
 *
 * Shows a detailed preview of a template including its tracks,
 * markers, and other configuration details.
 */

import { makeStyles, tokens, Text, Badge, Divider } from '@fluentui/react-components';
import {
  Video24Regular,
  MusicNote124Regular,
  Image24Regular,
  TextT24Regular,
  Flag24Regular,
  Timer24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import type { Template, TemplateTrackType } from '../../../stores/opencutTemplates';
import { openCutTokens } from '../../../styles/designTokens';

export interface TemplatePreviewProps {
  template: Template;
  className?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.lg,
    padding: openCutTokens.spacing.lg,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: openCutTokens.radius.lg,
  },
  header: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase500,
  },
  description: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase300,
  },
  badges: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
    marginTop: tokens.spacingVerticalXS,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  sectionTitle: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
  },
  trackList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  track: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: openCutTokens.radius.sm,
  },
  trackIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '16px',
  },
  trackIconVideo: {
    color: '#3B82F6',
  },
  trackIconAudio: {
    color: '#22C55E',
  },
  trackIconImage: {
    color: '#A855F7',
  },
  trackIconText: {
    color: '#F59E0B',
  },
  trackName: {
    flex: 1,
    fontSize: tokens.fontSizeBase200,
  },
  trackType: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground4,
    textTransform: 'capitalize',
  },
  markerList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  marker: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    fontSize: tokens.fontSizeBase200,
  },
  markerTime: {
    color: tokens.colorNeutralForeground4,
    fontFamily: 'monospace',
    minWidth: '50px',
  },
  markerName: {
    color: tokens.colorNeutralForeground2,
  },
  stats: {
    display: 'grid',
    gridTemplateColumns: 'repeat(2, 1fr)',
    gap: tokens.spacingHorizontalM,
  },
  stat: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: openCutTokens.radius.sm,
  },
  statIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '20px',
  },
  statContent: {
    display: 'flex',
    flexDirection: 'column',
  },
  statValue: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
  },
  statLabel: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground4,
  },
  tags: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
  },
  tag: {
    fontSize: tokens.fontSizeBase100,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusSmall,
    color: tokens.colorNeutralForeground3,
  },
  emptyState: {
    color: tokens.colorNeutralForeground4,
    fontSize: tokens.fontSizeBase200,
    fontStyle: 'italic',
  },
});

const TrackIcon: FC<{ type: TemplateTrackType; className?: string }> = ({ type, className }) => {
  const styles = useStyles();

  switch (type) {
    case 'video':
      return (
        <Video24Regular
          className={`${styles.trackIcon} ${styles.trackIconVideo} ${className || ''}`}
        />
      );
    case 'audio':
      return (
        <MusicNote124Regular
          className={`${styles.trackIcon} ${styles.trackIconAudio} ${className || ''}`}
        />
      );
    case 'image':
      return (
        <Image24Regular
          className={`${styles.trackIcon} ${styles.trackIconImage} ${className || ''}`}
        />
      );
    case 'text':
      return (
        <TextT24Regular
          className={`${styles.trackIcon} ${styles.trackIconText} ${className || ''}`}
        />
      );
    default:
      return <Video24Regular className={`${styles.trackIcon} ${className || ''}`} />;
  }
};

const formatTime = (seconds: number): string => {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
};

const formatDuration = (seconds: number): string => {
  if (seconds < 60) return `${seconds}s`;
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  if (minutes >= 60) {
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    return `${hours}h ${remainingMinutes}m`;
  }
  if (remainingSeconds === 0) return `${minutes}m`;
  return `${minutes}m ${remainingSeconds}s`;
};

export const TemplatePreview: FC<TemplatePreviewProps> = ({ template, className }) => {
  const styles = useStyles();

  const sortedTracks = [...template.data.tracks].sort((a, b) => a.order - b.order);

  return (
    <div className={`${styles.container} ${className || ''}`}>
      {/* Header */}
      <div className={styles.header}>
        <Text className={styles.title}>{template.name}</Text>
        <Text className={styles.description}>{template.description}</Text>
        <div className={styles.badges}>
          <Badge appearance="outline" size="small">
            {template.aspectRatio}
          </Badge>
          <Badge appearance="outline" size="small" color="informative">
            {template.category}
          </Badge>
          {template.isBuiltin && (
            <Badge appearance="filled" size="small" color="informative">
              Built-in
            </Badge>
          )}
        </div>
      </div>

      <Divider />

      {/* Stats */}
      <div className={styles.stats}>
        <div className={styles.stat}>
          <Video24Regular className={styles.statIcon} />
          <div className={styles.statContent}>
            <Text className={styles.statValue}>{template.data.tracks.length}</Text>
            <Text className={styles.statLabel}>Tracks</Text>
          </div>
        </div>
        <div className={styles.stat}>
          <Timer24Regular className={styles.statIcon} />
          <div className={styles.statContent}>
            <Text className={styles.statValue}>
              {template.duration > 0 ? formatDuration(template.duration) : 'Variable'}
            </Text>
            <Text className={styles.statLabel}>Duration</Text>
          </div>
        </div>
        <div className={styles.stat}>
          <Flag24Regular className={styles.statIcon} />
          <div className={styles.statContent}>
            <Text className={styles.statValue}>{template.data.markers.length}</Text>
            <Text className={styles.statLabel}>Markers</Text>
          </div>
        </div>
      </div>

      {/* Tracks */}
      <div className={styles.section}>
        <Text className={styles.sectionTitle}>Tracks</Text>
        <div className={styles.trackList}>
          {sortedTracks.length > 0 ? (
            sortedTracks.map((track) => (
              <div key={track.id} className={styles.track}>
                <TrackIcon type={track.type} />
                <Text className={styles.trackName}>{track.name}</Text>
                <Text className={styles.trackType}>{track.type}</Text>
              </div>
            ))
          ) : (
            <Text className={styles.emptyState}>No tracks configured</Text>
          )}
        </div>
      </div>

      {/* Markers */}
      {template.data.markers.length > 0 && (
        <div className={styles.section}>
          <Text className={styles.sectionTitle}>Markers</Text>
          <div className={styles.markerList}>
            {template.data.markers.map((marker) => (
              <div key={marker.id} className={styles.marker}>
                <Flag24Regular style={{ color: marker.color, fontSize: '14px' }} />
                <Text className={styles.markerTime}>{formatTime(marker.time)}</Text>
                <Text className={styles.markerName}>{marker.name}</Text>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Tags */}
      {template.tags.length > 0 && (
        <div className={styles.section}>
          <Text className={styles.sectionTitle}>Tags</Text>
          <div className={styles.tags}>
            {template.tags.map((tag) => (
              <span key={tag} className={styles.tag}>
                {tag}
              </span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export default TemplatePreview;
