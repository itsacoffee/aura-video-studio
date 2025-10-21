/**
 * Snapping service for timeline alignment
 */

export interface SnapPoint {
  position: number;
  type: 'playhead' | 'scene-start' | 'scene-end' | 'grid' | 'marker';
  priority: number;
}

export interface SnapResult {
  snapped: boolean;
  position: number;
  snapPoint?: SnapPoint;
}

export class SnappingService {
  private snapThreshold = 8; // pixels
  private enabled = true;

  /**
   * Set snap threshold in pixels
   */
  public setSnapThreshold(threshold: number): void {
    this.snapThreshold = threshold;
  }

  /**
   * Enable or disable snapping
   */
  public setEnabled(enabled: boolean): void {
    this.enabled = enabled;
  }

  /**
   * Calculate snap position if near a snap point
   */
  public calculateSnapPosition(
    dragPosition: number,
    snapPoints: SnapPoint[],
    pixelsPerSecond: number
  ): SnapResult {
    if (!this.enabled || snapPoints.length === 0) {
      return { snapped: false, position: dragPosition };
    }

    // Convert threshold to time units
    const thresholdTime = this.snapThreshold / pixelsPerSecond;

    // Find closest snap point within threshold, prioritizing by priority
    let closestPoint: SnapPoint | undefined;
    let closestDistance = Infinity;

    // Sort by priority first, then by distance
    const sortedPoints = [...snapPoints].sort((a, b) => a.priority - b.priority);

    for (const point of sortedPoints) {
      const distance = Math.abs(point.position - dragPosition);
      
      if (distance <= thresholdTime) {
        // If this point has higher priority (lower number) or same priority but closer
        if (!closestPoint || point.priority < closestPoint.priority || 
            (point.priority === closestPoint.priority && distance < closestDistance)) {
          closestDistance = distance;
          closestPoint = point;
        }
      }
    }

    if (closestPoint) {
      return {
        snapped: true,
        position: closestPoint.position,
        snapPoint: closestPoint,
      };
    }

    return { snapped: false, position: dragPosition };
  }

  /**
   * Generate snap points for timeline
   */
  public generateSnapPoints(
    playheadPosition: number,
    sceneStarts: number[],
    sceneEnds: number[],
    gridInterval: number,
    timelineDuration: number,
    markerPositions: number[] = []
  ): SnapPoint[] {
    const points: SnapPoint[] = [];

    // Playhead (highest priority)
    points.push({
      position: playheadPosition,
      type: 'playhead',
      priority: 1,
    });

    // Scene starts and ends
    sceneStarts.forEach(start => {
      points.push({
        position: start,
        type: 'scene-start',
        priority: 2,
      });
    });

    sceneEnds.forEach(end => {
      points.push({
        position: end,
        type: 'scene-end',
        priority: 2,
      });
    });

    // Grid lines
    for (let time = 0; time <= timelineDuration; time += gridInterval) {
      points.push({
        position: time,
        type: 'grid',
        priority: 3,
      });
    }

    // Markers
    markerPositions.forEach(pos => {
      points.push({
        position: pos,
        type: 'marker',
        priority: 2,
      });
    });

    // Sort by priority (lower number = higher priority)
    return points.sort((a, b) => a.priority - b.priority);
  }

  /**
   * Get appropriate grid interval based on zoom level
   */
  public getGridInterval(pixelsPerSecond: number): number {
    // Adjust grid interval based on zoom level
    if (pixelsPerSecond >= 100) return 1; // 1 second
    if (pixelsPerSecond >= 20) return 5; // 5 seconds
    if (pixelsPerSecond >= 10) return 10; // 10 seconds
    if (pixelsPerSecond >= 5) return 30; // 30 seconds
    return 60; // 1 minute
  }
}

// Singleton instance
export const snappingService = new SnappingService();
