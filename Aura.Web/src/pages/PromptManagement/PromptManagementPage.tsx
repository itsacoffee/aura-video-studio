import {
  Button,
  Card,
  Text,
  Title1,
  Title2,
  Title3,
  Spinner,
  makeStyles,
  tokens,
  Tab,
  TabList,
  Field,
  Input,
  Textarea,
  Dropdown,
  Option,
  Badge,
} from '@fluentui/react-components';
import {
  DocumentText24Regular,
  Add24Regular,
  Edit24Regular,
  Delete24Regular,
  History24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { ErrorState, SkeletonCard } from '../../components/Loading';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1400px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  headerIcon: {
    fontSize: '32px',
    color: tokens.colorBrandForeground1,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalS,
  },
  tabs: {
    marginBottom: tokens.spacingVerticalL,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  promptsList: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  promptCard: {
    padding: tokens.spacingVerticalL,
  },
  promptHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: tokens.spacingVerticalM,
  },
  promptActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  promptContent: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: '12px',
    fontFamily: 'monospace',
    maxHeight: '100px',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
  },
  promptMeta: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
    fontSize: '12px',
    color: tokens.colorNeutralForeground3,
  },
  editorCard: {
    padding: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  formActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
});

type TabValue = 'templates' | 'editor' | 'versions';

interface PromptTemplate {
  id: string;
  name: string;
  category: string;
  content: string;
  version: string;
  createdAt: string;
  updatedAt: string;
  isActive: boolean;
}

export const PromptManagementPage: React.FC = () => {
  const styles = useStyles();
  const [activeTab, setActiveTab] = useState<TabValue>('templates');
  const [templates, setTemplates] = useState<PromptTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingTemplate, setEditingTemplate] = useState<PromptTemplate | null>(null);
  const [templateName, setTemplateName] = useState('');
  const [templateCategory, setTemplateCategory] = useState('script-generation');
  const [templateContent, setTemplateContent] = useState('');

  const loadTemplates = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/prompt-management/templates');
      if (!response.ok) {
        throw new Error('Failed to load templates');
      }

      const data = await response.json();
      setTemplates(data.templates || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadTemplates();
  }, [loadTemplates]);

  const handleCreateTemplate = useCallback(async () => {
    if (!templateName || !templateContent) {
      setError('Name and content are required');
      return;
    }

    try {
      const response = await fetch('/api/prompt-management/templates', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: templateName,
          category: templateCategory,
          content: templateContent,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to create template');
      }

      setTemplateName('');
      setTemplateContent('');
      await loadTemplates();
      setActiveTab('templates');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create template');
    }
  }, [templateName, templateCategory, templateContent, loadTemplates]);

  const handleEditTemplate = useCallback((template: PromptTemplate) => {
    setEditingTemplate(template);
    setTemplateName(template.name);
    setTemplateCategory(template.category);
    setTemplateContent(template.content);
    setActiveTab('editor');
  }, []);

  const handleUpdateTemplate = useCallback(async () => {
    if (!editingTemplate || !templateName || !templateContent) {
      setError('Name and content are required');
      return;
    }

    try {
      const response = await fetch(`/api/prompt-management/templates/${editingTemplate.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: templateName,
          category: templateCategory,
          content: templateContent,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to update template');
      }

      setEditingTemplate(null);
      setTemplateName('');
      setTemplateContent('');
      await loadTemplates();
      setActiveTab('templates');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update template');
    }
  }, [editingTemplate, templateName, templateCategory, templateContent, loadTemplates]);

  const handleDeleteTemplate = useCallback(
    async (templateId: string) => {
      if (!confirm('Are you sure you want to delete this template?')) {
        return;
      }

      try {
        const response = await fetch(`/api/prompt-management/templates/${templateId}`, {
          method: 'DELETE',
        });

        if (!response.ok) {
          throw new Error('Failed to delete template');
        }

        await loadTemplates();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to delete template');
      }
    },
    [loadTemplates]
  );

  if (loading && templates.length === 0) {
    return (
      <div className={styles.container}>
        <SkeletonCard />
        <SkeletonCard />
        <SkeletonCard />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          <DocumentText24Regular className={styles.headerIcon} />
          <div>
            <Title1>Prompt Template Management</Title1>
            <Text className={styles.subtitle}>
              Create, edit, and manage AI prompt templates for content generation
            </Text>
          </div>
        </div>
        <Button
          appearance="primary"
          icon={<Add24Regular />}
          onClick={() => {
            setEditingTemplate(null);
            setTemplateName('');
            setTemplateContent('');
            setActiveTab('editor');
          }}
        >
          New Template
        </Button>
      </div>

      <TabList
        className={styles.tabs}
        selectedValue={activeTab}
        onTabSelect={(_, data) => setActiveTab(data.value as TabValue)}
      >
        <Tab value="templates" icon={<DocumentText24Regular />}>
          Templates
        </Tab>
        <Tab value="editor" icon={<Edit24Regular />}>
          Editor
        </Tab>
        <Tab value="versions" icon={<History24Regular />}>
          Version History
        </Tab>
      </TabList>

      {error && <ErrorState message={error} />}

      <div className={styles.content}>
        {activeTab === 'templates' && (
          <>
            <div>
              <Title2>Prompt Templates ({templates.length})</Title2>
              <Text>Manage your AI prompt templates for different content types</Text>
            </div>

            <div className={styles.promptsList}>
              {templates.map((template) => (
                <Card key={template.id} className={styles.promptCard}>
                  <div className={styles.promptHeader}>
                    <div>
                      <Title3>{template.name}</Title3>
                      <Badge appearance="tint">{template.category}</Badge>
                      {template.isActive && (
                        <Badge appearance="filled" color="success" style={{ marginLeft: '8px' }}>
                          Active
                        </Badge>
                      )}
                    </div>
                    <div className={styles.promptActions}>
                      <Button
                        size="small"
                        icon={<Edit24Regular />}
                        onClick={() => handleEditTemplate(template)}
                      />
                      <Button
                        size="small"
                        appearance="subtle"
                        icon={<Delete24Regular />}
                        onClick={() => handleDeleteTemplate(template.id)}
                      />
                    </div>
                  </div>

                  <div className={styles.promptContent}>{template.content}</div>

                  <div className={styles.promptMeta}>
                    <Text>Version: {template.version}</Text>
                    <Text>Updated: {new Date(template.updatedAt).toLocaleDateString()}</Text>
                  </div>
                </Card>
              ))}
            </div>
          </>
        )}

        {activeTab === 'editor' && (
          <Card className={styles.editorCard}>
            <Title2>{editingTemplate ? 'Edit Template' : 'Create New Template'}</Title2>

            <div className={styles.form}>
              <Field label="Template Name" required>
                <Input
                  value={templateName}
                  onChange={(_, data) => setTemplateName(data.value)}
                  placeholder="e.g., Educational Script Generator"
                />
              </Field>

              <Field label="Category" required>
                <Dropdown
                  value={templateCategory}
                  onOptionSelect={(_, data) => setTemplateCategory(data.optionValue as string)}
                >
                  <Option value="script-generation">Script Generation</Option>
                  <Option value="image-prompts">Image Prompts</Option>
                  <Option value="voice-direction">Voice Direction</Option>
                  <Option value="content-analysis">Content Analysis</Option>
                  <Option value="audience-adaptation">Audience Adaptation</Option>
                </Dropdown>
              </Field>

              <Field label="Prompt Template" required>
                <Textarea
                  value={templateContent}
                  onChange={(_, data) => setTemplateContent(data.value)}
                  placeholder="Enter your prompt template with variables like {{topic}}, {{audience}}, etc."
                  rows={12}
                />
              </Field>

              <div className={styles.formActions}>
                <Button
                  appearance="primary"
                  onClick={editingTemplate ? handleUpdateTemplate : handleCreateTemplate}
                  disabled={!templateName || !templateContent}
                >
                  {loading ? <Spinner size="tiny" /> : editingTemplate ? 'Update' : 'Create'}
                </Button>
                <Button
                  onClick={() => {
                    setEditingTemplate(null);
                    setTemplateName('');
                    setTemplateContent('');
                    setActiveTab('templates');
                  }}
                >
                  Cancel
                </Button>
              </div>
            </div>
          </Card>
        )}

        {activeTab === 'versions' && (
          <Card className={styles.editorCard}>
            <Title2>Version History</Title2>
            <Text>Version history feature coming soon</Text>
          </Card>
        )}
      </div>
    </div>
  );
};

export default PromptManagementPage;
