/**
 * OpenCut Media Store
 *
 * Manages media assets for the OpenCut editor including importing,
 * loading, and managing media files (video, audio, images).
 */

import { create } from 'zustand';

export type MediaType = 'video' | 'audio' | 'image';

export interface OpenCutMediaFile {
  id: string;
  name: string;
  type: MediaType;
  url: string;
  file?: File;
  duration?: number;
  width?: number;
  height?: number;
  thumbnailUrl?: string;
  fps?: number;
}

export interface OpenCutMediaState {
  mediaFiles: OpenCutMediaFile[];
  isLoading: boolean;
  selectedMediaId: string | null;
  error: string | null;
}

export interface OpenCutMediaActions {
  addMediaFile: (file: File) => Promise<OpenCutMediaFile | null>;
  removeMediaFile: (id: string) => void;
  selectMedia: (id: string | null) => void;
  clearAllMedia: () => void;
  getMediaById: (id: string) => OpenCutMediaFile | undefined;
}

export type OpenCutMediaStore = OpenCutMediaState & OpenCutMediaActions;

function generateId(): string {
  return `media-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

function getFileType(file: File): MediaType | null {
  const { type } = file;
  if (type.startsWith('image/')) return 'image';
  if (type.startsWith('video/')) return 'video';
  if (type.startsWith('audio/')) return 'audio';
  return null;
}

async function getImageDimensions(file: File): Promise<{ width: number; height: number }> {
  return new Promise((resolve, reject) => {
    const img = new window.Image();
    img.addEventListener('load', () => {
      resolve({ width: img.naturalWidth, height: img.naturalHeight });
      URL.revokeObjectURL(img.src);
    });
    img.addEventListener('error', () => {
      reject(new Error('Could not load image'));
      URL.revokeObjectURL(img.src);
    });
    img.src = URL.createObjectURL(file);
  });
}

async function getMediaDuration(file: File): Promise<number> {
  return new Promise((resolve, reject) => {
    const element = document.createElement(file.type.startsWith('video/') ? 'video' : 'audio');
    element.addEventListener('loadedmetadata', () => {
      resolve(element.duration);
      URL.revokeObjectURL(element.src);
    });
    element.addEventListener('error', () => {
      reject(new Error('Could not load media'));
      URL.revokeObjectURL(element.src);
    });
    element.src = URL.createObjectURL(file);
    element.load();
  });
}

async function generateVideoThumbnail(
  file: File
): Promise<{ thumbnailUrl: string; width: number; height: number; fps?: number }> {
  return new Promise((resolve, reject) => {
    const video = document.createElement('video');
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');

    if (!ctx) {
      reject(new Error('Could not get canvas context'));
      return;
    }

    video.addEventListener('loadedmetadata', () => {
      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;
      video.currentTime = Math.min(1, video.duration * 0.1);
    });

    video.addEventListener('seeked', () => {
      ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
      const thumbnailUrl = canvas.toDataURL('image/jpeg', 0.8);
      resolve({
        thumbnailUrl,
        width: video.videoWidth,
        height: video.videoHeight,
      });
      URL.revokeObjectURL(video.src);
    });

    video.addEventListener('error', () => {
      reject(new Error('Could not load video'));
      URL.revokeObjectURL(video.src);
    });

    video.src = URL.createObjectURL(file);
    video.load();
  });
}

export const useOpenCutMediaStore = create<OpenCutMediaStore>((set, get) => ({
  mediaFiles: [],
  isLoading: false,
  selectedMediaId: null,
  error: null,

  addMediaFile: async (file: File): Promise<OpenCutMediaFile | null> => {
    set({ isLoading: true, error: null });

    try {
      const fileType = getFileType(file);
      if (!fileType) {
        set({ isLoading: false, error: 'Unsupported file type' });
        return null;
      }

      const mediaFile: OpenCutMediaFile = {
        id: generateId(),
        name: file.name,
        type: fileType,
        url: URL.createObjectURL(file),
        file,
      };

      if (fileType === 'image') {
        const { width, height } = await getImageDimensions(file);
        mediaFile.width = width;
        mediaFile.height = height;
      } else if (fileType === 'video') {
        const [duration, thumbnail] = await Promise.all([
          getMediaDuration(file),
          generateVideoThumbnail(file),
        ]);
        mediaFile.duration = duration;
        mediaFile.thumbnailUrl = thumbnail.thumbnailUrl;
        mediaFile.width = thumbnail.width;
        mediaFile.height = thumbnail.height;
        mediaFile.fps = thumbnail.fps;
      } else if (fileType === 'audio') {
        mediaFile.duration = await getMediaDuration(file);
      }

      set((state) => ({
        mediaFiles: [...state.mediaFiles, mediaFile],
        isLoading: false,
      }));

      return mediaFile;
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to add media file';
      set({ isLoading: false, error: errorMessage });
      return null;
    }
  },

  removeMediaFile: (id: string) => {
    const { mediaFiles, selectedMediaId } = get();
    const mediaFile = mediaFiles.find((m) => m.id === id);

    if (mediaFile?.url) {
      URL.revokeObjectURL(mediaFile.url);
    }
    if (mediaFile?.thumbnailUrl) {
      URL.revokeObjectURL(mediaFile.thumbnailUrl);
    }

    set({
      mediaFiles: mediaFiles.filter((m) => m.id !== id),
      selectedMediaId: selectedMediaId === id ? null : selectedMediaId,
    });
  },

  selectMedia: (id: string | null) => {
    set({ selectedMediaId: id });
  },

  clearAllMedia: () => {
    const { mediaFiles } = get();
    mediaFiles.forEach((file) => {
      if (file.url) URL.revokeObjectURL(file.url);
      if (file.thumbnailUrl) URL.revokeObjectURL(file.thumbnailUrl);
    });
    set({ mediaFiles: [], selectedMediaId: null });
  },

  getMediaById: (id: string) => {
    return get().mediaFiles.find((m) => m.id === id);
  },
}));
