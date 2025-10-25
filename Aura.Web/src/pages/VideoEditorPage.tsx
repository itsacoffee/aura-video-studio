import { useState, useEffect, useRef, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import { EditorLayout } from '../components/EditorLayout/EditorLayout';
import { VideoPreviewPanel } from '../components/EditorLayout/VideoPreviewPanel';
import { TimelinePanel } from '../components/EditorLayout/TimelinePanel';
import { PropertiesPanel } from '../components/EditorLayout/PropertiesPanel';
import { MediaLibraryPanel } from '../components/EditorLayout/MediaLibraryPanel';
import { EffectsLibraryPanel } from '../components/EditorLayout/EffectsLibraryPanel';
import { AppliedEffect, EffectPreset } from '../types/effects';
import { keyboardShortcutManager } from '../services/keyboardShortcutManager';
import { useProjectState } from '../hooks/useProjectState';
import { ProjectFile, ProjectMediaItem } from '../types/project';

export interface TimelineClip {
  id: string;
  trackId: string;
  startTime: number;
  duration: number;
  label: string;
  type: 'video' | 'audio' | 'image';
  prompt?: string;
  effects?: AppliedEffect[];
  transform?: {
    x?: number;
    y?: number;
    scale?: number;
    rotation?: number;
  };
  // Media source reference
  mediaId?: string;
  file?: File;
  // Visual data
  thumbnails?: Array<{ dataUrl: string; timestamp: number }>;
  waveform?: { peaks: number[]; duration: number };
  preview?: string;
}

interface TimelineTrack {
  id: string;
  label: string;
  type: 'video' | 'audio';
  visible: boolean;
  locked: boolean;
}

export function VideoEditorPage() {
  const [searchParams] = useSearchParams();
  const [currentTime, setCurrentTime] = useState(0);
  const [selectedClipId, setSelectedClipId] = useState<string | null>(null);
  const [clips, setClips] = useState<TimelineClip[]>([]);
  const [, setShowKeyboardShortcuts] = useState(false);
  const [isPlaying, setIsPlaying] = useState(false);
  const [playbackSpeed, setPlaybackSpeed] = useState(1.0);
  const [inPoint, setInPoint] = useState<number | null>(null);
  const [outPoint, setOutPoint] = useState<number | null>(null);
  const [selectedTool, setSelectedTool] = useState<'select' | 'razor' | 'hand'>('select');
  const [tracks, setTracks] = useState<TimelineTrack[]>([
    { id: 'video1', label: 'Video 1', type: 'video', visible: true, locked: false },
    { id: 'video2', label: 'Video 2', type: 'video', visible: true, locked: false },
    { id: 'audio1', label: 'Audio 1', type: 'audio', visible: true, locked: false },
    { id: 'audio2', label: 'Audio 2', type: 'audio', visible: true, locked: false },
  ]);
  const [mediaLibrary, setMediaLibrary] = useState<ProjectMediaItem[]>([]);

  // Project state management with autosave
  const handleProjectLoaded = useCallback((project: ProjectFile) => {
    // Restore clips
    setClips(project.clips.map(clip => ({
      ...clip,
      file: undefined, // Files can't be serialized, will need to be re-added
    })));
    
    // Restore tracks
    if (project.tracks) {
      setTracks(project.tracks);
    }
    
    // Restore media library
    if (project.mediaLibrary) {
      setMediaLibrary(project.mediaLibrary);
    }
    
    // Restore player position
    if (project.playerPosition !== undefined) {
      setCurrentTime(project.playerPosition);
    }
  }, []);

  const {
    projectName,
    isDirty,
    autosaveStatus,
    lastSaved,
    saveCurrentProject,
    exportProject,
    loadProject,
  } = useProjectState(clips, tracks, mediaLibrary, currentTime, handleProjectLoaded);

  // Load project from URL parameter
  useEffect(() => {
    const projectId = searchParams.get('projectId');
    if (projectId) {
      loadProject(projectId).catch((error) => {
        console.error('Failed to load project:', error);
        // Better error message based on error type
        const errorMessage = error.message || 'An unknown error occurred';
        // TODO: Replace with toast notification system when available
        console.error(`Failed to load project: ${errorMessage}`);
      });
    }
  }, [searchParams, loadProject]);

  // Log state changes for debugging
  useEffect(() => {
    if (playbackSpeed !== 1.0) {
      console.log('Playback speed:', playbackSpeed);
    }
  }, [playbackSpeed]);

  useEffect(() => {
    if (inPoint !== null || outPoint !== null) {
      console.log('In/Out points:', { inPoint, outPoint });
    }
  }, [inPoint, outPoint]);

  useEffect(() => {
    console.log('Selected tool:', selectedTool);
  }, [selectedTool]);

  useEffect(() => {
    console.log('Playing:', isPlaying);
  }, [isPlaying]);
  
  // Ref to track video preview controls
  const videoPreviewRef = useRef<{
    play: () => void;
    pause: () => void;
    stepForward: () => void;
    stepBackward: () => void;
    setPlaybackRate: (rate: number) => void;
  } | null>(null);

  // Ref to media library panel for triggering file picker
  const mediaLibraryRef = useRef<{ openFilePicker: () => void } | null>(null);

  // Register keyboard shortcuts for video editor
  useEffect(() => {
    // Set the active context
    keyboardShortcutManager.setActiveContext('video-editor');

    // Register video editor specific shortcuts
    keyboardShortcutManager.registerMultiple([
      // Playback controls
      {
        id: 'play-pause',
        keys: 'Space',
        description: 'Play/Pause',
        context: 'video-editor',
        handler: () => {
          setIsPlaying((prev) => !prev);
        },
      },
      // J/K/L Shuttle control
      {
        id: 'shuttle-reverse',
        keys: 'J',
        description: 'Reverse playback (press multiple times for faster)',
        context: 'video-editor',
        handler: () => {
          setPlaybackSpeed((speed) => {
            let newSpeed = speed - 0.5;
            if (newSpeed < -4.0) newSpeed = -4.0;
            videoPreviewRef.current?.setPlaybackRate(newSpeed);
            setIsPlaying(true);
            return newSpeed;
          });
        },
      },
      {
        id: 'shuttle-pause',
        keys: 'K',
        description: 'Pause/Reset playback speed',
        context: 'video-editor',
        handler: () => {
          setIsPlaying(false);
          setPlaybackSpeed(1.0);
          videoPreviewRef.current?.setPlaybackRate(1.0);
        },
      },
      {
        id: 'shuttle-forward',
        keys: 'L',
        description: 'Forward playback (press multiple times for faster)',
        context: 'video-editor',
        handler: () => {
          setPlaybackSpeed((speed) => {
            let newSpeed = speed + 0.5;
            if (newSpeed > 4.0) newSpeed = 4.0;
            videoPreviewRef.current?.setPlaybackRate(newSpeed);
            setIsPlaying(true);
            return newSpeed;
          });
        },
      },
      // Frame navigation
      {
        id: 'frame-backward',
        keys: 'ArrowLeft',
        description: 'Previous frame',
        context: 'video-editor',
        handler: () => {
          videoPreviewRef.current?.stepBackward();
        },
      },
      {
        id: 'frame-forward',
        keys: 'ArrowRight',
        description: 'Next frame',
        context: 'video-editor',
        handler: () => {
          videoPreviewRef.current?.stepForward();
        },
      },
      // In/Out points
      {
        id: 'set-in-point',
        keys: 'I',
        description: 'Set In point',
        context: 'video-editor',
        handler: () => {
          setInPoint(currentTime);
          console.log('In point set at:', currentTime);
        },
      },
      {
        id: 'set-out-point',
        keys: 'O',
        description: 'Set Out point',
        context: 'video-editor',
        handler: () => {
          setOutPoint(currentTime);
          console.log('Out point set at:', currentTime);
        },
      },
      {
        id: 'clear-in-out',
        keys: 'Ctrl+Shift+X',
        description: 'Clear In/Out points',
        context: 'video-editor',
        handler: () => {
          setInPoint(null);
          setOutPoint(null);
        },
      },
      // Tool switching (numeric keys)
      {
        id: 'tool-select',
        keys: '1',
        description: 'Select Tool (V)',
        context: 'video-editor',
        handler: () => {
          setSelectedTool('select');
        },
      },
      {
        id: 'tool-razor',
        keys: '2',
        description: 'Razor Tool (C)',
        context: 'video-editor',
        handler: () => {
          setSelectedTool('razor');
        },
      },
      {
        id: 'tool-hand',
        keys: '3',
        description: 'Hand Tool (H)',
        context: 'video-editor',
        handler: () => {
          setSelectedTool('hand');
        },
      },
      // Alternative tool shortcuts (common in video editors)
      {
        id: 'tool-select-v',
        keys: 'V',
        description: 'Select Tool',
        context: 'video-editor',
        handler: () => {
          setSelectedTool('select');
        },
      },
      {
        id: 'tool-razor-c',
        keys: 'C',
        description: 'Razor Tool',
        context: 'video-editor',
        handler: () => {
          setSelectedTool('razor');
        },
      },
      {
        id: 'tool-hand-h',
        keys: 'H',
        description: 'Hand Tool',
        context: 'video-editor',
        handler: () => {
          setSelectedTool('hand');
        },
      },
      // Clip operations
      {
        id: 'delete-clip',
        keys: 'Delete',
        description: 'Delete selected clip',
        context: 'video-editor',
        handler: () => {
          if (selectedClipId) {
            handleDeleteClip();
          }
        },
      },
      {
        id: 'delete-clip-backspace',
        keys: 'Backspace',
        description: 'Delete selected clip',
        context: 'video-editor',
        handler: () => {
          if (selectedClipId) {
            handleDeleteClip();
          }
        },
      },
      {
        id: 'export-video',
        keys: 'Ctrl+E',
        description: 'Export video',
        context: 'video-editor',
        handler: () => {
          handleExportVideo();
        },
      },
      {
        id: 'save-project',
        keys: 'Ctrl+S',
        description: 'Save project',
        context: 'video-editor',
        handler: (e) => {
          e.preventDefault();
          handleSaveProject();
        },
      },
    ]);

    // Clean up on unmount
    return () => {
      keyboardShortcutManager.unregisterContext('video-editor');
    };
    // handleDeleteClip, handleExportVideo, and handleSaveProject are stable functions
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentTime, selectedClipId]);

  const handleUpdateClip = (updates: Partial<TimelineClip>) => {
    if (!selectedClipId) return;

    setClips((prevClips) =>
      prevClips.map((clip) => (clip.id === selectedClipId ? { ...clip, ...updates } : clip))
    );
  };

  const handleUpdateClipById = (clipId: string, updates: Partial<TimelineClip>) => {
    setClips((prevClips) =>
      prevClips.map((clip) => (clip.id === clipId ? { ...clip, ...updates } : clip))
    );
  };

  const handleDeleteClip = () => {
    if (!selectedClipId) return;

    setClips((prevClips) => prevClips.filter((clip) => clip.id !== selectedClipId));
    setSelectedClipId(null);
  };

  const handleAddClip = (_trackId: string, clip: TimelineClip) => {
    setClips((prevClips) => [...prevClips, clip]);
  };

  const handleTrackToggleVisibility = (trackId: string) => {
    setTracks((prevTracks) =>
      prevTracks.map((track) =>
        track.id === trackId ? { ...track, visible: !track.visible } : track
      )
    );
  };

  const handleTrackToggleLock = (trackId: string) => {
    setTracks((prevTracks) =>
      prevTracks.map((track) =>
        track.id === trackId ? { ...track, locked: !track.locked } : track
      )
    );
  };

  const selectedClip = clips.find((clip) => clip.id === selectedClipId);

  const handleImportMedia = () => {
    // Trigger file picker in MediaLibraryPanel
    mediaLibraryRef.current?.openFilePicker();
  };

  const handleExportVideo = () => {
    // Export project as .aura file
    exportProject();
  };

  const handleSaveProject = async () => {
    try {
      if (!projectName) {
        const name = prompt('Enter project name:', 'Untitled Project');
        if (!name) return;
        await saveCurrentProject(name, true);
      } else {
        await saveCurrentProject(projectName, true);
      }
      // Success feedback via console (TODO: use toast notification)
      console.log('Project saved successfully!');
    } catch (error) {
      console.error('Failed to save project:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      // TODO: Replace with toast notification system when available
      console.error(`Failed to save project: ${errorMessage}`);
    }
  };

  const handleApplyPreset = (preset: EffectPreset) => {
    if (!selectedClipId) return;

    // Apply preset effects to selected clip
    const presetEffects: AppliedEffect[] = preset.effects.map((presetEffect, index) => ({
      id: `effect-${Date.now()}-${index}-${Math.random().toString(36).substring(2, 11)}`,
      effectType: presetEffect.effectType,
      enabled: true,
      parameters: presetEffect.parameters,
    }));

    setClips((prevClips) =>
      prevClips.map((clip) =>
        clip.id === selectedClipId
          ? { ...clip, effects: [...(clip.effects || []), ...presetEffects] }
          : clip
      )
    );
  };

  return (
    <EditorLayout
      mediaLibrary={<MediaLibraryPanel ref={mediaLibraryRef} />}
      effects={<EffectsLibraryPanel onPresetApply={handleApplyPreset} />}
      preview={
        <VideoPreviewPanel
          currentTime={currentTime}
          effects={selectedClip?.effects}
          onTimeUpdate={setCurrentTime}
          onPlay={() => setIsPlaying(true)}
          onPause={() => setIsPlaying(false)}
        />
      }
      timeline={
        <TimelinePanel
          clips={clips}
          tracks={tracks}
          currentTime={currentTime}
          onTimeChange={setCurrentTime}
          onClipSelect={setSelectedClipId}
          selectedClipId={selectedClipId}
          onClipAdd={handleAddClip}
          onClipUpdate={handleUpdateClipById}
          onTrackToggleVisibility={handleTrackToggleVisibility}
          onTrackToggleLock={handleTrackToggleLock}
        />
      }
      properties={
        <PropertiesPanel
          selectedClip={selectedClip}
          onUpdateClip={handleUpdateClip}
          onDeleteClip={handleDeleteClip}
        />
      }
      onImportMedia={handleImportMedia}
      onExportVideo={handleExportVideo}
      onShowKeyboardShortcuts={() => setShowKeyboardShortcuts(true)}
      onSaveProject={handleSaveProject}
      projectName={projectName}
      isDirty={isDirty}
      autosaveStatus={autosaveStatus}
      lastSaved={lastSaved}
    />
  );
}
