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
import {
  Search24Regular,
  Add24Regular,
  VideoClip24Regular,
  ChevronLeft24Regular,
  ChevronRight24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Virtuoso } from 'react-virtuoso';
import { TemplateCard } from '../../components/Templates/TemplateCard';
import { TemplatePreview } from '../../components/Templates/TemplatePreview';
import {
  createFromTemplate,
  seedSampleTemplates,
} from '../../services/templatesService';
import { TemplateListItem, TemplateCategory } from '../../types/templates';
import { usePaginatedTemplates } from '../../hooks/usePaginatedTemplates';

const useStyles = makeStyles({
  container: {
    maxWidth: '1400px',
    margin: '0 auto',
    padding: tokens.spacingVerticalL,
    height: '100vh',
    display: 'flex',
    flexDirection: 'column',
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
    padding: tokens.spacingVerticalM,
  },
  virtuosoContainer: {
    flex: 1,
    minHeight: 0,
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
  pagination: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalL,
    marginTop: tokens.spacingVerticalL,
  },
  paginationInfo: {
    color: tokens.colorNeutralForeground3,
  },
});

export default function TemplatesLibrary() {
  const styles = useStyles();
  const navigate = useNavigate();

  const [currentPage, setCurrentPage] = useState(1);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [previewTemplate, setPreviewTemplate] = useState<TemplateListItem | null>(null);
  const [showPreview, setShowPreview] = useState(false);
  const [showNewProjectDialog, setShowNewProjectDialog] = useState(false);
  const [selectedTemplate, setSelectedTemplate] = useState<TemplateListItem | null>(null);
  const [projectName, setProjectName] = useState('');
  const [creating, setCreating] = useState(false);
  const [seeded, setSeeded] = useState(false);

  const pageSize = 50;

  // Use paginated templates hook with abort support
  const {
    templates,
    totalCount,
    totalPages,
    hasNextPage,
    hasPreviousPage,
    loading,
    error: hookError,
    refetch,
  } = usePaginatedTemplates({
    page: currentPage,
    pageSize,
    category: selectedCategory as TemplateCategory | 'all',
  });

  const [error, setError] = useState<string | null>(null);

  // Combine hook error with local error
  useEffect(() => {
    if (hookError) {
      setError(hookError);
    }
  }, [hookError]);

  // Seed templates if none exist
  useEffect(() => {
    const seedIfNeeded = async () => {
      if (!loading && templates.length === 0 && totalCount === 0 && !seeded) {
        try {
          await seedSampleTemplates();
          setSeeded(true);
          refetch();
        } catch (err) {
          console.error('Error seeding templates:', err);
        }
      }
    };
    seedIfNeeded();
  }, [loading, templates.length, totalCount, seeded, refetch]);

  // Filter templates by search query (client-side filtering for current page)
  const filteredTemplates = useMemo(() => {
    if (!searchQuery) return templates;

    const query = searchQuery.toLowerCase();
    return templates.filter(
      (t) =>
        t.name.toLowerCase().includes(query) ||
        t.description.toLowerCase().includes(query) ||
        t.tags.some((tag) => tag.toLowerCase().includes(query))
    );
  }, [templates, searchQuery]);

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
      const result = await createFromTemplate({
        templateId: selectedTemplate.id,
        projectName: projectName.trim(),
      });

      // Navigate to editor with the new project
      navigate('/editor', { state: { projectFile: result.projectFile } });
    } catch (err) {
      setError('Failed to create project from template');
      console.error('Error creating project:', err);
    } finally {
      setCreating(false);
      setShowNewProjectDialog(false);
    }
  };

  const handlePageChange = useCallback((page: number) => {
    setCurrentPage(page);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, []);

  // Group templates into rows for virtual grid (4 items per row)
  const itemsPerRow = 4;
  const rows = useMemo(() => {
    const result: TemplateListItem[][] = [];
    for (let i = 0; i < filteredTemplates.length; i += itemsPerRow) {
      result.push(filteredTemplates.slice(i, i + itemsPerRow));
    }
    return result;
  }, [filteredTemplates]);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <Title1>Templates Library</Title1>
          <Text>
            Get started quickly with pre-built project templates ({totalCount} total)
          </Text>
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
        onTabSelect={(_, data) => {
          setSelectedCategory(data.value as string);
          setCurrentPage(1);
        }}
      >
        <Tab value="all">All Templates</Tab>
        <Tab value={TemplateCategory.YouTube}>YouTube</Tab>
        <Tab value={TemplateCategory.SocialMedia}>Social Media</Tab>
        <Tab value={TemplateCategory.Business}>Business</Tab>
        <Tab value={TemplateCategory.Creative}>Creative</Tab>
      </TabList>

      {loading && currentPage === 1 ? (
        <div className={styles.loadingContainer}>
          <Spinner size="large" label="Loading templates..." />
        </div>
      ) : filteredTemplates.length === 0 ? (
        <div className={styles.emptyState}>
          <VideoClip24Regular style={{ fontSize: '48px' }} />
          <Title3>No templates found</Title3>
          <Text>Try adjusting your search or category filter</Text>
        </div>
      ) : (
        <>
          <div className={styles.virtuosoContainer}>
            <Virtuoso
              data={rows}
              overscan={2}
              itemContent={(index, row) => (
                <div className={styles.grid}>
                  {row.map((template) => (
                    <TemplateCard
                      key={template.id}
                      template={template}
                      onClick={handleTemplateClick}
                      onPreview={handlePreview}
                    />
                  ))}
                </div>
              )}
            />
          </div>

          {totalPages > 1 && (
            <div className={styles.pagination}>
              <Button
                icon={<ChevronLeft24Regular />}
                appearance="subtle"
                disabled={!hasPreviousPage || loading}
                onClick={() => handlePageChange(currentPage - 1)}
              >
                Previous
              </Button>
              <Text className={styles.paginationInfo}>
                Page {currentPage} of {totalPages}
              </Text>
              <Button
                icon={<ChevronRight24Regular />}
                iconPosition="after"
                appearance="subtle"
                disabled={!hasNextPage || loading}
                onClick={() => handlePageChange(currentPage + 1)}
              >
                Next
              </Button>
            </div>
          )}
        </>
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
