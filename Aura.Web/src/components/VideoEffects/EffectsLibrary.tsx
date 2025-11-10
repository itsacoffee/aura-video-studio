import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Grid,
  Card,
  CardContent,
  CardMedia,
  CardActions,
  Button,
  IconButton,
  Chip,
  Stack,
  Tabs,
  Tab,
  InputAdornment,
  Menu,
  MenuItem,
  Tooltip
} from '@mui/material';
import {
  Search,
  FilterList,
  Star,
  StarBorder,
  Add,
  MoreVert,
  Visibility
} from '@mui/icons-material';
import { EffectPreset, EffectCategory, VideoEffect } from '../../types/videoEffects';
import { videoEffectsApi } from '../../services/api/videoEffects';

interface EffectsLibraryProps {
  onEffectAdd: (effect: VideoEffect) => void;
  onPresetApply: (preset: EffectPreset) => void;
}

export const EffectsLibrary: React.FC<EffectsLibraryProps> = ({
  onEffectAdd,
  onPresetApply
}) => {
  const [presets, setPresets] = useState<EffectPreset[]>([]);
  const [filteredPresets, setFilteredPresets] = useState<EffectPreset[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<EffectCategory | 'all'>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [anchorEl, setAnchorEl] = useState<{ element: HTMLElement; preset: EffectPreset } | null>(null);

  useEffect(() => {
    loadPresets();
  }, []);

  useEffect(() => {
    filterPresets();
  }, [presets, selectedCategory, searchQuery]);

  const loadPresets = async () => {
    try {
      setLoading(true);
      const data = await videoEffectsApi.getPresets();
      setPresets(data);
    } catch (error) {
      console.error('Failed to load presets:', error);
    } finally {
      setLoading(false);
    }
  };

  const filterPresets = () => {
    let filtered = presets;

    // Filter by category
    if (selectedCategory !== 'all') {
      filtered = filtered.filter(p => p.category === selectedCategory);
    }

    // Filter by search query
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(
        p =>
          p.name.toLowerCase().includes(query) ||
          p.description?.toLowerCase().includes(query) ||
          p.tags.some(tag => tag.toLowerCase().includes(query))
      );
    }

    setFilteredPresets(filtered);
  };

  const handleCategoryChange = (_: React.SyntheticEvent, newValue: EffectCategory | 'all') => {
    setSelectedCategory(newValue);
  };

  const handleToggleFavorite = async (preset: EffectPreset, event: React.MouseEvent) => {
    event.stopPropagation();
    try {
      const updated = { ...preset, isFavorite: !preset.isFavorite };
      await videoEffectsApi.savePreset(updated);
      setPresets(presets.map(p => (p.id === preset.id ? updated : p)));
    } catch (error) {
      console.error('Failed to toggle favorite:', error);
    }
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, preset: EffectPreset) => {
    event.stopPropagation();
    setAnchorEl({ element: event.currentTarget, preset });
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleDeletePreset = async () => {
    if (anchorEl) {
      try {
        await videoEffectsApi.deletePreset(anchorEl.preset.id);
        setPresets(presets.filter(p => p.id !== anchorEl.preset.id));
        handleMenuClose();
      } catch (error) {
        console.error('Failed to delete preset:', error);
      }
    }
  };

  const handleDuplicatePreset = async () => {
    if (anchorEl) {
      try {
        const duplicate = {
          ...anchorEl.preset,
          id: `${anchorEl.preset.id}-copy-${Date.now()}`,
          name: `${anchorEl.preset.name} (Copy)`,
          isBuiltIn: false,
          usageCount: 0,
          createdAt: new Date()
        };
        await videoEffectsApi.savePreset(duplicate);
        await loadPresets();
        handleMenuClose();
      } catch (error) {
        console.error('Failed to duplicate preset:', error);
      }
    }
  };

  const categories: Array<{ value: EffectCategory | 'all'; label: string }> = [
    { value: 'all', label: 'All' },
    { value: 'Basic', label: 'Basic' },
    { value: 'Cinematic', label: 'Cinematic' },
    { value: 'ColorGrading', label: 'Color Grading' },
    { value: 'Vintage', label: 'Vintage' },
    { value: 'Artistic', label: 'Artistic' },
    { value: 'Blur', label: 'Blur' },
    { value: 'Modern', label: 'Modern' },
    { value: 'Custom', label: 'Custom' }
  ];

  return (
    <Paper sx={{ height: '100%', display: 'flex', flexDirection: 'column', bgcolor: '#1e1e1e' }}>
      {/* Header */}
      <Box sx={{ p: 2, borderBottom: '1px solid #424242' }}>
        <Typography variant="h6" gutterBottom sx={{ color: '#fff' }}>
          Effects Library
        </Typography>
        
        <TextField
          placeholder="Search effects..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          fullWidth
          size="small"
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Search />
              </InputAdornment>
            )
          }}
          sx={{ mb: 2 }}
        />

        {/* Category tabs */}
        <Tabs
          value={selectedCategory}
          onChange={handleCategoryChange}
          variant="scrollable"
          scrollButtons="auto"
          sx={{
            '& .MuiTab-root': {
              color: '#999',
              minWidth: 'auto',
              px: 2
            },
            '& .Mui-selected': {
              color: '#2196f3'
            }
          }}
        >
          {categories.map(cat => (
            <Tab key={cat.value} label={cat.label} value={cat.value} />
          ))}
        </Tabs>
      </Box>

      {/* Presets grid */}
      <Box sx={{ flex: 1, overflowY: 'auto', p: 2 }}>
        {loading ? (
          <Typography color="text.secondary">Loading effects...</Typography>
        ) : filteredPresets.length === 0 ? (
          <Typography color="text.secondary">No effects found</Typography>
        ) : (
          <Grid container spacing={2}>
            {filteredPresets.map(preset => (
              <Grid item xs={12} sm={6} md={4} key={preset.id}>
                <Card
                  sx={{
                    bgcolor: '#2a2a2a',
                    cursor: 'pointer',
                    transition: 'transform 0.2s, box-shadow 0.2s',
                    '&:hover': {
                      transform: 'translateY(-4px)',
                      boxShadow: '0 8px 16px rgba(0,0,0,0.3)'
                    }
                  }}
                  onClick={() => onPresetApply(preset)}
                >
                  {/* Thumbnail */}
                  {preset.thumbnailUrl ? (
                    <CardMedia
                      component="img"
                      height="140"
                      image={preset.thumbnailUrl}
                      alt={preset.name}
                    />
                  ) : (
                    <Box
                      sx={{
                        height: 140,
                        bgcolor: '#1e1e1e',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center'
                      }}
                    >
                      <Typography variant="h4" color="text.secondary">
                        FX
                      </Typography>
                    </Box>
                  )}

                  <CardContent>
                    <Stack direction="row" justifyContent="space-between" alignItems="start" mb={1}>
                      <Typography variant="h6" component="div" sx={{ flex: 1 }}>
                        {preset.name}
                      </Typography>
                      
                      <Stack direction="row" spacing={0.5}>
                        <IconButton
                          size="small"
                          onClick={(e) => handleToggleFavorite(preset, e)}
                          sx={{ color: preset.isFavorite ? '#ffc107' : '#666' }}
                        >
                          {preset.isFavorite ? <Star /> : <StarBorder />}
                        </IconButton>
                        
                        {!preset.isBuiltIn && (
                          <IconButton
                            size="small"
                            onClick={(e) => handleMenuOpen(e, preset)}
                            sx={{ color: '#666' }}
                          >
                            <MoreVert />
                          </IconButton>
                        )}
                      </Stack>
                    </Stack>

                    <Typography variant="body2" color="text.secondary" mb={1}>
                      {preset.description}
                    </Typography>

                    <Stack direction="row" spacing={0.5} flexWrap="wrap" gap={0.5}>
                      <Chip
                        label={preset.category}
                        size="small"
                        color="primary"
                        variant="outlined"
                      />
                      {preset.isBuiltIn && (
                        <Chip label="Built-in" size="small" variant="outlined" />
                      )}
                      <Chip
                        label={`${preset.usageCount} uses`}
                        size="small"
                        variant="outlined"
                      />
                    </Stack>
                  </CardContent>

                  <CardActions>
                    <Button
                      size="small"
                      startIcon={<Add />}
                      onClick={(e) => {
                        e.stopPropagation();
                        // Add first effect from preset
                        if (preset.effects.length > 0) {
                          onEffectAdd(preset.effects[0]);
                        }
                      }}
                    >
                      Add Effect
                    </Button>
                    
                    <Button
                      size="small"
                      startIcon={<Visibility />}
                      onClick={(e) => {
                        e.stopPropagation();
                        // Preview functionality
                      }}
                    >
                      Preview
                    </Button>
                  </CardActions>
                </Card>
              </Grid>
            ))}
          </Grid>
        )}
      </Box>

      {/* Context menu */}
      <Menu
        anchorEl={anchorEl?.element}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        <MenuItem onClick={handleDuplicatePreset}>Duplicate</MenuItem>
        <MenuItem onClick={handleDeletePreset}>Delete</MenuItem>
      </Menu>
    </Paper>
  );
};
