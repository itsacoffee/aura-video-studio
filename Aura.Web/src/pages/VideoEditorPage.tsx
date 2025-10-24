import { useState, useEffect } from 'react';
import { EditorLayout } from '../components/EditorLayout/EditorLayout';
import { VideoPreviewPanel } from '../components/EditorLayout/VideoPreviewPanel';
import { TimelinePanel } from '../components/EditorLayout/TimelinePanel';
import { PropertiesPanel } from '../components/EditorLayout/PropertiesPanel';

interface TimelineClip {
  id: string;
  trackId: string;
  startTime: number;
  duration: number;
  label: string;
  type: 'video' | 'audio' | 'image';
  prompt?: string;
  effects?: string[];
}

export function VideoEditorPage() {
  const [currentTime, setCurrentTime] = useState(0);
  const [selectedClipId, setSelectedClipId] = useState<string | null>(null);
  const [clips, setClips] = useState<TimelineClip[]>([
    {
      id: 'clip1',
      trackId: 'video1',
      startTime: 0,
      duration: 3,
      label: 'Sample Video 1',
      type: 'video',
      prompt: 'AI-generated landscape',
    },
    {
      id: 'clip2',
      trackId: 'video1',
      startTime: 3.5,
      duration: 2.5,
      label: 'Sample Video 2',
      type: 'video',
      prompt: 'AI-generated cityscape',
    },
    {
      id: 'audio1',
      trackId: 'audio1',
      startTime: 0,
      duration: 5.5,
      label: 'Background Music',
      type: 'audio',
    },
  ]);
  const [, setShowKeyboardShortcuts] = useState(false);

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Space for play/pause
      if (e.code === 'Space' && !e.ctrlKey && !e.shiftKey) {
        e.preventDefault();
        // Play/pause handled by VideoPreviewPanel
      }
      // Ctrl+K for keyboard shortcuts
      else if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        setShowKeyboardShortcuts(true);
      }
      // Ctrl+S for save (prevent browser save)
      else if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        console.log('Save project');
      }
      // Ctrl+E for export
      else if ((e.ctrlKey || e.metaKey) && e.key === 'e') {
        e.preventDefault();
        console.log('Export video');
      }
      // Delete key to delete selected clip
      else if (e.key === 'Delete' && selectedClipId) {
        e.preventDefault();
        handleDeleteClip();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [selectedClipId]);

  const handleUpdateClip = (updates: Partial<TimelineClip>) => {
    if (!selectedClipId) return;

    setClips((prevClips) =>
      prevClips.map((clip) => (clip.id === selectedClipId ? { ...clip, ...updates } : clip))
    );
  };

  const handleDeleteClip = () => {
    if (!selectedClipId) return;

    setClips((prevClips) => prevClips.filter((clip) => clip.id !== selectedClipId));
    setSelectedClipId(null);
  };

  const selectedClip = clips.find((clip) => clip.id === selectedClipId);

  const handleImportMedia = () => {
    console.log('Import media');
    // TODO: Implement media import
  };

  const handleExportVideo = () => {
    console.log('Export video');
    // TODO: Implement video export
  };

  return (
    <EditorLayout
      preview={
        <VideoPreviewPanel
          currentTime={currentTime}
          onTimeUpdate={setCurrentTime}
        />
      }
      timeline={
        <TimelinePanel
          clips={clips}
          currentTime={currentTime}
          onTimeChange={setCurrentTime}
          onClipSelect={setSelectedClipId}
          selectedClipId={selectedClipId}
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
    />
  );
}
