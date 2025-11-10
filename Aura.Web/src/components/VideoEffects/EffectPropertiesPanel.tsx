import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Slider,
  Switch,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Button,
  Stack,
  Divider,
  IconButton,
  Collapse,
  Chip,
  FormControlLabel,
  Grid
} from '@mui/material';
import {
  ExpandMore,
  ExpandLess,
  Close,
  Save,
  Refresh,
  Visibility,
  VisibilityOff
} from '@mui/icons-material';
import { VideoEffect, EffectParameter } from '../../types/videoEffects';

interface EffectPropertiesPanelProps {
  effect: VideoEffect | null;
  onEffectChange: (effect: VideoEffect) => void;
  onClose: () => void;
  onPreview?: (effect: VideoEffect) => void;
}

export const EffectPropertiesPanel: React.FC<EffectPropertiesPanelProps> = ({
  effect,
  onEffectChange,
  onClose,
  onPreview
}) => {
  const [localEffect, setLocalEffect] = useState<VideoEffect | null>(effect);
  const [expandedSections, setExpandedSections] = useState<Set<string>>(
    new Set(['basic', 'timing', 'parameters'])
  );

  useEffect(() => {
    setLocalEffect(effect);
  }, [effect]);

  if (!localEffect) {
    return null;
  }

  const toggleSection = (section: string) => {
    setExpandedSections(prev => {
      const next = new Set(prev);
      if (next.has(section)) {
        next.delete(section);
      } else {
        next.add(section);
      }
      return next;
    });
  };

  const handlePropertyChange = (property: string, value: any) => {
    const updated = { ...localEffect, [property]: value };
    setLocalEffect(updated);
    onEffectChange(updated);
  };

  const handleParameterChange = (paramName: string, value: any) => {
    const updated = {
      ...localEffect,
      parameters: {
        ...localEffect.parameters,
        [paramName]: value
      }
    };
    setLocalEffect(updated);
    onEffectChange(updated);
  };

  const handleReset = () => {
    // Reset to default values - this would ideally come from the effect definition
    setLocalEffect(effect);
  };

  const renderParameterControl = (param: EffectParameter) => {
    const value = localEffect.parameters[param.name] ?? param.defaultValue;

    switch (param.type) {
      case 'number':
        return (
          <Box key={param.name} sx={{ mb: 2 }}>
            <Typography variant="caption" color="text.secondary" gutterBottom>
              {param.label}
            </Typography>
            <Stack direction="row" spacing={2} alignItems="center">
              <Slider
                value={typeof value === 'number' ? value : 0}
                onChange={(_, newValue) => handleParameterChange(param.name, newValue)}
                min={param.min ?? 0}
                max={param.max ?? 100}
                step={param.step ?? 1}
                valueLabelDisplay="auto"
                sx={{ flex: 1 }}
              />
              <TextField
                type="number"
                value={value}
                onChange={(e) => handleParameterChange(param.name, parseFloat(e.target.value))}
                inputProps={{
                  min: param.min,
                  max: param.max,
                  step: param.step
                }}
                sx={{ width: 80 }}
                size="small"
              />
              {param.unit && (
                <Typography variant="caption" sx={{ minWidth: 30 }}>
                  {param.unit}
                </Typography>
              )}
            </Stack>
            {param.description && (
              <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5 }}>
                {param.description}
              </Typography>
            )}
          </Box>
        );

      case 'boolean':
        return (
          <FormControlLabel
            key={param.name}
            control={
              <Switch
                checked={Boolean(value)}
                onChange={(e) => handleParameterChange(param.name, e.target.checked)}
              />
            }
            label={param.label}
            sx={{ mb: 1 }}
          />
        );

      case 'color':
        return (
          <Box key={param.name} sx={{ mb: 2 }}>
            <Typography variant="caption" color="text.secondary" gutterBottom>
              {param.label}
            </Typography>
            <Stack direction="row" spacing={1} alignItems="center">
              <input
                type="color"
                value={value}
                onChange={(e) => handleParameterChange(param.name, e.target.value)}
                style={{
                  width: 60,
                  height: 40,
                  border: 'none',
                  borderRadius: 4,
                  cursor: 'pointer'
                }}
              />
              <TextField
                value={value}
                onChange={(e) => handleParameterChange(param.name, e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
            </Stack>
          </Box>
        );

      case 'select':
        return (
          <FormControl key={param.name} fullWidth size="small" sx={{ mb: 2 }}>
            <InputLabel>{param.label}</InputLabel>
            <Select
              value={value}
              onChange={(e) => handleParameterChange(param.name, e.target.value)}
              label={param.label}
            >
              {param.options?.map(option => (
                <MenuItem key={option} value={option}>
                  {option}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        );

      case 'text':
        return (
          <TextField
            key={param.name}
            label={param.label}
            value={value}
            onChange={(e) => handleParameterChange(param.name, e.target.value)}
            fullWidth
            size="small"
            multiline={param.name.includes('text')}
            rows={param.name.includes('text') ? 3 : 1}
            sx={{ mb: 2 }}
          />
        );

      default:
        return null;
    }
  };

  return (
    <Paper
      sx={{
        height: '100%',
        overflowY: 'auto',
        bgcolor: '#1e1e1e',
        color: '#fff'
      }}
    >
      {/* Header */}
      <Box
        sx={{
          p: 2,
          borderBottom: '1px solid #424242',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          position: 'sticky',
          top: 0,
          bgcolor: '#1e1e1e',
          zIndex: 1
        }}
      >
        <Stack direction="row" spacing={1} alignItems="center">
          <Typography variant="h6">{localEffect.name}</Typography>
          <Chip
            label={localEffect.type}
            size="small"
            color="primary"
            variant="outlined"
          />
        </Stack>
        
        <Stack direction="row" spacing={1}>
          {onPreview && (
            <IconButton size="small" onClick={() => onPreview(localEffect)} sx={{ color: '#fff' }}>
              <Visibility />
            </IconButton>
          )}
          <IconButton size="small" onClick={handleReset} sx={{ color: '#fff' }}>
            <Refresh />
          </IconButton>
          <IconButton size="small" onClick={onClose} sx={{ color: '#fff' }}>
            <Close />
          </IconButton>
        </Stack>
      </Box>

      <Box sx={{ p: 2 }}>
        {/* Basic Properties Section */}
        <Box sx={{ mb: 2 }}>
          <Button
            fullWidth
            onClick={() => toggleSection('basic')}
            sx={{
              justifyContent: 'space-between',
              color: '#fff',
              textTransform: 'none',
              mb: 1
            }}
          >
            <Typography variant="subtitle1">Basic Properties</Typography>
            {expandedSections.has('basic') ? <ExpandLess /> : <ExpandMore />}
          </Button>
          
          <Collapse in={expandedSections.has('basic')}>
            <Box sx={{ pl: 2 }}>
              <TextField
                label="Effect Name"
                value={localEffect.name}
                onChange={(e) => handlePropertyChange('name', e.target.value)}
                fullWidth
                size="small"
                sx={{ mb: 2 }}
              />
              
              <TextField
                label="Description"
                value={localEffect.description || ''}
                onChange={(e) => handlePropertyChange('description', e.target.value)}
                fullWidth
                size="small"
                multiline
                rows={2}
                sx={{ mb: 2 }}
              />
              
              <FormControlLabel
                control={
                  <Switch
                    checked={localEffect.enabled}
                    onChange={(e) => handlePropertyChange('enabled', e.target.checked)}
                  />
                }
                label="Enabled"
              />
            </Box>
          </Collapse>
        </Box>

        <Divider sx={{ my: 2, borderColor: '#424242' }} />

        {/* Timing Section */}
        <Box sx={{ mb: 2 }}>
          <Button
            fullWidth
            onClick={() => toggleSection('timing')}
            sx={{
              justifyContent: 'space-between',
              color: '#fff',
              textTransform: 'none',
              mb: 1
            }}
          >
            <Typography variant="subtitle1">Timing</Typography>
            {expandedSections.has('timing') ? <ExpandLess /> : <ExpandMore />}
          </Button>
          
          <Collapse in={expandedSections.has('timing')}>
            <Box sx={{ pl: 2 }}>
              <TextField
                label="Start Time (seconds)"
                type="number"
                value={localEffect.startTime}
                onChange={(e) => handlePropertyChange('startTime', parseFloat(e.target.value))}
                fullWidth
                size="small"
                inputProps={{ min: 0, step: 0.1 }}
                sx={{ mb: 2 }}
              />
              
              <TextField
                label="Duration (seconds)"
                type="number"
                value={localEffect.duration}
                onChange={(e) => handlePropertyChange('duration', parseFloat(e.target.value))}
                fullWidth
                size="small"
                inputProps={{ min: 0.1, step: 0.1 }}
                sx={{ mb: 2 }}
              />
              
              <Typography variant="caption" color="text.secondary" gutterBottom>
                Intensity
              </Typography>
              <Slider
                value={localEffect.intensity}
                onChange={(_, value) => handlePropertyChange('intensity', value)}
                min={0}
                max={1}
                step={0.01}
                valueLabelDisplay="auto"
                valueLabelFormat={(value) => `${Math.round(value * 100)}%`}
              />
            </Box>
          </Collapse>
        </Box>

        <Divider sx={{ my: 2, borderColor: '#424242' }} />

        {/* Effect Parameters Section */}
        <Box sx={{ mb: 2 }}>
          <Button
            fullWidth
            onClick={() => toggleSection('parameters')}
            sx={{
              justifyContent: 'space-between',
              color: '#fff',
              textTransform: 'none',
              mb: 1
            }}
          >
            <Typography variant="subtitle1">Effect Parameters</Typography>
            {expandedSections.has('parameters') ? <ExpandLess /> : <ExpandMore />}
          </Button>
          
          <Collapse in={expandedSections.has('parameters')}>
            <Box sx={{ pl: 2 }}>
              {/* Render effect-specific parameters */}
              {/* This would be populated based on the effect type */}
              <Typography variant="caption" color="text.secondary">
                Effect-specific parameters will appear here
              </Typography>
            </Box>
          </Collapse>
        </Box>

        <Divider sx={{ my: 2, borderColor: '#424242' }} />

        {/* Layer and Advanced */}
        <Box sx={{ mb: 2 }}>
          <Button
            fullWidth
            onClick={() => toggleSection('advanced')}
            sx={{
              justifyContent: 'space-between',
              color: '#fff',
              textTransform: 'none',
              mb: 1
            }}
          >
            <Typography variant="subtitle1">Advanced</Typography>
            {expandedSections.has('advanced') ? <ExpandLess /> : <ExpandMore />}
          </Button>
          
          <Collapse in={expandedSections.has('advanced')}>
            <Box sx={{ pl: 2 }}>
              <TextField
                label="Layer"
                type="number"
                value={localEffect.layer}
                onChange={(e) => handlePropertyChange('layer', parseInt(e.target.value))}
                fullWidth
                size="small"
                inputProps={{ min: 0, max: 10 }}
                helperText="Higher layers are rendered on top"
                sx={{ mb: 2 }}
              />
              
              <FormControl fullWidth size="small">
                <InputLabel>Category</InputLabel>
                <Select
                  value={localEffect.category}
                  onChange={(e) => handlePropertyChange('category', e.target.value)}
                  label="Category"
                >
                  <MenuItem value="Basic">Basic</MenuItem>
                  <MenuItem value="Artistic">Artistic</MenuItem>
                  <MenuItem value="ColorGrading">Color Grading</MenuItem>
                  <MenuItem value="Blur">Blur</MenuItem>
                  <MenuItem value="Vintage">Vintage</MenuItem>
                  <MenuItem value="Modern">Modern</MenuItem>
                  <MenuItem value="Cinematic">Cinematic</MenuItem>
                  <MenuItem value="Custom">Custom</MenuItem>
                </Select>
              </FormControl>
            </Box>
          </Collapse>
        </Box>
      </Box>
    </Paper>
  );
};
