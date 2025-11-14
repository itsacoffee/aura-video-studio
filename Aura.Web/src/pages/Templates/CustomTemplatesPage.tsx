/**
 * Custom Templates Management Page
 * Allows users to create, edit, delete, and manage their custom video templates
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
  Card,
  Menu,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
} from '@fluentui/react-components';
import {
  Search24Regular,
  Add24Regular,
  Edit24Regular,
  Delete24Regular,
  MoreVertical24Regular,
  ArrowDownload24Regular,
  ArrowUpload24Regular,
  Star24Regular,
  Star24Filled,
} from '@fluentui/react-icons';
import React, { useState, useEffect, useRef } from 'react';
import { CustomTemplateBuilder } from '../../components/Templates/CustomTemplateBuilder';
import {
  getCustomTemplates,
  createCustomTemplate,
  updateCustomTemplate,
  deleteCustomTemplate,
  duplicateCustomTemplate,
  setDefaultCustomTemplate,
  exportCustomTemplate,
  importCustomTemplate,
} from '../../services/customTemplatesService';
import type {
  CustomVideoTemplate,
  CreateCustomTemplateRequest,
  TemplateExportData,
} from '../../types/templates';

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
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  templateCard: {
    padding: tokens.spacingVerticalL,
    cursor: 'pointer',
    transition: 'transform 0.2s, box-shadow 0.2s',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow8,
    },
  },
  cardHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: tokens.spacingVerticalM,
  },
  cardTitle: {
    flex: 1,
  },
  cardMeta: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    color: tokens.colorNeutralForeground3,
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
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  defaultBadge: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    color: tokens.colorBrandForeground1,
  },
});

export default function CustomTemplatesPage() {
  const styles = useStyles();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [templates, setTemplates] = useState<CustomVideoTemplate[]>([]);
  const [filteredTemplates, setFilteredTemplates] = useState<CustomVideoTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');

  const [showBuilder, setShowBuilder] = useState(false);
  const [editingTemplate, setEditingTemplate] = useState<CustomVideoTemplate | null>(null);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [templateToDelete, setTemplateToDelete] = useState<CustomVideoTemplate | null>(null);

  useEffect(() => {
    loadTemplates();
  }, []);

  useEffect(() => {
    filterTemplates();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [templates, searchQuery, selectedCategory]);

  const loadTemplates = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getCustomTemplates();
      setTemplates(data);
    } catch (err) {
      setError('Failed to load custom templates');
      console.error('Error loading templates:', err);
    } finally {
      setLoading(false);
    }
  };

  const filterTemplates = () => {
    let filtered = templates;

    if (selectedCategory !== 'all') {
      filtered = filtered.filter((t) => t.category === selectedCategory);
    }

    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(
        (t) =>
          t.name.toLowerCase().includes(query) ||
          t.description.toLowerCase().includes(query) ||
          t.tags.some((tag) => tag.toLowerCase().includes(query))
      );
    }

    setFilteredTemplates(filtered);
  };

  const handleCreateNew = () => {
    setEditingTemplate(null);
    setShowBuilder(true);
  };

  const handleEdit = (template: CustomVideoTemplate) => {
    setEditingTemplate(template);
    setShowBuilder(true);
  };

  const handleSave = async (data: CreateCustomTemplateRequest) => {
    if (editingTemplate) {
      await updateCustomTemplate(editingTemplate.id, data);
    } else {
      await createCustomTemplate(data);
    }
    setShowBuilder(false);
    setEditingTemplate(null);
    await loadTemplates();
  };

  const handleDelete = (template: CustomVideoTemplate) => {
    setTemplateToDelete(template);
    setShowDeleteDialog(true);
  };

  const confirmDelete = async () => {
    if (templateToDelete) {
      try {
        await deleteCustomTemplate(templateToDelete.id);
        await loadTemplates();
        setShowDeleteDialog(false);
        setTemplateToDelete(null);
      } catch (err) {
        setError('Failed to delete template');
        console.error('Error deleting template:', err);
      }
    }
  };

  const handleDuplicate = async (template: CustomVideoTemplate) => {
    try {
      await duplicateCustomTemplate(template.id);
      await loadTemplates();
    } catch (err) {
      setError('Failed to duplicate template');
      console.error('Error duplicating template:', err);
    }
  };

  const handleSetDefault = async (template: CustomVideoTemplate) => {
    try {
      await setDefaultCustomTemplate(template.id);
      await loadTemplates();
    } catch (err) {
      setError('Failed to set default template');
      console.error('Error setting default:', err);
    }
  };

  const handleExport = async (template: CustomVideoTemplate) => {
    try {
      const exportData = await exportCustomTemplate(template.id);
      const blob = new Blob([JSON.stringify(exportData, null, 2)], {
        type: 'application/json',
      });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${template.name.replace(/\s+/g, '-')}.json`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      setError('Failed to export template');
      console.error('Error exporting template:', err);
    }
  };

  const handleImportClick = () => {
    fileInputRef.current?.click();
  };

  const handleImportFile = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    try {
      const text = await file.text();
      const data: TemplateExportData = JSON.parse(text);
      await importCustomTemplate(data);
      await loadTemplates();
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    } catch (err) {
      setError('Failed to import template. Please check the file format.');
      console.error('Error importing template:', err);
    }
  };

  const categories = ['all', ...new Set(templates.map((t) => t.category))];

  if (showBuilder) {
    return (
      <CustomTemplateBuilder
        initialTemplate={editingTemplate || undefined}
        onSave={handleSave}
        onCancel={() => {
          setShowBuilder(false);
          setEditingTemplate(null);
        }}
      />
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <Title1>Custom Templates</Title1>
          <Text>Create and manage your personalized video templates</Text>
        </div>
        <div className={styles.actions}>
          <Button
            appearance="secondary"
            icon={<ArrowUpload24Regular />}
            onClick={handleImportClick}
          >
            Import
          </Button>
          <Button appearance="primary" icon={<Add24Regular />} onClick={handleCreateNew}>
            Create Template
          </Button>
        </div>
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept=".json"
        style={{ display: 'none' }}
        onChange={handleImportFile}
      />

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
        selectedValue={selectedCategory}
        onTabSelect={(_, data) => setSelectedCategory(data.value as string)}
      >
        {categories.map((cat) => (
          <Tab key={cat} value={cat}>
            {cat === 'all' ? 'All' : cat}
          </Tab>
        ))}
      </TabList>

      {loading ? (
        <div className={styles.loadingContainer}>
          <Spinner size="large" label="Loading templates..." />
        </div>
      ) : filteredTemplates.length === 0 ? (
        <div className={styles.emptyState}>
          <Add24Regular style={{ fontSize: '48px' }} />
          <Title3>No custom templates yet</Title3>
          <Text>Create your first custom template to get started</Text>
          <Button appearance="primary" icon={<Add24Regular />} onClick={handleCreateNew}>
            Create Template
          </Button>
        </div>
      ) : (
        <div className={styles.grid}>
          {filteredTemplates.map((template) => (
            <Card key={template.id} className={styles.templateCard}>
              <div className={styles.cardHeader}>
                <div className={styles.cardTitle}>
                  <Title3>{template.name}</Title3>
                  {template.isDefault && (
                    <div className={styles.defaultBadge}>
                      <Star24Filled />
                      <Text size={200}>Default</Text>
                    </div>
                  )}
                </div>
                <Menu>
                  <MenuTrigger disableButtonEnhancement>
                    <Button appearance="subtle" icon={<MoreVertical24Regular />} />
                  </MenuTrigger>
                  <MenuPopover>
                    <MenuList>
                      <MenuItem icon={<Edit24Regular />} onClick={() => handleEdit(template)}>
                        Edit
                      </MenuItem>
                      <MenuItem icon={<Star24Regular />} onClick={() => handleDuplicate(template)}>
                        Duplicate
                      </MenuItem>
                      <MenuItem
                        icon={<Star24Filled />}
                        onClick={() => handleSetDefault(template)}
                        disabled={template.isDefault}
                      >
                        Set as Default
                      </MenuItem>
                      <MenuItem
                        icon={<ArrowDownload24Regular />}
                        onClick={() => handleExport(template)}
                      >
                        Export
                      </MenuItem>
                      <MenuItem icon={<Delete24Regular />} onClick={() => handleDelete(template)}>
                        Delete
                      </MenuItem>
                    </MenuList>
                  </MenuPopover>
                </Menu>
              </div>

              <Text style={{ marginBottom: tokens.spacingVerticalM }}>{template.description}</Text>

              <div className={styles.cardMeta}>
                <Text size={200}>Category: {template.category}</Text>
                <Text size={200}>Sections: {template.scriptStructure.sections.length}</Text>
                <Text size={200}>Duration: ~{template.videoStructure.typicalDuration}s</Text>
                <Text size={200}>Updated: {new Date(template.updatedAt).toLocaleDateString()}</Text>
              </div>
            </Card>
          ))}
        </div>
      )}

      <Dialog
        open={showDeleteDialog}
        onOpenChange={(_, data) => !data.open && setShowDeleteDialog(false)}
      >
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Delete Template</DialogTitle>
            <DialogContent>
              Are you sure you want to delete &quot;{templateToDelete?.name}&quot;? This action
              cannot be undone.
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setShowDeleteDialog(false)}>
                Cancel
              </Button>
              <Button appearance="primary" onClick={confirmDelete}>
                Delete
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
}
