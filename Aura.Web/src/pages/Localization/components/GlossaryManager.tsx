/**
 * Glossary Manager Component
 * CRUD operations for translation glossaries with CSV import/export
 */

import {
  Card,
  Text,
  Title2,
  Title3,
  Button,
  makeStyles,
  tokens,
  Field,
  Input,
  Textarea,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Badge,
  Spinner,
} from '@fluentui/react-components';
import { Add24Regular, Delete24Regular, ArrowDownload24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { ErrorState } from '../../../components/Loading';
import {
  listGlossaries,
  createGlossary,
  deleteGlossary,
  addGlossaryEntry,
  getGlossary,
} from '../../../services/api/localizationApi';
import type {
  ProjectGlossaryDto,
  CreateGlossaryRequest,
  AddGlossaryEntryRequest,
} from '../../../types/api-v1';

const useStyles = makeStyles({
  card: {
    padding: tokens.spacingVerticalXL,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
  },
  glossaryList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  glossaryCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  glossaryHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: tokens.spacingVerticalM,
  },
  glossaryActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  entriesTable: {
    marginTop: tokens.spacingVerticalM,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  translationsInput: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  translationRow: {
    display: 'grid',
    gridTemplateColumns: '150px 1fr auto',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
});

export const GlossaryManager: React.FC = () => {
  const styles = useStyles();
  const [glossaries, setGlossaries] = useState<ProjectGlossaryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedGlossary, setSelectedGlossary] = useState<ProjectGlossaryDto | null>(null);

  // Create glossary dialog state
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [newGlossaryName, setNewGlossaryName] = useState('');
  const [newGlossaryDesc, setNewGlossaryDesc] = useState('');
  const [creating, setCreating] = useState(false);

  // Add entry dialog state
  const [addEntryDialogOpen, setAddEntryDialogOpen] = useState(false);
  const [newTerm, setNewTerm] = useState('');
  const [newTranslations, setNewTranslations] = useState<Record<string, string>>({
    en: '',
    es: '',
  });
  const [newContext, setNewContext] = useState('');
  const [addingEntry, setAddingEntry] = useState(false);

  useEffect(() => {
    loadGlossaries();
  }, []);

  const loadGlossaries = async () => {
    setLoading(true);
    setError(null);
    try {
      const glossariesData = await listGlossaries();
      setGlossaries(glossariesData);
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to load glossaries';
      setError(errorMsg);
      console.error('Error loading glossaries:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateGlossary = async () => {
    if (!newGlossaryName) {
      setError('Glossary name is required');
      return;
    }

    setCreating(true);
    setError(null);

    try {
      const request: CreateGlossaryRequest = {
        name: newGlossaryName,
        description: newGlossaryDesc || undefined,
      };
      await createGlossary(request);
      await loadGlossaries();
      setCreateDialogOpen(false);
      setNewGlossaryName('');
      setNewGlossaryDesc('');
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to create glossary';
      setError(errorMsg);
      console.error('Error creating glossary:', err);
    } finally {
      setCreating(false);
    }
  };

  const handleDeleteGlossary = async (glossaryId: string) => {
    try {
      await deleteGlossary(glossaryId);
      await loadGlossaries();
      if (selectedGlossary?.id === glossaryId) {
        setSelectedGlossary(null);
      }
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to delete glossary';
      setError(errorMsg);
      console.error('Error deleting glossary:', err);
    }
  };

  const handleAddEntry = async () => {
    if (!selectedGlossary || !newTerm) {
      setError('Term is required');
      return;
    }

    setAddingEntry(true);
    setError(null);

    try {
      const request: AddGlossaryEntryRequest = {
        term: newTerm,
        translations: newTranslations,
        context: newContext || undefined,
      };
      await addGlossaryEntry(selectedGlossary.id, request);

      // Reload glossary to get updated entries
      const updated = await getGlossary(selectedGlossary.id);
      setSelectedGlossary(updated);

      // Update in list
      await loadGlossaries();

      setAddEntryDialogOpen(false);
      setNewTerm('');
      setNewTranslations({ en: '', es: '' });
      setNewContext('');
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to add entry';
      setError(errorMsg);
      console.error('Error adding entry:', err);
    } finally {
      setAddingEntry(false);
    }
  };

  const exportToCSV = (glossary: ProjectGlossaryDto) => {
    // Get all language codes from all entries
    const languages = new Set<string>();
    glossary.entries.forEach((entry) => {
      Object.keys(entry.translations).forEach((lang) => languages.add(lang));
    });
    const langArray = Array.from(languages).sort();

    // Build CSV header
    const header = ['Term', ...langArray, 'Context', 'Industry'].join(',');

    // Build CSV rows
    const rows = glossary.entries.map((entry) => {
      const translations = langArray.map((lang) => {
        const value = entry.translations[lang] || '';
        return `"${value.replace(/"/g, '""')}"`;
      });
      return [
        `"${entry.term.replace(/"/g, '""')}"`,
        ...translations,
        `"${(entry.context || '').replace(/"/g, '""')}"`,
        `"${(entry.industry || '').replace(/"/g, '""')}"`,
      ].join(',');
    });

    const csv = [header, ...rows].join('\n');

    // Create download
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${glossary.name.replace(/[^a-z0-9]/gi, '_')}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const updateTranslation = (lang: string, value: string) => {
    setNewTranslations((prev) => ({ ...prev, [lang]: value }));
  };

  const addTranslationLanguage = () => {
    const newLang = prompt('Enter language code (e.g., fr, de, zh):');
    if (newLang && !newTranslations[newLang]) {
      updateTranslation(newLang, '');
    }
  };

  if (loading) {
    return (
      <Card className={styles.card}>
        <Spinner label="Loading glossaries..." />
      </Card>
    );
  }

  return (
    <Card className={styles.card}>
      <Title2>Glossary Management</Title2>
      <Text>Manage translation glossaries and terminology</Text>

      {error && <ErrorState message={error} />}

      <div className={styles.actions}>
        <Dialog open={createDialogOpen} onOpenChange={(_, data) => setCreateDialogOpen(data.open)}>
          <DialogTrigger disableButtonEnhancement>
            <Button icon={<Add24Regular />} appearance="primary">
              Create Glossary
            </Button>
          </DialogTrigger>
          <DialogSurface>
            <DialogBody>
              <DialogTitle>Create New Glossary</DialogTitle>
              <DialogContent>
                <div className={styles.form}>
                  <Field label="Glossary Name" required>
                    <Input
                      value={newGlossaryName}
                      onChange={(_, data) => setNewGlossaryName(data.value)}
                      placeholder="e.g., Medical Terms, Marketing Copy"
                    />
                  </Field>
                  <Field label="Description">
                    <Textarea
                      value={newGlossaryDesc}
                      onChange={(_, data) => setNewGlossaryDesc(data.value)}
                      placeholder="Optional description..."
                      rows={3}
                    />
                  </Field>
                </div>
              </DialogContent>
              <DialogActions>
                <DialogTrigger disableButtonEnhancement>
                  <Button appearance="secondary">Cancel</Button>
                </DialogTrigger>
                <Button
                  appearance="primary"
                  onClick={handleCreateGlossary}
                  disabled={creating || !newGlossaryName}
                >
                  {creating ? <Spinner size="tiny" /> : 'Create'}
                </Button>
              </DialogActions>
            </DialogBody>
          </DialogSurface>
        </Dialog>
      </div>

      {glossaries.length === 0 ? (
        <Card>
          <Text>No glossaries created yet. Create one to get started.</Text>
        </Card>
      ) : (
        <div className={styles.glossaryList}>
          {glossaries.map((glossary) => (
            <div key={glossary.id} className={styles.glossaryCard}>
              <div className={styles.glossaryHeader}>
                <div>
                  <Title3>{glossary.name}</Title3>
                  {glossary.description && (
                    <Text style={{ color: tokens.colorNeutralForeground3 }}>
                      {glossary.description}
                    </Text>
                  )}
                  <div style={{ marginTop: tokens.spacingVerticalS }}>
                    <Badge appearance="tint">{glossary.entries.length} entries</Badge>
                  </div>
                </div>
                <div className={styles.glossaryActions}>
                  <Button
                    icon={<Add24Regular />}
                    size="small"
                    onClick={() => {
                      setSelectedGlossary(glossary);
                      setAddEntryDialogOpen(true);
                    }}
                  >
                    Add Entry
                  </Button>
                  <Button
                    icon={<ArrowDownload24Regular />}
                    size="small"
                    onClick={() => exportToCSV(glossary)}
                  >
                    Export CSV
                  </Button>
                  <Button
                    icon={<Delete24Regular />}
                    size="small"
                    appearance="subtle"
                    onClick={() => handleDeleteGlossary(glossary.id)}
                  >
                    Delete
                  </Button>
                </div>
              </div>

              {glossary.entries.length > 0 && (
                <div className={styles.entriesTable}>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHeaderCell>Term</TableHeaderCell>
                        <TableHeaderCell>Translations</TableHeaderCell>
                        <TableHeaderCell>Context</TableHeaderCell>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {glossary.entries.slice(0, 5).map((entry) => (
                        <TableRow key={entry.id}>
                          <TableCell>
                            <Text weight="semibold">{entry.term}</Text>
                          </TableCell>
                          <TableCell>
                            {Object.entries(entry.translations).map(([lang, trans]) => (
                              <Badge
                                key={lang}
                                appearance="outline"
                                style={{ marginRight: tokens.spacingHorizontalXS }}
                              >
                                {lang}: {trans}
                              </Badge>
                            ))}
                          </TableCell>
                          <TableCell>
                            <Text style={{ fontSize: '13px' }}>{entry.context || '-'}</Text>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                  {glossary.entries.length > 5 && (
                    <Text
                      style={{
                        marginTop: tokens.spacingVerticalS,
                        fontSize: '13px',
                        color: tokens.colorNeutralForeground3,
                      }}
                    >
                      ... and {glossary.entries.length - 5} more entries
                    </Text>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Add Entry Dialog */}
      <Dialog
        open={addEntryDialogOpen}
        onOpenChange={(_, data) => setAddEntryDialogOpen(data.open)}
      >
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Add Glossary Entry</DialogTitle>
            <DialogContent>
              <div className={styles.form}>
                <Field label="Term" required>
                  <Input
                    value={newTerm}
                    onChange={(_, data) => setNewTerm(data.value)}
                    placeholder="e.g., Video Generation"
                  />
                </Field>

                <Field label="Translations">
                  <div className={styles.translationsInput}>
                    {Object.keys(newTranslations).map((lang) => (
                      <div key={lang} className={styles.translationRow}>
                        <Text weight="semibold">{lang.toUpperCase()}:</Text>
                        <Input
                          value={newTranslations[lang]}
                          onChange={(_, data) => updateTranslation(lang, data.value)}
                          placeholder={`Translation in ${lang}...`}
                        />
                      </div>
                    ))}
                    <Button size="small" onClick={addTranslationLanguage}>
                      + Add Language
                    </Button>
                  </div>
                </Field>

                <Field label="Context">
                  <Textarea
                    value={newContext}
                    onChange={(_, data) => setNewContext(data.value)}
                    placeholder="Usage context or notes..."
                    rows={2}
                  />
                </Field>
              </div>
            </DialogContent>
            <DialogActions>
              <DialogTrigger disableButtonEnhancement>
                <Button appearance="secondary">Cancel</Button>
              </DialogTrigger>
              <Button
                appearance="primary"
                onClick={handleAddEntry}
                disabled={addingEntry || !newTerm}
              >
                {addingEntry ? <Spinner size="tiny" /> : 'Add Entry'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </Card>
  );
};
