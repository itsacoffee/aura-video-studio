import React, { useState, useRef, useEffect, useCallback } from 'react';
import {
  Box,
  Paper,
  Slider,
  IconButton,
  Typography,
  Tooltip,
  Stack,
  Chip,
  Menu,
  MenuItem
} from '@mui/material';
import {
  PlayArrow,
  Pause,
  SkipPrevious,
  SkipNext,
  ZoomIn,
  ZoomOut,
  Add,
  Delete,
  ContentCopy,
  Settings
} from '@mui/icons-material';
import { VideoEffect } from '../../types/videoEffects';

interface EffectsTimelineProps {
  videoDuration: number;
  effects: VideoEffect[];
  currentTime: number;
  onEffectsChange: (effects: VideoEffect[]) => void;
  onTimeChange: (time: number) => void;
  onEffectSelect: (effect: VideoEffect | null) => void;
  selectedEffect: VideoEffect | null;
}

interface TimelineEffect extends VideoEffect {
  width: number;
  left: number;
}

export const EffectsTimeline: React.FC<EffectsTimelineProps> = ({
  videoDuration,
  effects,
  currentTime,
  onEffectsChange,
  onTimeChange,
  onEffectSelect,
  selectedEffect
}) => {
  const [isPlaying, setIsPlaying] = useState(false);
  const [zoom, setZoom] = useState(1);
  const [draggedEffect, setDraggedEffect] = useState<string | null>(null);
  const [dragOffset, setDragOffset] = useState(0);
  const [resizingEffect, setResizingEffect] = useState<{ id: string; side: 'left' | 'right' } | null>(null);
  const [contextMenu, setContextMenu] = useState<{ x: number; y: number; effect: VideoEffect } | null>(null);
  
  const timelineRef = useRef<HTMLDivElement>(null);
  const playbackIntervalRef = useRef<number | null>(null);

  // Calculate pixel width per second based on zoom
  const pixelsPerSecond = 100 * zoom;
  const timelineWidth = videoDuration * pixelsPerSecond;

  // Convert effects to timeline coordinates
  const timelineEffects: TimelineEffect[] = effects.map(effect => ({
    ...effect,
    left: effect.startTime * pixelsPerSecond,
    width: effect.duration * pixelsPerSecond
  }));

  // Playback logic
  useEffect(() => {
    if (isPlaying) {
      playbackIntervalRef.current = window.setInterval(() => {
        onTimeChange(prev => {
          const next = prev + 0.1;
          if (next >= videoDuration) {
            setIsPlaying(false);
            return 0;
          }
          return next;
        });
      }, 100);
    } else {
      if (playbackIntervalRef.current) {
        clearInterval(playbackIntervalRef.current);
        playbackIntervalRef.current = null;
      }
    }

    return () => {
      if (playbackIntervalRef.current) {
        clearInterval(playbackIntervalRef.current);
      }
    };
  }, [isPlaying, videoDuration, onTimeChange]);

  const handlePlayPause = () => {
    setIsPlaying(!isPlaying);
  };

  const handleZoomIn = () => {
    setZoom(prev => Math.min(prev * 1.5, 10));
  };

  const handleZoomOut = () => {
    setZoom(prev => Math.max(prev / 1.5, 0.1));
  };

  const handleTimelineClick = (e: React.MouseEvent) => {
    if (timelineRef.current && !draggedEffect && !resizingEffect) {
      const rect = timelineRef.current.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const time = Math.max(0, Math.min(videoDuration, x / pixelsPerSecond));
      onTimeChange(time);
    }
  };

  const handleEffectMouseDown = useCallback((e: React.MouseEvent, effect: VideoEffect) => {
    e.stopPropagation();
    
    const target = e.target as HTMLElement;
    const isResizeHandle = target.classList.contains('resize-handle');
    
    if (isResizeHandle) {
      const side = target.classList.contains('left') ? 'left' : 'right';
      setResizingEffect({ id: effect.id, side });
    } else {
      setDraggedEffect(effect.id);
      const rect = (e.currentTarget as HTMLElement).getBoundingClientRect();
      setDragOffset(e.clientX - rect.left);
    }
    
    onEffectSelect(effect);
  }, [onEffectSelect]);

  const handleMouseMove = useCallback((e: MouseEvent) => {
    if (timelineRef.current) {
      const rect = timelineRef.current.getBoundingClientRect();
      const x = e.clientX - rect.left;

      if (draggedEffect) {
        const newStartTime = Math.max(0, (x - dragOffset) / pixelsPerSecond);
        const effect = effects.find(e => e.id === draggedEffect);
        if (effect && newStartTime + effect.duration <= videoDuration) {
          const updatedEffects = effects.map(e =>
            e.id === draggedEffect ? { ...e, startTime: newStartTime } : e
          );
          onEffectsChange(updatedEffects);
        }
      } else if (resizingEffect) {
        const time = Math.max(0, Math.min(videoDuration, x / pixelsPerSecond));
        const effect = effects.find(e => e.id === resizingEffect.id);
        
        if (effect) {
          let updatedEffect = { ...effect };
          
          if (resizingEffect.side === 'left') {
            const newStartTime = Math.min(time, effect.startTime + effect.duration - 0.1);
            const newDuration = effect.startTime + effect.duration - newStartTime;
            updatedEffect = { ...effect, startTime: newStartTime, duration: newDuration };
          } else {
            const newDuration = Math.max(0.1, time - effect.startTime);
            updatedEffect = { ...effect, duration: newDuration };
          }
          
          const updatedEffects = effects.map(e =>
            e.id === resizingEffect.id ? updatedEffect : e
          );
          onEffectsChange(updatedEffects);
        }
      }
    }
  }, [draggedEffect, resizingEffect, effects, pixelsPerSecond, dragOffset, videoDuration, onEffectsChange]);

  const handleMouseUp = useCallback(() => {
    setDraggedEffect(null);
    setResizingEffect(null);
  }, []);

  useEffect(() => {
    if (draggedEffect || resizingEffect) {
      window.addEventListener('mousemove', handleMouseMove);
      window.addEventListener('mouseup', handleMouseUp);
      
      return () => {
        window.removeEventListener('mousemove', handleMouseMove);
        window.removeEventListener('mouseup', handleMouseUp);
      };
    }
  }, [draggedEffect, resizingEffect, handleMouseMove, handleMouseUp]);

  const handleContextMenu = (e: React.MouseEvent, effect: VideoEffect) => {
    e.preventDefault();
    setContextMenu({ x: e.clientX, y: e.clientY, effect });
  };

  const handleCloseContextMenu = () => {
    setContextMenu(null);
  };

  const handleDuplicateEffect = (effect: VideoEffect) => {
    const newEffect = {
      ...effect,
      id: `${effect.id}-copy-${Date.now()}`,
      startTime: effect.startTime + effect.duration
    };
    onEffectsChange([...effects, newEffect]);
    handleCloseContextMenu();
  };

  const handleDeleteEffect = (effect: VideoEffect) => {
    onEffectsChange(effects.filter(e => e.id !== effect.id));
    if (selectedEffect?.id === effect.id) {
      onEffectSelect(null);
    }
    handleCloseContextMenu();
  };

  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    const ms = Math.floor((seconds % 1) * 100);
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}.${ms.toString().padStart(2, '0')}`;
  };

  const getEffectColor = (effect: VideoEffect): string => {
    switch (effect.type) {
      case 'Transition': return '#9c27b0';
      case 'Filter': return '#2196f3';
      case 'TextAnimation': return '#ff9800';
      case 'ColorCorrection': return '#4caf50';
      case 'Transform': return '#f44336';
      default: return '#757575';
    }
  };

  return (
    <Paper sx={{ p: 2, bgcolor: '#1e1e1e', color: '#fff' }}>
      {/* Controls */}
      <Stack direction="row" spacing={2} alignItems="center" mb={2}>
        <IconButton onClick={() => onTimeChange(0)} size="small" sx={{ color: '#fff' }}>
          <SkipPrevious />
        </IconButton>
        
        <IconButton onClick={handlePlayPause} size="small" sx={{ color: '#fff' }}>
          {isPlaying ? <Pause /> : <PlayArrow />}
        </IconButton>
        
        <IconButton onClick={() => onTimeChange(Math.min(currentTime + 1, videoDuration))} size="small" sx={{ color: '#fff' }}>
          <SkipNext />
        </IconButton>
        
        <Typography variant="body2" sx={{ minWidth: 80 }}>
          {formatTime(currentTime)} / {formatTime(videoDuration)}
        </Typography>
        
        <Box flex={1} />
        
        <IconButton onClick={handleZoomOut} size="small" sx={{ color: '#fff' }}>
          <ZoomOut />
        </IconButton>
        
        <Typography variant="body2" sx={{ minWidth: 50, textAlign: 'center' }}>
          {Math.round(zoom * 100)}%
        </Typography>
        
        <IconButton onClick={handleZoomIn} size="small" sx={{ color: '#fff' }}>
          <ZoomIn />
        </IconButton>
      </Stack>

      {/* Timeline */}
      <Box
        ref={timelineRef}
        onClick={handleTimelineClick}
        sx={{
          position: 'relative',
          height: 150,
          overflowX: 'auto',
          overflowY: 'hidden',
          bgcolor: '#2a2a2a',
          borderRadius: 1,
          cursor: 'crosshair',
          '&::-webkit-scrollbar': {
            height: 8
          },
          '&::-webkit-scrollbar-track': {
            bgcolor: '#1e1e1e'
          },
          '&::-webkit-scrollbar-thumb': {
            bgcolor: '#424242',
            borderRadius: 1
          }
        }}
      >
        {/* Time ruler */}
        <Box sx={{ position: 'relative', height: 30, borderBottom: '1px solid #424242' }}>
          {Array.from({ length: Math.ceil(videoDuration) + 1 }, (_, i) => (
            <Box
              key={i}
              sx={{
                position: 'absolute',
                left: i * pixelsPerSecond,
                height: '100%',
                borderLeft: '1px solid #424242',
                pl: 0.5
              }}
            >
              <Typography variant="caption" sx={{ color: '#999' }}>
                {i}s
              </Typography>
            </Box>
          ))}
        </Box>

        {/* Effects */}
        <Box sx={{ position: 'relative', height: 120, minWidth: timelineWidth }}>
          {timelineEffects.map(effect => (
            <Box
              key={effect.id}
              onMouseDown={(e) => handleEffectMouseDown(e, effect)}
              onContextMenu={(e) => handleContextMenu(e, effect)}
              sx={{
                position: 'absolute',
                left: effect.left,
                top: effect.layer * 30 + 10,
                width: effect.width,
                height: 25,
                bgcolor: getEffectColor(effect),
                opacity: effect.enabled ? 0.8 : 0.4,
                borderRadius: 0.5,
                border: selectedEffect?.id === effect.id ? '2px solid #fff' : 'none',
                cursor: draggedEffect === effect.id ? 'grabbing' : 'grab',
                display: 'flex',
                alignItems: 'center',
                px: 0.5,
                transition: draggedEffect === effect.id ? 'none' : 'left 0.1s, width 0.1s',
                '&:hover': {
                  opacity: 1,
                  boxShadow: '0 2px 8px rgba(0,0,0,0.3)'
                }
              }}
            >
              {/* Left resize handle */}
              <Box
                className="resize-handle left"
                sx={{
                  position: 'absolute',
                  left: 0,
                  top: 0,
                  bottom: 0,
                  width: 4,
                  cursor: 'ew-resize',
                  bgcolor: 'rgba(255,255,255,0.3)',
                  '&:hover': {
                    bgcolor: 'rgba(255,255,255,0.6)'
                  }
                }}
              />
              
              {/* Effect name */}
              <Typography
                variant="caption"
                noWrap
                sx={{
                  flex: 1,
                  fontSize: '0.7rem',
                  fontWeight: 500,
                  px: 0.5
                }}
              >
                {effect.name}
              </Typography>
              
              {/* Right resize handle */}
              <Box
                className="resize-handle right"
                sx={{
                  position: 'absolute',
                  right: 0,
                  top: 0,
                  bottom: 0,
                  width: 4,
                  cursor: 'ew-resize',
                  bgcolor: 'rgba(255,255,255,0.3)',
                  '&:hover': {
                    bgcolor: 'rgba(255,255,255,0.6)'
                  }
                }}
              />
            </Box>
          ))}
        </Box>

        {/* Playhead */}
        <Box
          sx={{
            position: 'absolute',
            left: currentTime * pixelsPerSecond,
            top: 0,
            bottom: 0,
            width: 2,
            bgcolor: '#f44336',
            pointerEvents: 'none',
            zIndex: 10
          }}
        >
          <Box
            sx={{
              position: 'absolute',
              top: -5,
              left: -5,
              width: 0,
              height: 0,
              borderLeft: '5px solid transparent',
              borderRight: '5px solid transparent',
              borderTop: '10px solid #f44336'
            }}
          />
        </Box>
      </Box>

      {/* Context menu */}
      <Menu
        open={contextMenu !== null}
        onClose={handleCloseContextMenu}
        anchorReference="anchorPosition"
        anchorPosition={
          contextMenu ? { top: contextMenu.y, left: contextMenu.x } : undefined
        }
      >
        <MenuItem onClick={() => contextMenu && handleDuplicateEffect(contextMenu.effect)}>
          <ContentCopy fontSize="small" sx={{ mr: 1 }} />
          Duplicate
        </MenuItem>
        <MenuItem onClick={() => contextMenu && handleDeleteEffect(contextMenu.effect)}>
          <Delete fontSize="small" sx={{ mr: 1 }} />
          Delete
        </MenuItem>
        <MenuItem onClick={() => {
          if (contextMenu) {
            onEffectSelect(contextMenu.effect);
          }
          handleCloseContextMenu();
        }}>
          <Settings fontSize="small" sx={{ mr: 1 }} />
          Edit Properties
        </MenuItem>
      </Menu>
    </Paper>
  );
};
