import {
  Button,
  Input,
  Card,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Spinner,
  MessageBar,
  MessageBarBody,
  Badge,
  tokens,
  ProgressBar,
  Text,
  Field,
  Select,
} from '@fluentui/react-components';
import {
  AddRegular,
  ChartMultipleRegular,
  CheckmarkCircleRegular,
  DismissCircleRegular,
} from '@fluentui/react-icons';
import React, { useState, useEffect } from 'react';
import apiClient from '../../services/api/apiClient';

interface ABTest {
  testId: string;
  testName: string;
  description: string;
  category: string;
  status: 'draft' | 'running' | 'completed' | 'paused';
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  variants: ABTestVariant[];
  results?: ABTestResults;
}

interface ABTestVariant {
  variantId: string;
  variantName: string;
  description?: string;
  projectId?: string;
  videoId?: string;
}

interface ABTestResults {
  analyzedAt: string;
  winner?: string;
  confidence: number;
  isStatisticallySignificant: boolean;
  insights: string[];
}

export const ABTestManagementPage: React.FC = () => {
  const [tests, setTests] = useState<ABTest[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedTest, setSelectedTest] = useState<ABTest | null>(null);
  const [resultsDialogOpen, setResultsDialogOpen] = useState(false);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const profileId = 'default-profile';

  const [newTest, setNewTest] = useState({
    testName: '',
    description: '',
    category: 'general',
    variants: [
      { variantName: 'Variant A', description: '', projectId: '' },
      { variantName: 'Variant B', description: '', projectId: '' },
    ],
  });

  useEffect(() => {
    loadTests();
  }, []);

  const loadTests = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await apiClient.get<{ tests: ABTest[] }>(
        '/api/performance-analytics/ab-tests',
        {
          params: { profileId },
        }
      );
      setTests(response.data.tests || []);
    } catch (err) {
      setError('Failed to load A/B tests');
      console.error('Error loading tests:', err);
      setTests([]);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTest = async () => {
    try {
      setError(null);
      await apiClient.post('/api/performance-analytics/ab-test', {
        profileId,
        ...newTest,
      });
      setCreateDialogOpen(false);
      setNewTest({
        testName: '',
        description: '',
        category: 'general',
        variants: [
          { variantName: 'Variant A', description: '', projectId: '' },
          { variantName: 'Variant B', description: '', projectId: '' },
        ],
      });
      await loadTests();
    } catch (err) {
      setError('Failed to create A/B test');
      console.error('Error creating test:', err);
    }
  };

  const handleViewResults = async (test: ABTest) => {
    try {
      setSelectedTest(test);
      const response = await apiClient.get<{ test: ABTest }>(
        `/api/performance-analytics/ab-results/${test.testId}`,
        {
          params: { profileId },
        }
      );
      setSelectedTest(response.data.test);
      setResultsDialogOpen(true);
    } catch (err) {
      console.error('Error loading test results:', err);
    }
  };

  const getStatusBadge = (status: string) => {
    const colorMap: Record<string, 'success' | 'warning' | 'danger' | 'informative'> = {
      draft: 'informative',
      running: 'warning',
      completed: 'success',
      paused: 'danger',
    };
    return <Badge color={colorMap[status] || 'informative'}>{status.toUpperCase()}</Badge>;
  };

  if (loading) {
    return (
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          minHeight: '400px',
        }}
      >
        <Spinner size="large" label="Loading A/B tests..." />
      </div>
    );
  }

  return (
    <div style={{ padding: tokens.spacingVerticalXL }}>
      <div style={{ marginBottom: tokens.spacingVerticalXL }}>
        <h1>A/B Test Management</h1>
        <p style={{ color: tokens.colorNeutralForeground3 }}>
          Create and manage A/B tests to compare different video variations and optimize
          performance.
        </p>
      </div>

      {error && (
        <MessageBar intent="error" style={{ marginBottom: tokens.spacingVerticalM }}>
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      <div style={{ marginBottom: tokens.spacingVerticalL }}>
        <Button
          appearance="primary"
          icon={<AddRegular />}
          onClick={() => setCreateDialogOpen(true)}
        >
          Create New Test
        </Button>
      </div>

      <Card>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Test Name</TableHeaderCell>
              <TableHeaderCell>Status</TableHeaderCell>
              <TableHeaderCell>Category</TableHeaderCell>
              <TableHeaderCell>Variants</TableHeaderCell>
              <TableHeaderCell>Created</TableHeaderCell>
              <TableHeaderCell>Results</TableHeaderCell>
              <TableHeaderCell>Actions</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {tests.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={7}
                  style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}
                >
                  No A/B tests yet. Create one to start comparing video variants!
                </TableCell>
              </TableRow>
            ) : (
              tests.map((test) => (
                <TableRow key={test.testId}>
                  <TableCell>
                    <strong>{test.testName}</strong>
                    {test.description && (
                      <div style={{ fontSize: '0.875rem', color: tokens.colorNeutralForeground3 }}>
                        {test.description}
                      </div>
                    )}
                  </TableCell>
                  <TableCell>{getStatusBadge(test.status)}</TableCell>
                  <TableCell>{test.category}</TableCell>
                  <TableCell>{test.variants.length} variants</TableCell>
                  <TableCell>{new Date(test.createdAt).toLocaleDateString()}</TableCell>
                  <TableCell>
                    {test.results ? (
                      <div
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: tokens.spacingHorizontalS,
                        }}
                      >
                        {test.results.isStatisticallySignificant ? (
                          <CheckmarkCircleRegular
                            style={{ color: tokens.colorPaletteGreenForeground1 }}
                          />
                        ) : (
                          <DismissCircleRegular style={{ color: tokens.colorNeutralForeground3 }} />
                        )}
                        <span>{Math.round(test.results.confidence * 100)}% confidence</span>
                      </div>
                    ) : (
                      <span style={{ color: tokens.colorNeutralForeground3 }}>No results yet</span>
                    )}
                  </TableCell>
                  <TableCell>
                    <Button
                      size="small"
                      icon={<ChartMultipleRegular />}
                      appearance="subtle"
                      onClick={() => handleViewResults(test)}
                      disabled={!test.results}
                    >
                      View Results
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </Card>

      <Dialog open={createDialogOpen} onOpenChange={(_, data) => setCreateDialogOpen(data.open)}>
        <DialogSurface style={{ maxWidth: '600px' }}>
          <DialogBody>
            <DialogTitle>Create New A/B Test</DialogTitle>
            <DialogContent>
              <div
                style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}
              >
                <Field label="Test Name">
                  <Input
                    value={newTest.testName}
                    onChange={(e) => setNewTest({ ...newTest, testName: e.target.value })}
                    placeholder="e.g., Hook Comparison Test"
                  />
                </Field>
                <Field label="Description">
                  <Input
                    value={newTest.description}
                    onChange={(e) => setNewTest({ ...newTest, description: e.target.value })}
                    placeholder="Describe what you're testing..."
                  />
                </Field>
                <Field label="Category">
                  <Select
                    value={newTest.category}
                    onChange={(e) => setNewTest({ ...newTest, category: e.target.value })}
                  >
                    <option value="general">General</option>
                    <option value="hook">Hook</option>
                    <option value="pacing">Pacing</option>
                    <option value="voice">Voice</option>
                    <option value="visuals">Visuals</option>
                  </Select>
                </Field>
                {newTest.variants.map((variant, index) => (
                  <Card key={index}>
                    <Field label={`Variant ${String.fromCharCode(65 + index)} Name`}>
                      <Input
                        value={variant.variantName}
                        onChange={(e) => {
                          const updated = [...newTest.variants];
                          updated[index].variantName = e.target.value;
                          setNewTest({ ...newTest, variants: updated });
                        }}
                      />
                    </Field>
                    <Field label="Project ID">
                      <Input
                        value={variant.projectId}
                        onChange={(e) => {
                          const updated = [...newTest.variants];
                          updated[index].projectId = e.target.value;
                          setNewTest({ ...newTest, variants: updated });
                        }}
                        placeholder="Optional"
                      />
                    </Field>
                  </Card>
                ))}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setCreateDialogOpen(false)}>
                Cancel
              </Button>
              <Button appearance="primary" onClick={handleCreateTest} disabled={!newTest.testName}>
                Create Test
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      <Dialog open={resultsDialogOpen} onOpenChange={(_, data) => setResultsDialogOpen(data.open)}>
        <DialogSurface style={{ maxWidth: '700px' }}>
          <DialogBody>
            <DialogTitle>Test Results: {selectedTest?.testName}</DialogTitle>
            <DialogContent>
              {selectedTest?.results && (
                <div
                  style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalL }}
                >
                  <div>
                    <Text weight="semibold">Winner:</Text>
                    <div style={{ marginTop: tokens.spacingVerticalS }}>
                      <Badge size="large" color="success">
                        {selectedTest.results.winner || 'No clear winner'}
                      </Badge>
                    </div>
                  </div>
                  <div>
                    <Text weight="semibold">Confidence Level</Text>
                    <ProgressBar
                      value={selectedTest.results.confidence}
                      max={1}
                      style={{ marginTop: tokens.spacingVerticalS }}
                    />
                    <Text size={200}>{Math.round(selectedTest.results.confidence * 100)}%</Text>
                  </div>
                  <div>
                    <Text weight="semibold">Statistical Significance:</Text>
                    <div style={{ marginTop: tokens.spacingVerticalS }}>
                      {selectedTest.results.isStatisticallySignificant ? (
                        <Badge color="success">Statistically Significant</Badge>
                      ) : (
                        <Badge color="warning">Not Statistically Significant</Badge>
                      )}
                    </div>
                  </div>
                  <div>
                    <Text weight="semibold">Insights:</Text>
                    <ul style={{ marginTop: tokens.spacingVerticalS }}>
                      {selectedTest.results.insights.map((insight, idx) => (
                        <li key={idx}>{insight}</li>
                      ))}
                    </ul>
                  </div>
                  <div>
                    <Text weight="semibold">Analyzed At:</Text>
                    <div>{new Date(selectedTest.results.analyzedAt).toLocaleString()}</div>
                  </div>
                </div>
              )}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setResultsDialogOpen(false)}>
                Close
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
};

export default ABTestManagementPage;
