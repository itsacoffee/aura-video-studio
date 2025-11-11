import { makeStyles, Card, Text, Badge, Button } from '@fluentui/react-components';
import {
  Info24Regular,
  Desktop24Regular,
  Settings24Regular,
  CheckmarkCircle24Regular,
  DismissCircle24Regular,
} from '@fluentui/react-icons';
import { ContextMenu, useContextMenu } from '../components/ContextMenu';
import type { ContextMenuItem } from '../components/ContextMenu';
import { useWindowsNativeUI } from '../hooks/useWindowsNativeUI';

const useStyles = makeStyles({
  container: {
    padding: '24px',
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: '32px',
  },
  section: {
    marginBottom: '32px',
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: '16px',
    marginTop: '16px',
  },
  card: {
    padding: '20px',
  },
  infoRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '12px',
  },
  demoButton: {
    marginTop: '16px',
  },
});

/**
 * Windows 11 Native UI Demo Page
 * Demonstrates Windows 11 integration features and DPI scaling
 */
export function Windows11DemoPage() {
  const styles = useStyles();
  const windowsUI = useWindowsNativeUI();
  const { position, showContextMenu, hideContextMenu } = useContextMenu();

  const contextMenuItems: ContextMenuItem[] = [
    {
      id: 'info',
      label: 'System Information',
      icon: <Info24Regular />,
      onClick: () => alert('Windows 11 Demo'),
    },
    {
      id: 'settings',
      label: 'Display Settings',
      icon: <Settings24Regular />,
      shortcut: 'Ctrl+,',
      onClick: () => alert('Open display settings'),
    },
    { id: 'divider-1', label: '', divider: true },
    {
      id: 'submenu',
      label: 'DPI Options',
      icon: <Desktop24Regular />,
      submenu: [
        {
          id: 'dpi-100',
          label: '100% (Normal)',
          onClick: () => alert('DPI: 100%'),
        },
        {
          id: 'dpi-150',
          label: '150% (Recommended)',
          onClick: () => alert('DPI: 150%'),
        },
        {
          id: 'dpi-200',
          label: '200% (Large)',
          onClick: () => alert('DPI: 200%'),
        },
      ],
    },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text as="h1" size={900} weight="bold">
          Windows 11 Native UI Integration
        </Text>
        <Text as="p" size={400}>
          Demonstration of Windows 11 design language, DPI scaling, and platform features
        </Text>
      </div>

      {/* Platform Detection Section */}
      <div className={styles.section}>
        <Text as="h2" size={600} weight="semibold" style={{ marginBottom: '16px' }}>
          Platform Detection
        </Text>
        <div className={styles.grid}>
          <Card className={styles.card}>
            <div className={styles.infoRow}>
              <Text weight="semibold">Windows Platform</Text>
              <Badge
                appearance="filled"
                color={windowsUI.isWindows ? 'success' : 'danger'}
                icon={
                  windowsUI.isWindows ? <CheckmarkCircle24Regular /> : <DismissCircle24Regular />
                }
              >
                {windowsUI.isWindows ? 'Detected' : 'Not Detected'}
              </Badge>
            </div>
            <div className={styles.infoRow}>
              <Text weight="semibold">Windows 11</Text>
              <Badge
                appearance="filled"
                color={windowsUI.isWindows11 ? 'success' : 'subtle'}
                icon={windowsUI.isWindows11 ? <CheckmarkCircle24Regular /> : <Info24Regular />}
              >
                {windowsUI.isWindows11 ? 'Detected' : 'Unknown'}
              </Badge>
            </div>
          </Card>

          <Card className={styles.card}>
            <div className={styles.infoRow}>
              <Text weight="semibold">System Theme</Text>
              <Badge
                appearance="outline"
                color={windowsUI.systemTheme === 'dark' ? 'brand' : 'warning'}
              >
                {windowsUI.systemTheme === 'dark' ? 'Dark Mode' : 'Light Mode'}
              </Badge>
            </div>
            <div className={styles.infoRow}>
              <Text weight="semibold">Snap Layouts</Text>
              <Badge
                appearance="filled"
                color={windowsUI.supportsSnapLayouts ? 'success' : 'subtle'}
                icon={
                  windowsUI.supportsSnapLayouts ? <CheckmarkCircle24Regular /> : <Info24Regular />
                }
              >
                {windowsUI.supportsSnapLayouts ? 'Supported' : 'Not Available'}
              </Badge>
            </div>
          </Card>
        </div>
      </div>

      {/* DPI Scaling Section */}
      <div className={styles.section}>
        <Text as="h2" size={600} weight="semibold" style={{ marginBottom: '16px' }}>
          DPI Scaling Information
        </Text>
        <div className={styles.grid}>
          <Card className={styles.card}>
            <Text weight="semibold" block style={{ marginBottom: '12px' }}>
              Display Scaling
            </Text>
            <div className={styles.infoRow}>
              <Text>DPI Ratio</Text>
              <Text weight="semibold">{windowsUI.dpiInfo.ratio.toFixed(2)}x</Text>
            </div>
            <div className={styles.infoRow}>
              <Text>Scaling Percentage</Text>
              <Text weight="semibold">{windowsUI.dpiInfo.percentage}%</Text>
            </div>
            <div className={styles.infoRow}>
              <Text>High DPI</Text>
              <Badge
                appearance="filled"
                color={windowsUI.dpiInfo.isHighDPI ? 'important' : 'subtle'}
              >
                {windowsUI.dpiInfo.isHighDPI ? 'Yes' : 'No'}
              </Badge>
            </div>
          </Card>

          <Card className={styles.card}>
            <Text weight="semibold" block style={{ marginBottom: '12px' }}>
              DPI Category
            </Text>
            <Badge
              appearance="filled"
              size="large"
              color={
                windowsUI.dpiInfo.scaleCategory === 'very-high'
                  ? 'danger'
                  : windowsUI.dpiInfo.scaleCategory === 'high'
                    ? 'important'
                    : windowsUI.dpiInfo.scaleCategory === 'medium'
                      ? 'warning'
                      : 'success'
              }
            >
              {windowsUI.dpiInfo.scaleCategory.toUpperCase()}
            </Badge>
            <Text as="p" size={300} style={{ marginTop: '12px', opacity: 0.7 }}>
              {windowsUI.dpiInfo.scaleCategory === 'normal' && 'Standard 100% display scaling'}
              {windowsUI.dpiInfo.scaleCategory === 'medium' &&
                'Medium 150% display scaling (recommended)'}
              {windowsUI.dpiInfo.scaleCategory === 'high' && 'High 200% display scaling'}
              {windowsUI.dpiInfo.scaleCategory === 'very-high' && '300%+ display scaling'}
            </Text>
          </Card>
        </div>
      </div>

      {/* Windows 11 Features Demo */}
      <div className={styles.section}>
        <Text as="h2" size={600} weight="semibold" style={{ marginBottom: '16px' }}>
          Windows 11 Features Demo
        </Text>
        <Card className={styles.card}>
          <Text as="p" style={{ marginBottom: '16px' }}>
            Right-click anywhere in this card to open a Windows-native context menu
          </Text>
          <div
            onContextMenu={showContextMenu}
            style={{
              padding: '48px',
              border: '2px dashed var(--color-border)',
              borderRadius: '8px',
              textAlign: 'center',
              cursor: 'context-menu',
            }}
          >
            <Desktop24Regular style={{ fontSize: '48px', marginBottom: '16px' }} />
            <Text block weight="semibold">
              Right-click here
            </Text>
            <Text block size={300} style={{ opacity: 0.7 }}>
              To see Windows 11 context menu with acrylic effect
            </Text>
          </div>

          <Button appearance="primary" className={styles.demoButton}>
            Test Windows 11 Button Styling
          </Button>
        </Card>
      </div>

      {/* Context Menu */}
      <ContextMenu position={position} items={contextMenuItems} onClose={hideContextMenu} />

      {/* CSS Classes Applied */}
      <div className={styles.section}>
        <Text as="h2" size={600} weight="semibold" style={{ marginBottom: '16px' }}>
          Applied CSS Classes
        </Text>
        <Card className={styles.card}>
          <Text as="p" style={{ marginBottom: '12px' }}>
            The following classes are automatically applied based on your system:
          </Text>
          <div style={{ fontFamily: 'monospace', fontSize: '14px' }}>
            <div style={{ marginBottom: '8px' }}>
              <Badge appearance="outline" color="brand">
                body.
                {windowsUI.isWindows11 ? 'windows-11' : windowsUI.isWindows ? 'windows' : 'other'}
              </Badge>
            </div>
            <div style={{ marginBottom: '8px' }}>
              <Badge appearance="outline" color="brand">
                body.dpi-{windowsUI.dpiInfo.scaleCategory}
              </Badge>
            </div>
          </div>
          <Text as="p" size={300} style={{ marginTop: '16px', opacity: 0.7 }}>
            These classes enable Windows 11-specific styling including rounded corners, acrylic
            materials, mica effects, and DPI-aware sizing.
          </Text>
        </Card>
      </div>
    </div>
  );
}
