import React, { useState } from 'react';
import {
  Button,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  makeStyles,
} from '@fluentui/react-components';
import { ArrowDownload24Regular } from '@fluentui/react-icons';
import { useQualityDashboardStore } from '../../state/qualityDashboard';

const useStyles = makeStyles({
  button: {
    minWidth: '120px',
  },
});

export const ExportControls: React.FC = () => {
  const styles = useStyles();
  const [isExporting, setIsExporting] = useState(false);
  const { exportReport } = useQualityDashboardStore();

  const handleExport = async (format: 'json' | 'csv' | 'markdown') => {
    setIsExporting(true);
    try {
      await exportReport(format);
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <Menu>
      <MenuTrigger disableButtonEnhancement>
        <Button
          appearance="primary"
          icon={<ArrowDownload24Regular />}
          className={styles.button}
          disabled={isExporting}
        >
          {isExporting ? 'Exporting...' : 'Export Report'}
        </Button>
      </MenuTrigger>

      <MenuPopover>
        <MenuList>
          <MenuItem onClick={() => handleExport('json')}>Export as JSON</MenuItem>
          <MenuItem onClick={() => handleExport('csv')}>Export as CSV</MenuItem>
          <MenuItem onClick={() => handleExport('markdown')}>Export as Markdown</MenuItem>
        </MenuList>
      </MenuPopover>
    </Menu>
  );
};
