import { Badge, makeStyles, tokens } from '@fluentui/react-components';
import React from 'react';
import { useBackendHealth } from '../../hooks/useBackendHealth';

const useStyles = makeStyles({
  container: {
    position: 'fixed',
    bottom: '10px',
    right: '10px',
    zIndex: 1000,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'flex-end',
    gap: tokens.spacingVerticalXXS,
  },
  timestamp: {
    fontSize: '10px',
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalXXS,
  },
});

export const HealthStatusIndicator: React.FC = () => {
  const classes = useStyles();
  const { status, lastChecked } = useBackendHealth();

  const isHealthy = status === 'online';

  return (
    <div className={classes.container}>
      <Badge appearance="filled" color={isHealthy ? 'success' : 'danger'}>
        {isHealthy ? '● Backend Online' : '● Backend Offline'}
      </Badge>
      {lastChecked && (
        <div className={classes.timestamp}>Last check: {lastChecked.toLocaleTimeString()}</div>
      )}
    </div>
  );
};
