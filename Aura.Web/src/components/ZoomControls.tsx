import { Button, makeStyles, tokens, Tooltip, Text } from '@fluentui/react-components';
import { ZoomIn24Regular, ZoomOut24Regular } from '@fluentui/react-icons';
import { useEffect, useMemo, useState } from 'react';

const MIN_ZOOM = 100;
const MAX_ZOOM = 140;
const STEP = 10;
const DEFAULT_ZOOM = 120;

const useStyles = makeStyles({
  root: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
  },
  value: {
    minWidth: '48px',
    textAlign: 'center',
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground2,
  },
});

export function ZoomControls() {
  const styles = useStyles();
  const initialZoom = useMemo(() => {
    try {
      const stored = localStorage.getItem('aura-ui-zoom');
      const parsed = stored ? parseInt(stored, 10) : DEFAULT_ZOOM;
      return Number.isFinite(parsed)
        ? Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, parsed))
        : DEFAULT_ZOOM;
    } catch {
      return DEFAULT_ZOOM;
    }
  }, []);

  const [zoom, setZoom] = useState<number>(initialZoom);

  useEffect(() => {
    const clamped = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, zoom));
    document.documentElement.style.setProperty('--aura-base-font-size', `${clamped}%`);
    try {
      localStorage.setItem('aura-ui-zoom', String(clamped));
    } catch {
      // ignore storage issues
    }
    // ensure local state remains clamped
    if (clamped !== zoom) {
      setZoom(clamped);
    }
  }, [zoom]);

  const handleZoom = (delta: number) => {
    setZoom((current) => Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, current + delta)));
  };

  return (
    <div className={styles.root} aria-label="UI zoom controls">
      <Tooltip content="Zoom out" relationship="label">
        <Button
          appearance="subtle"
          icon={<ZoomOut24Regular />}
          aria-label="Zoom out"
          onClick={() => handleZoom(-STEP)}
        />
      </Tooltip>
      <Text size={200} className={styles.value}>
        {zoom}%
      </Text>
      <Tooltip content="Zoom in" relationship="label">
        <Button
          appearance="subtle"
          icon={<ZoomIn24Regular />}
          aria-label="Zoom in"
          onClick={() => handleZoom(STEP)}
        />
      </Tooltip>
    </div>
  );
}
