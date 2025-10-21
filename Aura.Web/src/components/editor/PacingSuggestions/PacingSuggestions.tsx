/**
 * Pacing Suggestions Component
 * Displays AI-driven pacing and rhythm optimization recommendations
 */

import React, { useState, useCallback, useEffect } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Typography,
  Alert,
  CircularProgress,
  Chip,
  Divider,
  List,
  ListItem,
  ListItemText,
  Stack,
  LinearProgress,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from '@mui/material';
import {
  ExpandMore,
  Timeline,
  Speed,
  TrendingUp,
  Warning,
  CheckCircle,
  Lightbulb,
} from '@mui/icons-material';
import { Scene } from '../../../types';
import {
  pacingAnalysisService,
  VideoFormat,
  PacingAnalysisResult,
  VideoRetentionAnalysis,
  Priority,
} from '../../../services/analysis/PacingAnalysisService';

interface PacingSuggestionsProps {
  scenes: Scene[];
  audioPath?: string;
  onApplySuggestion?: (sceneIndex: number, newDuration: string) => void;
}

const PacingSuggestions: React.FC<PacingSuggestionsProps> = ({
  scenes,
  audioPath,
  onApplySuggestion,
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedFormat, setSelectedFormat] = useState<VideoFormat>(VideoFormat.Explainer);
  const [pacingAnalysis, setPacingAnalysis] = useState<PacingAnalysisResult | null>(null);
  const [retentionAnalysis, setRetentionAnalysis] = useState<VideoRetentionAnalysis | null>(null);

  const analyzePacing = useCallback(async () => {
    if (scenes.length === 0) {
      setError('No scenes available for analysis');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const [pacing, retention] = await Promise.all([
        pacingAnalysisService.analyzePacing(scenes, audioPath || null, selectedFormat),
        pacingAnalysisService.predictRetention(scenes, audioPath || null, selectedFormat),
      ]);

      setPacingAnalysis(pacing);
      setRetentionAnalysis(retention);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to analyze pacing');
      console.error('Pacing analysis error:', err);
    } finally {
      setLoading(false);
    }
  }, [scenes, audioPath, selectedFormat]);

  useEffect(() => {
    // Auto-analyze when scenes change
    if (scenes.length > 0) {
      analyzePacing();
    }
  }, [scenes, analyzePacing]);

  const getPriorityColor = (priority: Priority): 'error' | 'warning' | 'info' | 'default' => {
    switch (priority) {
      case Priority.Critical:
      case Priority.High:
        return 'error';
      case Priority.Medium:
        return 'warning';
      case Priority.Low:
        return 'info';
      default:
        return 'default';
    }
  };

  const formatDuration = (durationStr: string): string => {
    const seconds = pacingAnalysisService.durationToSeconds(durationStr);
    if (seconds < 60) {
      return `${seconds.toFixed(1)}s`;
    }
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}m ${secs}s`;
  };

  const getEngagementColor = (score: number): string => {
    if (score >= 80) return '#4caf50';
    if (score >= 60) return '#ff9800';
    return '#f44336';
  };

  if (scenes.length === 0) {
    return (
      <Card>
        <CardContent>
          <Typography variant="body2" color="text.secondary">
            Add scenes to get pacing suggestions
          </Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <Box sx={{ width: '100%' }}>
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Box display="flex" alignItems="center" justifyContent="space-between">
              <Typography variant="h6" display="flex" alignItems="center" gap={1}>
                <Timeline />
                Pacing Optimization
              </Typography>
              <FormControl size="small" sx={{ minWidth: 150 }}>
                <InputLabel>Video Format</InputLabel>
                <Select
                  value={selectedFormat}
                  label="Video Format"
                  onChange={(e) => setSelectedFormat(e.target.value as VideoFormat)}
                >
                  <MenuItem value={VideoFormat.Explainer}>Explainer</MenuItem>
                  <MenuItem value={VideoFormat.Tutorial}>Tutorial</MenuItem>
                  <MenuItem value={VideoFormat.Vlog}>Vlog</MenuItem>
                  <MenuItem value={VideoFormat.Review}>Review</MenuItem>
                  <MenuItem value={VideoFormat.Educational}>Educational</MenuItem>
                  <MenuItem value={VideoFormat.Entertainment}>Entertainment</MenuItem>
                </Select>
              </FormControl>
            </Box>

            <Button
              variant="contained"
              onClick={analyzePacing}
              disabled={loading}
              startIcon={loading ? <CircularProgress size={20} /> : <Speed />}
            >
              {loading ? 'Analyzing...' : 'Analyze Pacing'}
            </Button>

            {error && (
              <Alert severity="error" onClose={() => setError(null)}>
                {error}
              </Alert>
            )}

            {pacingAnalysis && (
              <>
                <Divider />

                {/* Engagement Score */}
                <Box>
                  <Typography variant="subtitle2" gutterBottom display="flex" alignItems="center" gap={1}>
                    <TrendingUp />
                    Engagement Score
                  </Typography>
                  <Box display="flex" alignItems="center" gap={2}>
                    <LinearProgress
                      variant="determinate"
                      value={pacingAnalysis.engagementScore}
                      sx={{
                        flex: 1,
                        height: 8,
                        borderRadius: 4,
                        bgcolor: 'grey.300',
                        '& .MuiLinearProgress-bar': {
                          bgcolor: getEngagementColor(pacingAnalysis.engagementScore),
                        },
                      }}
                    />
                    <Typography variant="h6" fontWeight="bold">
                      {pacingAnalysis.engagementScore.toFixed(1)}%
                    </Typography>
                  </Box>
                </Box>

                {/* Optimal Duration */}
                <Box>
                  <Typography variant="body2" color="text.secondary">
                    Optimal Duration: <strong>{formatDuration(pacingAnalysis.optimalDuration)}</strong>
                  </Typography>
                </Box>

                {/* Narrative Assessment */}
                <Box>
                  <Typography variant="subtitle2" gutterBottom>
                    Narrative Structure
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {pacingAnalysis.narrativeArcAssessment}
                  </Typography>
                </Box>

                {/* Warnings */}
                {pacingAnalysis.warnings.length > 0 && (
                  <Alert severity="warning" icon={<Warning />}>
                    <Typography variant="subtitle2" gutterBottom>
                      Pacing Warnings
                    </Typography>
                    <List dense>
                      {pacingAnalysis.warnings.map((warning, index) => (
                        <ListItem key={index}>
                          <ListItemText primary={warning} />
                        </ListItem>
                      ))}
                    </List>
                  </Alert>
                )}

                {/* Scene Recommendations */}
                <Accordion defaultExpanded>
                  <AccordionSummary expandIcon={<ExpandMore />}>
                    <Typography variant="subtitle2">
                      Scene Recommendations ({pacingAnalysis.sceneRecommendations.length})
                    </Typography>
                  </AccordionSummary>
                  <AccordionDetails>
                    <List>
                      {pacingAnalysis.sceneRecommendations.map((rec) => (
                        <ListItem
                          key={rec.sceneIndex}
                          sx={{
                            border: '1px solid',
                            borderColor: 'divider',
                            borderRadius: 1,
                            mb: 1,
                            flexDirection: 'column',
                            alignItems: 'flex-start',
                          }}
                        >
                          <Box width="100%" display="flex" justifyContent="space-between" alignItems="center" mb={1}>
                            <Typography variant="subtitle2">
                              Scene {rec.sceneIndex + 1}
                            </Typography>
                            <Stack direction="row" spacing={1}>
                              <Chip
                                size="small"
                                label={`Importance: ${(rec.importanceScore * 100).toFixed(0)}%`}
                                color="primary"
                                variant="outlined"
                              />
                              <Chip
                                size="small"
                                label={`Complexity: ${(rec.complexityScore * 100).toFixed(0)}%`}
                                color="secondary"
                                variant="outlined"
                              />
                            </Stack>
                          </Box>
                          <Typography variant="body2" color="text.secondary" mb={1}>
                            Current: {formatDuration(rec.currentDuration)} → Recommended:{' '}
                            {formatDuration(rec.recommendedDuration)}
                          </Typography>
                          <Typography variant="body2">{rec.reasoning}</Typography>
                          {onApplySuggestion && (
                            <Button
                              size="small"
                              variant="outlined"
                              onClick={() => onApplySuggestion(rec.sceneIndex, rec.recommendedDuration)}
                              sx={{ mt: 1 }}
                            >
                              Apply
                            </Button>
                          )}
                        </ListItem>
                      ))}
                    </List>
                  </AccordionDetails>
                </Accordion>

                {/* Retention Analysis */}
                {retentionAnalysis && (
                  <Accordion>
                    <AccordionSummary expandIcon={<ExpandMore />}>
                      <Typography variant="subtitle2">
                        Viewer Retention Analysis
                      </Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                      <Stack spacing={2}>
                        <Box>
                          <Typography variant="body2" color="text.secondary">
                            Overall Retention Score:{' '}
                            <strong>
                              {(retentionAnalysis.retentionPrediction.overallRetentionScore * 100).toFixed(1)}%
                            </strong>
                          </Typography>
                        </Box>

                        {retentionAnalysis.recommendations.length > 0 && (
                          <>
                            <Typography variant="subtitle2" display="flex" alignItems="center" gap={1}>
                              <Lightbulb />
                              Recommendations
                            </Typography>
                            <List>
                              {retentionAnalysis.recommendations.map((rec, index) => (
                                <ListItem
                                  key={index}
                                  sx={{
                                    border: '1px solid',
                                    borderColor: 'divider',
                                    borderRadius: 1,
                                    mb: 1,
                                    flexDirection: 'column',
                                    alignItems: 'flex-start',
                                  }}
                                >
                                  <Box width="100%" display="flex" justifyContent="space-between" mb={1}>
                                    <Typography variant="subtitle2">{rec.title}</Typography>
                                    <Chip
                                      size="small"
                                      label={rec.priority}
                                      color={getPriorityColor(rec.priority)}
                                    />
                                  </Box>
                                  <Typography variant="body2" color="text.secondary" mb={0.5}>
                                    At {formatDuration(rec.timestamp)}
                                  </Typography>
                                  <Typography variant="body2">{rec.description}</Typography>
                                </ListItem>
                              ))}
                            </List>
                          </>
                        )}
                      </Stack>
                    </AccordionDetails>
                  </Accordion>
                )}
              </>
            )}
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
};

export default PacingSuggestions;
