/**
 * TemplatesPanel Component
 *
 * Template browser panel with category filter tabs, search functionality,
 * and a template grid with thumbnails and hover previews.
 */

import {
  makeStyles,
  tokens,
  Text,
  Input,
  mergeClasses,
  TabList,
  Tab,
  Button,
  Tooltip,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@fluentui/react-components';
import {
  Search24Regular,
  DocumentBulletList24Regular,
  Add24Regular,
  ArrowDownload24Regular,
  ArrowUpload24Regular,
  MoreHorizontal24Regular,
  Dismiss24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, useMemo, useRef } from 'react';
import type { FC } from 'react';
import {
  useTemplatesStore,
  type Template,
  type TemplateData,
  type TemplateTrackType,
} from '../../../stores/opencutTemplates';
import { useOpenCutTimelineStore } from '../../../stores/opencutTimeline';
import { openCutTokens } from '../../../styles/designTokens';
import { EmptyState } from '../EmptyState';
import { SaveTemplateDialog } from './SaveTemplateDialog';
import { TemplateCard } from './TemplateCard';
import { TemplatePreview } from './TemplatePreview';

export interface TemplatesPanelProps {
  className?: string;
  onTemplateApply?: (templateData: TemplateData) => void;
}

type CategoryTab = 'all' | string;

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${openCutTokens.spacing.md} ${openCutTokens.spacing.md}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    minHeight: '56px',
    gap: openCutTokens.spacing.sm,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
  },
  headerIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '20px',
  },
  headerActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  toolbar: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  searchInput: {
    width: '100%',
  },
  tabList: {
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
  },
  tab: {
    minWidth: 'auto',
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    fontSize: tokens.fontSizeBase200,
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingHorizontalM,
  },
  templatesGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  categorySection: {
    marginBottom: tokens.spacingVerticalL,
  },
  categoryHeader: {
    marginBottom: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground2,
  },
  previewDialog: {
    maxWidth: '500px',
    maxHeight: '80vh',
    overflow: 'auto',
  },
  importExportDialog: {
    maxWidth: '600px',
  },
  jsonTextarea: {
    width: '100%',
    minHeight: '200px',
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    padding: tokens.spacingHorizontalM,
    backgroundColor: tokens.colorNeutralBackground4,
    border: `1px solid ${tokens.colorNeutralStroke3}`,
    borderRadius: openCutTokens.radius.md,
    resize: 'vertical',
  },
  importError: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalS,
  },
});

export const TemplatesPanel: FC<TemplatesPanelProps> = ({ className, onTemplateApply }) => {
  const styles = useStyles();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<CategoryTab>('all');
  const [selectedTemplateId, setSelectedTemplateId] = useState<string | null>(null);
  const [previewTemplate, setPreviewTemplate] = useState<Template | null>(null);
  const [showSaveDialog, setShowSaveDialog] = useState(false);
  const [showImportDialog, setShowImportDialog] = useState(false);
  const [showExportDialog, setShowExportDialog] = useState(false);
  const [importJson, setImportJson] = useState('');
  const [exportJson, setExportJson] = useState('');
  const [importError, setImportError] = useState<string | null>(null);

  const templatesStore = useTemplatesStore();
  const timelineStore = useOpenCutTimelineStore();

  // Get all templates
  const allTemplates = useMemo(() => templatesStore.getAllTemplates(), [templatesStore]);

  // Filter templates based on search and category
  const filteredTemplates = useMemo(() => {
    let templates = allTemplates;

    // Filter by category
    if (selectedCategory !== 'all') {
      templates = templates.filter((t) => t.category === selectedCategory);
    }

    // Filter by search query
    if (searchQuery) {
      templates = templatesStore.searchTemplates(searchQuery);
      if (selectedCategory !== 'all') {
        templates = templates.filter((t) => t.category === selectedCategory);
      }
    }

    return templates;
  }, [allTemplates, selectedCategory, searchQuery, templatesStore]);

  // Group templates by category when showing all
  const groupedTemplates = useMemo(() => {
    if (selectedCategory !== 'all') {
      return { [selectedCategory]: filteredTemplates };
    }

    const groups: Record<string, Template[]> = {};
    filteredTemplates.forEach((t) => {
      if (!groups[t.category]) {
        groups[t.category] = [];
      }
      groups[t.category].push(t);
    });
    return groups;
  }, [filteredTemplates, selectedCategory]);

  // Get all categories
  const categories = useMemo(() => templatesStore.getCategories(), [templatesStore]);

  const handleCategoryChange = useCallback((_: unknown, data: { value: string }) => {
    setSelectedCategory(data.value as CategoryTab);
  }, []);

  const handleTemplateClick = useCallback((template: Template) => {
    setSelectedTemplateId(template.id);
    setPreviewTemplate(template);
  }, []);

  const handleApplyTemplate = useCallback(
    (templateId: string) => {
      const data = templatesStore.applyTemplate(templateId);
      if (data) {
        onTemplateApply?.(data);
      }
    },
    [templatesStore, onTemplateApply]
  );

  const handleDuplicateTemplate = useCallback(
    (templateId: string) => {
      templatesStore.duplicateTemplate(templateId);
    },
    [templatesStore]
  );

  const handleDeleteTemplate = useCallback(
    (templateId: string) => {
      templatesStore.deleteTemplate(templateId);
      if (selectedTemplateId === templateId) {
        setSelectedTemplateId(null);
        setPreviewTemplate(null);
      }
    },
    [templatesStore, selectedTemplateId]
  );

  const handleSaveCurrentAsTemplate = useCallback(() => {
    setShowSaveDialog(true);
  }, []);

  const getCurrentProjectData = useCallback((): TemplateData => {
    const { tracks, clips } = timelineStore;

    // Helper to validate and convert track type
    const toTemplateTrackType = (type: string): TemplateTrackType => {
      const validTypes: TemplateTrackType[] = ['video', 'audio', 'image', 'text'];
      return validTypes.includes(type as TemplateTrackType) ? (type as TemplateTrackType) : 'video'; // Default fallback
    };

    return {
      tracks: tracks.map((t) => ({
        id: t.id,
        type: toTemplateTrackType(t.type),
        name: t.name,
        order: t.order,
        height: t.height,
        muted: t.muted,
        solo: t.solo,
        locked: t.locked,
        visible: t.visible,
      })),
      clips: clips.map((c) => ({
        id: c.id,
        trackId: c.trackId,
        type: toTemplateTrackType(c.type),
        name: c.name,
        startTime: c.startTime,
        duration: c.duration,
        isPlaceholder: false,
      })),
      effects: [],
      transitions: [],
      markers: [],
    };
  }, [timelineStore]);

  const handleImportTemplate = useCallback(() => {
    setImportError(null);
    const result = templatesStore.importTemplate(importJson);
    if (result) {
      setShowImportDialog(false);
      setImportJson('');
    } else {
      setImportError('Invalid template JSON. Please check the format and try again.');
    }
  }, [templatesStore, importJson]);

  const handleExportTemplate = useCallback(
    (templateId: string) => {
      const json = templatesStore.exportTemplate(templateId);
      setExportJson(json);
      setShowExportDialog(true);
    },
    [templatesStore]
  );

  const handleCopyToClipboard = useCallback(() => {
    navigator.clipboard.writeText(exportJson);
  }, [exportJson]);

  const handleFileImport = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e) => {
        const content = e.target?.result as string;
        setImportJson(content);
        setShowImportDialog(true);
      };
      reader.readAsText(file);
    }
    // Reset file input
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  }, []);

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <DocumentBulletList24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Templates
          </Text>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            ({allTemplates.length})
          </Text>
        </div>
        <div className={styles.headerActions}>
          <Tooltip content="Save current as template" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<Add24Regular />}
              onClick={handleSaveCurrentAsTemplate}
            />
          </Tooltip>
          <Menu>
            <MenuTrigger>
              <Tooltip content="More options" relationship="label">
                <Button appearance="subtle" size="small" icon={<MoreHorizontal24Regular />} />
              </Tooltip>
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                <MenuItem
                  icon={<ArrowDownload24Regular />}
                  onClick={() => fileInputRef.current?.click()}
                >
                  Import Template
                </MenuItem>
                {selectedTemplateId && (
                  <MenuItem
                    icon={<ArrowUpload24Regular />}
                    onClick={() => handleExportTemplate(selectedTemplateId)}
                  >
                    Export Selected
                  </MenuItem>
                )}
              </MenuList>
            </MenuPopover>
          </Menu>
        </div>
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept=".json"
        style={{ display: 'none' }}
        onChange={handleFileImport}
      />

      <div className={styles.toolbar}>
        <Input
          className={styles.searchInput}
          contentBefore={<Search24Regular />}
          placeholder="Search templates..."
          size="small"
          value={searchQuery}
          onChange={(_, data) => setSearchQuery(data.value)}
        />
        <TabList
          className={styles.tabList}
          selectedValue={selectedCategory}
          onTabSelect={handleCategoryChange}
          size="small"
        >
          <Tab value="all" className={styles.tab}>
            All
          </Tab>
          {categories.map((cat) => (
            <Tab key={cat} value={cat} className={styles.tab}>
              {cat}
            </Tab>
          ))}
        </TabList>
      </div>

      <div className={styles.content}>
        {filteredTemplates.length === 0 ? (
          <EmptyState
            icon={<DocumentBulletList24Regular />}
            title="No templates found"
            description={
              searchQuery
                ? 'Try a different search term'
                : 'Create a template by saving your current project'
            }
            size="small"
          />
        ) : selectedCategory === 'all' ? (
          // Show grouped by category
          Object.entries(groupedTemplates).map(([category, templates]) => (
            <div key={category} className={styles.categorySection}>
              <Text weight="semibold" size={200} className={styles.categoryHeader}>
                {category}
              </Text>
              <div className={styles.templatesGrid}>
                {templates.map((template) => (
                  <TemplateCard
                    key={template.id}
                    template={template}
                    isSelected={selectedTemplateId === template.id}
                    onClick={() => handleTemplateClick(template)}
                    onApply={() => handleApplyTemplate(template.id)}
                    onDuplicate={() => handleDuplicateTemplate(template.id)}
                    onDelete={
                      !template.isBuiltin ? () => handleDeleteTemplate(template.id) : undefined
                    }
                  />
                ))}
              </div>
            </div>
          ))
        ) : (
          // Show flat list for specific category
          <div className={styles.templatesGrid}>
            {filteredTemplates.map((template) => (
              <TemplateCard
                key={template.id}
                template={template}
                isSelected={selectedTemplateId === template.id}
                onClick={() => handleTemplateClick(template)}
                onApply={() => handleApplyTemplate(template.id)}
                onDuplicate={() => handleDuplicateTemplate(template.id)}
                onDelete={!template.isBuiltin ? () => handleDeleteTemplate(template.id) : undefined}
              />
            ))}
          </div>
        )}
      </div>

      {/* Preview Dialog */}
      <Dialog
        open={previewTemplate !== null}
        onOpenChange={(_, data) => !data.open && setPreviewTemplate(null)}
      >
        <DialogSurface className={styles.previewDialog}>
          <DialogTitle
            action={
              <Button
                appearance="subtle"
                icon={<Dismiss24Regular />}
                onClick={() => setPreviewTemplate(null)}
                aria-label="Close"
              />
            }
          >
            Template Preview
          </DialogTitle>
          <DialogBody>
            <DialogContent>
              {previewTemplate && <TemplatePreview template={previewTemplate} />}
            </DialogContent>
          </DialogBody>
          <DialogActions>
            <Button appearance="secondary" onClick={() => setPreviewTemplate(null)}>
              Close
            </Button>
            <Button
              appearance="primary"
              onClick={() => {
                if (previewTemplate) {
                  handleApplyTemplate(previewTemplate.id);
                  setPreviewTemplate(null);
                }
              }}
            >
              Apply Template
            </Button>
          </DialogActions>
        </DialogSurface>
      </Dialog>

      {/* Save Template Dialog */}
      <SaveTemplateDialog
        open={showSaveDialog}
        onClose={() => setShowSaveDialog(false)}
        templateData={getCurrentProjectData()}
        aspectRatio="16:9"
        onSaved={(id) => {
          setSelectedTemplateId(id);
        }}
      />

      {/* Import Dialog */}
      <Dialog
        open={showImportDialog}
        onOpenChange={(_, data) => {
          if (!data.open) {
            setShowImportDialog(false);
            setImportError(null);
          }
        }}
      >
        <DialogSurface className={styles.importExportDialog}>
          <DialogTitle
            action={
              <Button
                appearance="subtle"
                icon={<Dismiss24Regular />}
                onClick={() => setShowImportDialog(false)}
                aria-label="Close"
              />
            }
          >
            Import Template
          </DialogTitle>
          <DialogBody>
            <DialogContent>
              <Text block style={{ marginBottom: tokens.spacingVerticalS }}>
                Paste the template JSON below:
              </Text>
              <textarea
                className={styles.jsonTextarea}
                value={importJson}
                onChange={(e) => {
                  setImportJson(e.target.value);
                  setImportError(null);
                }}
                placeholder='{"id": "...", "name": "...", ...}'
              />
              {importError && <Text className={styles.importError}>{importError}</Text>}
            </DialogContent>
          </DialogBody>
          <DialogActions>
            <Button appearance="secondary" onClick={() => setShowImportDialog(false)}>
              Cancel
            </Button>
            <Button
              appearance="primary"
              onClick={handleImportTemplate}
              disabled={!importJson.trim()}
            >
              Import
            </Button>
          </DialogActions>
        </DialogSurface>
      </Dialog>

      {/* Export Dialog */}
      <Dialog
        open={showExportDialog}
        onOpenChange={(_, data) => !data.open && setShowExportDialog(false)}
      >
        <DialogSurface className={styles.importExportDialog}>
          <DialogTitle
            action={
              <Button
                appearance="subtle"
                icon={<Dismiss24Regular />}
                onClick={() => setShowExportDialog(false)}
                aria-label="Close"
              />
            }
          >
            Export Template
          </DialogTitle>
          <DialogBody>
            <DialogContent>
              <Text block style={{ marginBottom: tokens.spacingVerticalS }}>
                Copy this JSON to share the template:
              </Text>
              <textarea
                className={styles.jsonTextarea}
                value={exportJson}
                readOnly
                onClick={(e) => (e.target as HTMLTextAreaElement).select()}
              />
            </DialogContent>
          </DialogBody>
          <DialogActions>
            <Button appearance="secondary" onClick={() => setShowExportDialog(false)}>
              Close
            </Button>
            <Button appearance="primary" onClick={handleCopyToClipboard}>
              Copy to Clipboard
            </Button>
          </DialogActions>
        </DialogSurface>
      </Dialog>
    </div>
  );
};

export default TemplatesPanel;
