/**
 * Templates Library page for browsing and selecting project templates
 */

import {
  makeStyles,
  tokens,
  Text,
  Title1,
  Title3,
  Button,
  TabList,
  Tab,
  Input,
  Spinner,
  MessageBar,
  MessageBarBody,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Field,
} from '@fluentui/react-components';
import { Search24Regular, Add24Regular, VideoClip24Regular } from '@fluentui/react-icons';
import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Virtuoso } from 'react-virtuoso';
import { TemplateCard } from '../../components/Templates/TemplateCard';
import { TemplatePreview } from '../../components/Templates/TemplatePreview';
import { useTemplatesPagination } from '../../hooks/useTemplatesPagination';
import { createFromTemplate, seedSampleTemplates } from '../../services/templatesService';
import { TemplateListItem, TemplateCategory } from '../../types/templates';

const useStyles = makeStyles({
  container: {
    maxWidth: '1400px',
    margin: '0 auto',
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalXXL,
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  searchBox: {
    width: '300px',
  },
  categoryTabs: {
    marginBottom: tokens.spacingVerticalL,
  },
  section: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  sectionTitle: {
    marginBottom: tokens.spacingVerticalL,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalL,
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXXL,
  },
  messageBar: {
    marginBottom: tokens.spacingVerticalL,
  },
});

export default function TemplatesLibrary() {
  const styles = useStyles();
  const navigate = useNavigate();

  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<'all' | TemplateCategory>('all');
  const [previewTemplate, setPreviewTemplate] = useState<TemplateListItem | null>(null);
  const [showPreview, setShowPreview] = useState(false);
  const [showNewProjectDialog, setShowNewProjectDialog] = useState(false);
  const [selectedTemplate, setSelectedTemplate] = useState<TemplateListItem | null>(null);
  const [projectName, setProjectName] = useState('');
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const {
    templates,
    loading,
    error,
    page,
    totalPages,
    totalCount,
    hasNextPage,
    loadNextPage,
    reload,
  } = useTemplatesPagination({
    category: selectedCategory,
    searchQuery,
    pageSize: 50,
  });

  const handleTemplateClick = (template: TemplateListItem) => {
    setSelectedTemplate(template);
    setProjectName(`${template.name} - ${new Date().toLocaleDateString()}`);
    setShowNewProjectDialog(true);
  };

  const handlePreview = (template: TemplateListItem) => {
    setPreviewTemplate(template);
    setShowPreview(true);
  };

  const handleUseTemplate = async () => {
    if (!selectedTemplate || !projectName.trim()) return;

    try {
      setCreating(true);
      setCreateError(null);
      const result = await createFromTemplate({
        templateId: selectedTemplate.id,
        projectName: projectName.trim(),
      });

      // Navigate to editor with the new project
      navigate('/editor', { state: { projectFile: result.projectFile } });
    } catch (err) {
      setCreateError('Failed to create project from template');
      console.error('Error creating project:', err);
    } finally {
      setCreating(false);
      setShowNewProjectDialog(false);
    }
  };

  // Group templates by subcategory for display
  const groupedTemplates = useMemo(() => {
    return templates.reduce(
      (acc, template) => {
        const key = template.subCategory || 'Other';
        if (!acc[key]) {
          acc[key] = [];
        }
        acc[key].push(template);
        return acc;
      },
      {} as Record<string, TemplateListItem[]>
    );
  }, [templates]);

  // Flatten grouped templates into rows for virtualization
  const rows = useMemo(() => {
    const result: Array<
      { type: 'header'; title: string } | { type: 'template'; template: TemplateListItem }
    > = [];
    Object.entries(groupedTemplates).forEach(([subcategory, items]) => {
      result.push({ type: 'header', title: subcategory });
      items.forEach((template) => {
        result.push({ type: 'template', template });
      });
    });
    return result;
  }, [groupedTemplates]);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <Title1>Templates Library</Title1>
          <Text>Get started quickly with pre-built project templates</Text>
        </div>
        <Button appearance="primary" icon={<Add24Regular />}>
          Save as Template
        </Button>
      </div>

      {error && (
        <MessageBar intent="error" className={styles.messageBar}>
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      <div className={styles.controls}>
        <Input
          className={styles.searchBox}
          placeholder="Search templates..."
          value={searchQuery}
          onChange={(_, data) => setSearchQuery(data.value)}
          contentBefore={<Search24Regular />}
        />
      </div>

      <TabList
        className={styles.categoryTabs}
        selectedValue={selectedCategory}
        onTabSelect={(_, data) => setSelectedCategory(data.value as 'all' | TemplateCategory)}
      >
        <Tab value="all">All Templates</Tab>
        <Tab value={TemplateCategory.YouTube}>YouTube</Tab>
        <Tab value={TemplateCategory.SocialMedia}>Social Media</Tab>
        <Tab value={TemplateCategory.Business}>Business</Tab>
        <Tab value={TemplateCategory.Creative}>Creative</Tab>
      </TabList>

      {loading && templates.length === 0 ? (
        <div className={styles.loadingContainer}>
          <Spinner size="large" label="Loading templates..." />
        </div>
      ) : templates.length === 0 ? (
        <div className={styles.emptyState}>
          <VideoClip24Regular style={{ fontSize: '48px' }} />
          <Title3>No templates found</Title3>
          <Text>Try adjusting your search or category filter</Text>
          <Button
            appearance="primary"
            onClick={async () => {
              await seedSampleTemplates();
              reload();
            }}
          >
            Load Sample Templates
          </Button>
        </div>
      ) : (
        <div style={{ height: '70vh' }}>
          <Virtuoso
            style={{ height: '100%' }}
            totalCount={rows.length}
            endReached={() => {
              if (hasNextPage && !loading) {
                loadNextPage();
              }
            }}
            itemContent={(index) => {
              const row = rows[index];
              if (row.type === 'header') {
                return (
                  <div className={styles.section}>
                    <Title3 className={styles.sectionTitle}>{row.title}</Title3>
                  </div>
                );
              } else {
                return (
                  <div style={{ padding: '8px' }}>
                    <TemplateCard
                      template={row.template}
                      onClick={handleTemplateClick}
                      onPreview={handlePreview}
                    />
                  </div>
                );
              }
            }}
            components={{
              Footer: () => {
                if (loading) {
                  return (
                    <div style={{ padding: '20px', textAlign: 'center' }}>
                      <Spinner size="small" label="Loading more..." />
                    </div>
                  );
                }
                if (totalPages > page) {
                  return (
                    <div style={{ padding: '20px', textAlign: 'center' }}>
                      <Text>
                        Page {page} of {totalPages} ({totalCount} templates)
                      </Text>
                    </div>
                  );
                }
                return null;
              },
            }}
          />
        </div>
      )}

      <TemplatePreview
        template={previewTemplate}
        open={showPreview}
        onClose={() => setShowPreview(false)}
        onUseTemplate={(template) => {
          setShowPreview(false);
          handleTemplateClick(template);
        }}
      />

      <Dialog
        open={showNewProjectDialog}
        onOpenChange={(_, data) => !data.open && setShowNewProjectDialog(false)}
      >
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Create Project from Template</DialogTitle>
            <DialogContent>
              {createError && (
                <MessageBar intent="error" className={styles.messageBar}>
                  <MessageBarBody>{createError}</MessageBarBody>
                </MessageBar>
              )}
              <Field label="Project Name" required>
                <Input
                  value={projectName}
                  onChange={(_, data) => setProjectName(data.value)}
                  placeholder="Enter project name"
                  disabled={creating}
                />
              </Field>
              {selectedTemplate && (
                <Text>
                  Template: <strong>{selectedTemplate.name}</strong>
                </Text>
              )}
            </DialogContent>
            <DialogActions>
              <Button
                appearance="secondary"
                onClick={() => setShowNewProjectDialog(false)}
                disabled={creating}
              >
                Cancel
              </Button>
              <Button
                appearance="primary"
                onClick={handleUseTemplate}
                disabled={!projectName.trim() || creating}
              >
                {creating ? <Spinner size="tiny" /> : 'Create Project'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
}
