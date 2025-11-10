import { useMutation, useQueryClient } from '@tanstack/react-query';
import { mediaLibraryApi } from '../api/mediaLibraryApi';
import type { MediaType, MediaSource } from '../types/mediaLibrary';

interface SaveGeneratedMediaParams {
  file: File;
  type: MediaType;
  projectId?: string;
  projectName?: string;
  description?: string;
  tags?: string[];
}

interface LinkMediaToProjectParams {
  mediaId: string;
  projectId: string;
  projectName?: string;
}

/**
 * Hook for integrating media library with video generation workflows
 */
export function useMediaGeneration() {
  const queryClient = useQueryClient();

  // Save generated media to library
  const saveGeneratedMedia = useMutation({
    mutationFn: async (params: SaveGeneratedMediaParams) => {
      const { file, type, projectId, projectName, description, tags } = params;
      
      const request = {
        fileName: file.name,
        type,
        source: 'Generated' as MediaSource,
        description: description || `Generated ${type.toLowerCase()} from ${projectName || 'project'}`,
        tags: tags || ['generated', projectId || ''].filter(Boolean),
        generateThumbnail: true,
        extractMetadata: true,
      };

      return mediaLibraryApi.uploadMedia(file, request);
    },
    onSuccess: (data, variables) => {
      // Invalidate relevant queries
      queryClient.invalidateQueries({ queryKey: ['media'] });
      queryClient.invalidateQueries({ queryKey: ['media-stats'] });
      
      // If project ID provided, track the usage
      if (variables.projectId) {
        mediaLibraryApi.trackMediaUsage(data.id, {
          projectId: variables.projectId,
          projectName: variables.projectName,
        });
      }
    },
  });

  // Link existing media to a project
  const linkMediaToProject = useMutation({
    mutationFn: async (params: LinkMediaToProjectParams) => {
      return mediaLibraryApi.trackMediaUsage(params.mediaId, {
        projectId: params.projectId,
        projectName: params.projectName,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['media'] });
    },
  });

  // Get media for a project
  const getProjectMedia = async (projectId: string) => {
    const result = await mediaLibraryApi.searchMedia({
      searchTerm: projectId,
      pageSize: 1000,
    });
    return result.items;
  };

  return {
    saveGeneratedMedia,
    linkMediaToProject,
    getProjectMedia,
  };
}

/**
 * Track media usage when media is used in a project
 */
export function useTrackMediaUsage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      mediaId,
      projectId,
      projectName,
    }: {
      mediaId: string;
      projectId: string;
      projectName?: string;
    }) => {
      return mediaLibraryApi.trackMediaUsage(mediaId, { projectId, projectName });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['media'] });
    },
  });
}
