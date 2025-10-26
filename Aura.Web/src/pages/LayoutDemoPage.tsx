/**
 * Layout Demo Page
 * Demonstrates all new layout components and features
 */

import { makeStyles, Title1, Title2, Text, Button, Card } from '@fluentui/react-components';
import { useState } from 'react';
import {
  SkeletonText,
  SkeletonTitle,
  SkeletonButton,
  SkeletonPanel,
  SkeletonMediaItem,
  SkeletonTimelineItem,
  SkeletonPropertiesPanel,
  FadeIn,
} from '../components/Layout/Loading';
import { PanelTabs, TabItem } from '../components/Layout/PanelTabs';
import { StatusFooter } from '../components/Layout/StatusFooter';
import { TopMenuBar } from '../components/Layout/TopMenuBar';

const useStyles = makeStyles({
  page: {
    padding: 'var(--space-4)',
    maxWidth: '1400px',
    margin: '0 auto',
  },
  section: {
    marginBottom: 'var(--space-6)',
    padding: 'var(--space-3)',
    backgroundColor: 'var(--panel-bg)',
    borderRadius: 'var(--border-radius-md)',
    border: '1px solid var(--panel-border)',
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: 'var(--space-3)',
    marginTop: 'var(--space-3)',
  },
  demoBox: {
    padding: 'var(--space-2)',
    backgroundColor: 'var(--color-background)',
    borderRadius: 'var(--border-radius-sm)',
    border: `1px solid var(--color-border)`,
  },
  tabsDemo: {
    height: '400px',
    marginTop: 'var(--space-3)',
  },
  menuBarDemo: {
    marginTop: 'var(--space-3)',
    border: `1px solid var(--panel-border)`,
    borderRadius: 'var(--border-radius-md)',
  },
  footerDemo: {
    marginTop: 'var(--space-3)',
    position: 'relative',
    height: '100px',
    border: `1px solid var(--panel-border)`,
    borderRadius: 'var(--border-radius-md)',
    backgroundColor: 'var(--color-background)',
  },
});

export function LayoutDemoPage() {
  const styles = useStyles();
  const [showLoading, setShowLoading] = useState(true);

  // Demo tabs
  const demoTabs: TabItem[] = [
    {
      id: 'properties',
      label: 'Properties',
      content: showLoading ? (
        <SkeletonPropertiesPanel />
      ) : (
        <FadeIn>
          <div style={{ padding: 'var(--space-2)' }}>
            <Title2>Properties Panel</Title2>
            <Text>This is the properties content.</Text>
          </div>
        </FadeIn>
      ),
    },
    {
      id: 'effects',
      label: 'Effects',
      content: showLoading ? (
        <SkeletonPanel />
      ) : (
        <FadeIn delay={100}>
          <div style={{ padding: 'var(--space-2)' }}>
            <Title2>Effects Panel</Title2>
            <Text>Effects library content goes here.</Text>
          </div>
        </FadeIn>
      ),
      closable: true,
    },
    {
      id: 'audio',
      label: 'Audio',
      content: (
        <FadeIn delay={200}>
          <div style={{ padding: 'var(--space-2)' }}>
            <Title2>Audio Panel</Title2>
            <Text>Audio mixing controls.</Text>
          </div>
        </FadeIn>
      ),
      closable: true,
    },
  ];

  return (
    <div className={styles.page}>
      <FadeIn>
        <Title1>Professional Layout Components Demo</Title1>
        <Text
          block
          style={{ marginBottom: 'var(--space-4)', color: 'var(--color-text-secondary)' }}
        >
          This page demonstrates all the new professional layout components and features.
        </Text>
      </FadeIn>

      {/* Top Menu Bar Section */}
      <FadeIn delay={100}>
        <section className={styles.section}>
          <Title2>Top Menu Bar</Title2>
          <Text
            block
            style={{ marginBottom: 'var(--space-2)', color: 'var(--color-text-secondary)' }}
          >
            Professional desktop-style menu bar with File, Edit, View, Window, and Help menus.
          </Text>
          <div className={styles.menuBarDemo}>
            <TopMenuBar
              onSaveProject={() => {
                /* Demo handler */
              }}
              onImportMedia={() => {
                /* Demo handler */
              }}
              onExportVideo={() => {
                /* Demo handler */
              }}
              onShowKeyboardShortcuts={() => {
                /* Demo handler */
              }}
            />
          </div>
        </section>
      </FadeIn>

      {/* Panel Tabs Section */}
      <FadeIn delay={200}>
        <section className={styles.section}>
          <Title2>Panel Tabs</Title2>
          <Text
            block
            style={{ marginBottom: 'var(--space-2)', color: 'var(--color-text-secondary)' }}
          >
            Tabbed interface with drag-to-reorder, close buttons, and smooth transitions.
          </Text>
          <div className={styles.tabsDemo}>
            <PanelTabs
              tabs={demoTabs}
              onTabClose={(id) => console.log('Close tab:', id)}
              onTabReorder={(tabs) => console.log('Reorder tabs:', tabs)}
            />
          </div>
        </section>
      </FadeIn>

      {/* Status Footer Section */}
      <FadeIn delay={300}>
        <section className={styles.section}>
          <Title2>Status Footer</Title2>
          <Text
            block
            style={{ marginBottom: 'var(--space-2)', color: 'var(--color-text-secondary)' }}
          >
            Project status information with toggle to show/hide.
          </Text>
          <div className={styles.footerDemo}>
            <StatusFooter
              projectName="Demo Project"
              resolution="1920x1080"
              frameRate={30}
              timecode="00:01:23:15"
            />
          </div>
        </section>
      </FadeIn>

      {/* Loading States Section */}
      <FadeIn delay={400}>
        <section className={styles.section}>
          <Title2>Professional Loading States</Title2>
          <Text
            block
            style={{ marginBottom: 'var(--space-2)', color: 'var(--color-text-secondary)' }}
          >
            Skeleton screens with shimmer effects for professional loading experience.
          </Text>
          <Button
            onClick={() => setShowLoading(!showLoading)}
            style={{ marginBottom: 'var(--space-3)' }}
          >
            {showLoading ? 'Show Content' : 'Show Loading State'}
          </Button>
          <div className={styles.grid}>
            <Card className={styles.demoBox}>
              <Title2 style={{ fontSize: '16px', marginBottom: 'var(--space-2)' }}>
                Skeleton Panel
              </Title2>
              <SkeletonPanel />
            </Card>
            <Card className={styles.demoBox}>
              <Title2 style={{ fontSize: '16px', marginBottom: 'var(--space-2)' }}>
                Media Item
              </Title2>
              <SkeletonMediaItem />
            </Card>
            <Card className={styles.demoBox}>
              <Title2 style={{ fontSize: '16px', marginBottom: 'var(--space-2)' }}>
                Timeline Item
              </Title2>
              <div style={{ display: 'flex', gap: 'var(--space-1)' }}>
                <SkeletonTimelineItem />
                <SkeletonTimelineItem />
                <SkeletonTimelineItem />
              </div>
            </Card>
            <Card className={styles.demoBox}>
              <Title2 style={{ fontSize: '16px', marginBottom: 'var(--space-2)' }}>
                Text Skeletons
              </Title2>
              <SkeletonTitle />
              <SkeletonText width="90%" />
              <SkeletonText width="85%" />
              <SkeletonText width="75%" />
              <div style={{ marginTop: 'var(--space-2)' }}>
                <SkeletonButton />
              </div>
            </Card>
          </div>
        </section>
      </FadeIn>

      {/* Theme Colors Section */}
      <FadeIn delay={500}>
        <section className={styles.section}>
          <Title2>Professional Dark Theme</Title2>
          <Text
            block
            style={{ marginBottom: 'var(--space-2)', color: 'var(--color-text-secondary)' }}
          >
            Carefully chosen color palette for professional NLE aesthetic.
          </Text>
          <div className={styles.grid}>
            <div className={styles.demoBox}>
              <Text weight="semibold">Background</Text>
              <div
                style={{
                  height: '50px',
                  backgroundColor: 'var(--color-background)',
                  border: '1px solid var(--color-border)',
                  borderRadius: 'var(--border-radius-sm)',
                  marginTop: 'var(--space-1)',
                }}
              />
              <Text size={200} style={{ color: 'var(--color-text-secondary)' }}>
                #0D0D0D
              </Text>
            </div>
            <div className={styles.demoBox}>
              <Text weight="semibold">Panel Background</Text>
              <div
                style={{
                  height: '50px',
                  backgroundColor: 'var(--panel-bg)',
                  border: '1px solid var(--color-border)',
                  borderRadius: 'var(--border-radius-sm)',
                  marginTop: 'var(--space-1)',
                }}
              />
              <Text size={200} style={{ color: 'var(--color-text-secondary)' }}>
                #1A1A1A
              </Text>
            </div>
            <div className={styles.demoBox}>
              <Text weight="semibold">Panel Header</Text>
              <div
                style={{
                  height: '50px',
                  backgroundColor: 'var(--panel-header-bg)',
                  border: '1px solid var(--color-border)',
                  borderRadius: 'var(--border-radius-sm)',
                  marginTop: 'var(--space-1)',
                }}
              />
              <Text size={200} style={{ color: 'var(--color-text-secondary)' }}>
                #252525
              </Text>
            </div>
            <div className={styles.demoBox}>
              <Text weight="semibold">Accent Blue</Text>
              <div
                style={{
                  height: '50px',
                  backgroundColor: 'var(--color-primary)',
                  borderRadius: 'var(--border-radius-sm)',
                  marginTop: 'var(--space-1)',
                }}
              />
              <Text size={200} style={{ color: 'var(--color-text-secondary)' }}>
                #0078D4
              </Text>
            </div>
          </div>
        </section>
      </FadeIn>
    </div>
  );
}
