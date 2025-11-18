/**
 * Video Generation IPC Handlers
 * Handles video generation commands and progress updates
 */

const { ipcMain } = require('electron');
const axios = require('axios');

class VideoHandler {
  constructor(backendUrl, networkContract) {
    this.backendUrl = backendUrl;
    this.networkContract = networkContract;
    this.sseJobEventsTemplate = networkContract?.sseJobEventsTemplate || '/api/jobs/{id}/events';
    this.activeGenerations = new Map();
  }

  /**
   * Update backend URL
   */
  setBackendUrl(url) {
    this.backendUrl = url;
  }

  /**
   * Build full job events URL for SSE subscription
   * @param {string} jobId - The job ID to build the URL for
   * @returns {string} - Full URL for SSE job events
   */
  _buildJobEventsUrl(jobId) {
    if (!jobId) {
      throw new Error('Job ID is required to build events URL');
    }
    const path = this.sseJobEventsTemplate.replace('{id}', encodeURIComponent(jobId));
    return new URL(path, this.backendUrl).toString();
  }

  /**
   * Register all video generation IPC handlers
   */
  register() {
    // Start video generation
    ipcMain.handle('video:generate:start', async (event, config) => {
      try {
        // Validate input
        if (!config || typeof config !== 'object') {
          throw new Error('Invalid video generation configuration');
        }

        const response = await axios.post(
          `${this.backendUrl}/api/videos/generate`,
          config,
          {
            timeout: 10000,
            headers: { 'Content-Type': 'application/json' }
          }
        );

        const generationId = response.data.id || response.data.generationId;
        this.activeGenerations.set(generationId, {
          status: 'processing',
          startTime: Date.now()
        });

        return response.data;
      } catch (error) {
        console.error('Error starting video generation:', error);
        throw new Error(`Failed to start video generation: ${error.message}`);
      }
    });

    // Pause video generation
    ipcMain.handle('video:generate:pause', async (event, generationId) => {
      try {
        if (!generationId) {
          throw new Error('Invalid generation ID');
        }

        const response = await axios.post(
          `${this.backendUrl}/api/videos/generate/${generationId}/pause`,
          {},
          { timeout: 5000 }
        );

        const generation = this.activeGenerations.get(generationId);
        if (generation) {
          generation.status = 'paused';
        }

        return response.data;
      } catch (error) {
        console.error('Error pausing video generation:', error);
        throw new Error(`Failed to pause video generation: ${error.message}`);
      }
    });

    // Resume video generation
    ipcMain.handle('video:generate:resume', async (event, generationId) => {
      try {
        if (!generationId) {
          throw new Error('Invalid generation ID');
        }

        const response = await axios.post(
          `${this.backendUrl}/api/videos/generate/${generationId}/resume`,
          {},
          { timeout: 5000 }
        );

        const generation = this.activeGenerations.get(generationId);
        if (generation) {
          generation.status = 'processing';
        }

        return response.data;
      } catch (error) {
        console.error('Error resuming video generation:', error);
        throw new Error(`Failed to resume video generation: ${error.message}`);
      }
    });

    // Cancel video generation
    ipcMain.handle('video:generate:cancel', async (event, generationId) => {
      try {
        if (!generationId) {
          throw new Error('Invalid generation ID');
        }

        const response = await axios.post(
          `${this.backendUrl}/api/videos/generate/${generationId}/cancel`,
          {},
          { timeout: 5000 }
        );

        this.activeGenerations.delete(generationId);

        return response.data;
      } catch (error) {
        console.error('Error canceling video generation:', error);
        throw new Error(`Failed to cancel video generation: ${error.message}`);
      }
    });

    // Get video generation status
    ipcMain.handle('video:generate:status', async (event, generationId) => {
      try {
        if (!generationId) {
          throw new Error('Invalid generation ID');
        }

        const response = await axios.get(
          `${this.backendUrl}/api/videos/generate/${generationId}/status`,
          { timeout: 5000 }
        );

        return response.data;
      } catch (error) {
        console.error('Error getting video status:', error);
        throw new Error(`Failed to get video status: ${error.message}`);
      }
    });

    // Get all active generations
    ipcMain.handle('video:generate:list', async () => {
      try {
        const response = await axios.get(
          `${this.backendUrl}/api/videos/generate`,
          { timeout: 5000 }
        );

        return response.data;
      } catch (error) {
        console.error('Error listing video generations:', error);
        throw new Error(`Failed to list video generations: ${error.message}`);
      }
    });

    console.log('Video generation IPC handlers registered');
  }

  /**
   * Send progress update to renderer
   */
  sendProgress(window, generationId, progress) {
    if (window && !window.isDestroyed()) {
      window.webContents.send('video:progress', {
        generationId,
        progress
      });
    }
  }

  /**
   * Send error to renderer
   */
  sendError(window, generationId, error) {
    if (window && !window.isDestroyed()) {
      window.webContents.send('video:error', {
        generationId,
        error: error.message || error
      });
    }
  }

  /**
   * Send completion notification to renderer
   */
  sendComplete(window, generationId, result) {
    if (window && !window.isDestroyed()) {
      window.webContents.send('video:complete', {
        generationId,
        result
      });
    }
    
    this.activeGenerations.delete(generationId);
  }
}

module.exports = VideoHandler;
