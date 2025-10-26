import { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  AddClipCommand,
  DeleteClipCommand,
  UpdatePropertyCommand,
  AddEffectCommand,
} from '../commands/clipCommands';
import { EditorLayout } from '../components/EditorLayout/EditorLayout';
import { EffectsLibraryPanel } from '../components/EditorLayout/EffectsLibraryPanel';
import { HistoryPanel } from '../components/EditorLayout/HistoryPanel';
import { MediaLibraryPanel } from '../components/EditorLayout/MediaLibraryPanel';
import { PropertiesPanel } from '../components/EditorLayout/PropertiesPanel';
import { TimelinePanel } from '../components/EditorLayout/TimelinePanel';
import {
  VideoPreviewPanel,
  VideoPreviewPanelHandle,
} from '../components/EditorLayout/VideoPreviewPanel';
import { ExportDialog, ExportOptions } from '../components/Export/ExportDialog';
import { useProjectState } from '../hooks/useProjectState';
import { CommandHistory } from '../services/commandHistory';
import { startExport, pollExportStatus } from '../services/exportService';
import { getHardwareInfo, HardwareInfo } from '../services/hardwareService';
import { keyboardShortcutManager } from '../services/keyboardShortcutManager';
import { useActivity } from '../state/activityContext';
import { AppliedEffect, EffectPreset } from '../types/effects';
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
  const [, setIsPlaying] = useState(false);
  const [, setPlaybackSpeed] = useState(1.0);
  const [, setInPoint] = useState<number | null>(null);
  const [, setOutPoint] = useState<number | null>(null);
  const [, setSelectedTool] = useState<'select' | 'razor' | 'hand'>('select');
  const [tracks, setTracks] = useState<TimelineTrack[]>([
    { id: 'video1', label: 'Video 1', type: 'video', visible: true, locked: false },
    { id: 'video2', label: 'Video 2', type: 'video', visible: true, locked: false },
    { id: 'audio1', label: 'Audio 1', type: 'audio', visible: true, locked: false },
    { id: 'audio2', label: 'Audio 2', type: 'audio', visible: true, locked: false },
  ]);
  const [mediaLibrary, setMediaLibrary] = useState<ProjectMediaItem[]>([]);
  const [showExportDialog, setShowExportDialog] = useState(false);
  const [hardwareInfo, setHardwareInfo] = useState<HardwareInfo | null>(null);

  // Access activity context for progress tracking
  const { addActivity, updateActivity } = useActivity();

  // Command history for undo/redo
  const commandHistory = useMemo(() => new CommandHistory(50), []);
  const [canUndo, setCanUndo] = useState(false);
  const [canRedo, setCanRedo] = useState(false);

  // Subscribe to command history changes
  useEffect(() => {
    return commandHistory.subscribe((undo, redo) => {
      setCanUndo(undo);
      setCanRedo(redo);
    });
  }, [commandHistory]);

  // Prevent unused variable warnings - these will be used for UI indicators
  void canUndo;
  void canRedo;

  // Project state management with autosave
  const handleProjectLoaded = useCallback(
    (project: ProjectFile) => {
      // Clear command history on project load
      commandHistory.clear();

      // Restore clips
      setClips(
        project.clips.map((clip) => ({
          ...clip,
          file: undefined, // Files can't be serialized, will need to be re-added
        }))
      );

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
    },
    [commandHistory]
  );

  const {
    projectName,
    isDirty,
    autosaveStatus,
    lastSaved,
    saveCurrentProject,
    exportProject, // Keep for potential future use (project file export)
    loadProject,
  } = useProjectState(clips, tracks, mediaLibrary, currentTime, handleProjectLoaded);

  // Prevent unused variable warning
  void exportProject;

  // Load project from URL parameter
  useEffect(() => {
    const projectId = searchParams.get('projectId');
    if (projectId) {
      loadProject(projectId).catch((error) => {
        console.error('Failed to load project:', error);
      });
    }
  }, [searchParams, loadProject]);

  // Detect hardware capabilities on mount
  useEffect(() => {
    getHardwareInfo()
      .then(setHardwareInfo)
      .catch((error) => {
        console.error('Failed to detect hardware:', error);
        // Set fallback values
        setHardwareInfo({
          cpuCores: 4,
          ramGB: 8,
          gpu: null,
          hardwareAccelerationAvailable: false,
          hardwareType: 'None',
          encoderType: 'Software (CPU)',
        });
      });
  }, []);

  // Ref to track video preview controls
  const videoPreviewRef = useRef<VideoPreviewPanelHandle | null>(null);

  // Ref to media library panel for triggering file picker
  const mediaLibraryRef = useRef<{ openFilePicker: () => void } | null>(null);

  // Register keyboard shortcuts for video editor
  useEffect(() => {
    // Set the active context
    keyboardShortcutManager.setActiveContext('video-editor');

    // Register video editor specific shortcuts
    keyboardShortcutManager.registerMultiple([
      // Undo/Redo
      {
        id: 'undo',
        keys: 'Ctrl+Z',
        description: 'Undo',
        context: 'video-editor',
        handler: (e) => {
          e.preventDefault();
          commandHistory.undo();
        },
      },
      {
        id: 'redo',
        keys: 'Ctrl+Y',
        description: 'Redo',
        context: 'video-editor',
        handler: (e) => {
          e.preventDefault();
          commandHistory.redo();
        },
      },
      {
        id: 'redo-shift',
        keys: 'Ctrl+Shift+Z',
        description: 'Redo',
        context: 'video-editor',
        handler: (e) => {
          e.preventDefault();
          commandHistory.redo();
        },
      },
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
        },
      },
      {
        id: 'set-out-point',
        keys: 'O',
        description: 'Set Out point',
        context: 'video-editor',
        handler: () => {
          setOutPoint(currentTime);
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
      // Play around current position
      {
        id: 'play-around',
        keys: '/',
        description: 'Play around current position (2s before/after)',
        context: 'video-editor',
        handler: () => {
          videoPreviewRef.current?.playAround(2, 2);
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
  }, [currentTime, selectedClipId, commandHistory]);

  const handleUpdateClip = (updates: Partial<TimelineClip>) => {
    if (!selectedClipId) return;

    const command = new UpdatePropertyCommand(selectedClipId, updates, clips, setClips);
    commandHistory.execute(command);
  };

  const handleUpdateClipById = (clipId: string, updates: Partial<TimelineClip>) => {
    const command = new UpdatePropertyCommand(clipId, updates, clips, setClips);
    commandHistory.execute(command);
  };

  const handleDeleteClip = () => {
    if (!selectedClipId) return;

    const command = new DeleteClipCommand(selectedClipId, clips, setClips);
    commandHistory.execute(command);
    setSelectedClipId(null);
  };

  const handleAddClip = (_trackId: string, clip: TimelineClip) => {
    const command = new AddClipCommand(clip, setClips);
    commandHistory.execute(command);
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
    // Show export dialog
    setShowExportDialog(true);
  };

  const handleExportDialogClose = () => {
    setShowExportDialog(false);
  };

  // Convert timeline clips to EditableTimeline format for export
  const buildTimelineForExport = () => {
    // Group clips by track and time to create scenes
    const scenes: any[] = [];

    // Sort clips by start time
    const sortedClips = [...clips].sort((a, b) => a.startTime - b.startTime);

    if (sortedClips.length === 0) {
      // Create a default scene if no clips
      scenes.push({
        index: 0,
        heading: 'Scene 1',
        script: 'Empty scene',
        start: '00:00:00',
        duration: '00:00:05',
        visualAssets: [],
        transitionType: 'None',
      });
    } else {
      // Create scenes from clips
      // For simplicity, we'll create one scene per clip for now
      sortedClips.forEach((clip, index) => {
        const assets: any[] = [];

        // If clip has a media reference, add it as an asset
        if (clip.file || clip.preview) {
          // For timeline rendering, we need actual file paths
          // In a real scenario, uploaded files would be available on the server
          // For now, we'll only include clips with preview URLs (which are server-accessible)
          const filePath = clip.preview;

          if (filePath) {
            // Map asset type
            const assetType =
              clip.type === 'video' ? 'Video' : clip.type === 'audio' ? 'Audio' : 'Image';

            // Build effects object from clip effects if present
            let effectsConfig = undefined;
            if (clip.effects && clip.effects.length > 0) {
              // For now, we'll use default values as effect mapping would require
              // knowing the specific effect types and their parameters
              effectsConfig = {
                brightness: 1.0,
                contrast: 1.0,
                saturation: 1.0,
              };
            }

            assets.push({
              id: clip.id,
              type: assetType,
              filePath: filePath,
              start: '00:00:00',
              duration: formatTimeSpan(clip.duration),
              position: {
                x: clip.transform?.x || 0,
                y: clip.transform?.y || 0,
                width: 100,
                height: 100,
              },
              zIndex: 0,
              opacity: 1.0,
              effects: effectsConfig,
            });
          }
        }

        scenes.push({
          index: index,
          heading: clip.label || `Scene ${index + 1}`,
          script: clip.prompt || '',
          start: formatTimeSpan(clip.startTime),
          duration: formatTimeSpan(clip.duration),
          visualAssets: assets,
          transitionType: 'Fade',
          transitionDuration: '00:00:00.5',
        });
      });
    }

    return {
      scenes: scenes,
      backgroundMusicPath: undefined,
      subtitles: {
        enabled: false,
      },
    };
  };

  // Helper function to format seconds to TimeSpan string (HH:MM:SS)
  const formatTimeSpan = (seconds: number): string => {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = Math.floor(seconds % 60);
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  const handleStartExport = async (options: ExportOptions) => {
    try {
      // Close the dialog
      setShowExportDialog(false);

      // Build timeline from current clips
      const timeline = buildTimelineForExport();

      // Create export request with timeline data
      const exportRequest = {
        outputFile: options.outputPath,
        presetName: options.preset,
        timeline: timeline,
      };

      // Start the export
      const response = await startExport(exportRequest);

      // Add activity to track progress
      const activityId = addActivity({
        type: 'render',
        title: `Exporting ${options.preset}`,
        message: `Starting export to ${options.outputPath}`,
        canCancel: true,
        canRetry: false,
      });

      // Update activity to running state
      updateActivity(activityId, {
        status: 'running',
        progress: 0,
      });

      // Poll for progress
      try {
        await pollExportStatus(response.jobId, (job) => {
          updateActivity(activityId, {
            status: 'running',
            progress: Math.round(job.progress),
            message: `Exporting... ${Math.round(job.progress)}%`,
          });
        });

        // Export completed successfully
        updateActivity(activityId, {
          status: 'completed',
          progress: 100,
          message: `Export completed: ${options.outputPath}`,
        });
      } catch (error) {
        // Export failed
        updateActivity(activityId, {
          status: 'failed',
          error: error instanceof Error ? error.message : 'Export failed',
          message: 'Export failed',
        });
      }
    } catch (error) {
      console.error('Failed to start export:', error);
      // Could show a toast notification here
    }
  };

  const handleAddToQueue = async (options: ExportOptions) => {
    // Same as handleStartExport for now
    // In a full implementation, this would add to a queue without starting immediately
    await handleStartExport(options);
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
    } catch (error) {
      console.error('Failed to save project:', error);
    }
  };

  const handleApplyPreset = (preset: EffectPreset) => {
    if (!selectedClipId) return;

    // Apply preset effects to selected clip as a batch
    const presetEffects: AppliedEffect[] = preset.effects.map((presetEffect, index) => ({
      id: `effect-${Date.now()}-${index}-${Math.random().toString(36).substring(2, 11)}`,
      effectType: presetEffect.effectType,
      enabled: true,
      parameters: presetEffect.parameters,
    }));

    // Use command for each effect
    presetEffects.forEach((effect) => {
      const command = new AddEffectCommand(selectedClipId, effect, setClips);
      commandHistory.execute(command);
    });
  };

  return (
    <>
      <EditorLayout
        mediaLibrary={<MediaLibraryPanel ref={mediaLibraryRef} />}
        effects={<EffectsLibraryPanel onPresetApply={handleApplyPreset} />}
        history={<HistoryPanel commandHistory={commandHistory} />}
        preview={
          <VideoPreviewPanel
            ref={videoPreviewRef}
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
      {showExportDialog && (
        <ExportDialog
          open={showExportDialog}
          onClose={handleExportDialogClose}
          onExport={handleStartExport}
          onAddToQueue={handleAddToQueue}
          timeline={{ totalDuration: 180 }}
          hardwareAccelerationAvailable={hardwareInfo?.hardwareAccelerationAvailable ?? false}
          hardwareType={hardwareInfo?.hardwareType ?? 'None'}
        />
      )}
    </>
  );
}
