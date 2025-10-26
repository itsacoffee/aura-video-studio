/**
 * Motion Tracking Service
 * Track objects in video for locked graphics and effects
 */

export interface TrackingPoint {
  id: string;
  name: string;
  x: number;
  y: number;
  confidence: number; // 0-1
  timestamp: number; // frame timestamp
}

export interface TrackingPath {
  id: string;
  name: string;
  points: TrackingPoint[];
  startFrame: number;
  endFrame: number;
}

export interface TrackingTarget {
  id: string;
  name: string;
  boundingBox: {
    x: number;
    y: number;
    width: number;
    height: number;
  };
  center: { x: number; y: number };
}

/**
 * Simple optical flow-based tracker using frame differencing
 * Note: This is a simplified implementation. Production would use more sophisticated
 * algorithms like KLT (Kanade-Lucas-Tomasi) or feature matching
 */
export class MotionTracker {
  private prevFrame: ImageData | null = null;
  private trackingPoints: Map<string, TrackingPoint[]> = new Map();

  /**
   * Initialize tracking for a point
   */
  public startTracking(
    pointId: string,
    pointName: string,
    x: number,
    y: number,
    timestamp: number
  ): void {
    const point: TrackingPoint = {
      id: `${pointId}-${timestamp}`,
      name: pointName,
      x,
      y,
      confidence: 1.0,
      timestamp,
    };

    if (!this.trackingPoints.has(pointId)) {
      this.trackingPoints.set(pointId, []);
    }
    this.trackingPoints.get(pointId)!.push(point);
  }

  /**
   * Track point in new frame
   */
  public trackFrame(
    pointId: string,
    currentFrame: ImageData,
    timestamp: number
  ): TrackingPoint | null {
    const points = this.trackingPoints.get(pointId);
    if (!points || points.length === 0) {
      return null;
    }

    const lastPoint = points[points.length - 1];

    if (!this.prevFrame) {
      this.prevFrame = currentFrame;
      return lastPoint;
    }

    // Simple template matching in a search window
    const searchRadius = 50; // pixels
    const templateSize = 15; // pixels

    let bestMatch = { x: lastPoint.x, y: lastPoint.y, confidence: 0 };
    let bestScore = Infinity;

    // Search in a window around the last known position
    for (let dy = -searchRadius; dy <= searchRadius; dy += 2) {
      for (let dx = -searchRadius; dx <= searchRadius; dx += 2) {
        const testX = Math.round(lastPoint.x + dx);
        const testY = Math.round(lastPoint.y + dy);

        // Calculate similarity score
        const score = this.calculateTemplateSimilarity(
          this.prevFrame,
          currentFrame,
          lastPoint.x,
          lastPoint.y,
          testX,
          testY,
          templateSize
        );

        if (score < bestScore) {
          bestScore = score;
          bestMatch = {
            x: testX,
            y: testY,
            confidence: Math.max(0, 1 - score / 1000),
          };
        }
      }
    }

    const newPoint: TrackingPoint = {
      id: `${pointId}-${timestamp}`,
      name: lastPoint.name,
      x: bestMatch.x,
      y: bestMatch.y,
      confidence: bestMatch.confidence,
      timestamp,
    };

    points.push(newPoint);
    this.prevFrame = currentFrame;

    return newPoint;
  }

  /**
   * Calculate template similarity using sum of squared differences
   */
  private calculateTemplateSimilarity(
    prevFrame: ImageData,
    currFrame: ImageData,
    prevX: number,
    prevY: number,
    currX: number,
    currY: number,
    templateSize: number
  ): number {
    let ssd = 0;
    const halfSize = Math.floor(templateSize / 2);

    for (let dy = -halfSize; dy <= halfSize; dy++) {
      for (let dx = -halfSize; dx <= halfSize; dx++) {
        const prevIdx = this.getPixelIndex(
          prevFrame.width,
          prevX + dx,
          prevY + dy
        );
        const currIdx = this.getPixelIndex(
          currFrame.width,
          currX + dx,
          currY + dy
        );

        if (
          prevIdx >= 0 &&
          currIdx >= 0 &&
          prevIdx < prevFrame.data.length &&
          currIdx < currFrame.data.length
        ) {
          // Compare RGB values
          for (let c = 0; c < 3; c++) {
            const diff = prevFrame.data[prevIdx + c] - currFrame.data[currIdx + c];
            ssd += diff * diff;
          }
        }
      }
    }

    return ssd;
  }

  /**
   * Get pixel index in ImageData
   */
  private getPixelIndex(width: number, x: number, y: number): number {
    return (Math.floor(y) * width + Math.floor(x)) * 4;
  }

  /**
   * Get tracking path for a point
   */
  public getTrackingPath(pointId: string): TrackingPath | null {
    const points = this.trackingPoints.get(pointId);
    if (!points || points.length === 0) {
      return null;
    }

    return {
      id: pointId,
      name: points[0].name,
      points,
      startFrame: points[0].timestamp,
      endFrame: points[points.length - 1].timestamp,
    };
  }

  /**
   * Get interpolated position at a specific time
   */
  public getPositionAtTime(pointId: string, timestamp: number): { x: number; y: number } | null {
    const points = this.trackingPoints.get(pointId);
    if (!points || points.length === 0) {
      return null;
    }

    // Find the two points to interpolate between
    let before: TrackingPoint | null = null;
    let after: TrackingPoint | null = null;

    for (const point of points) {
      if (point.timestamp <= timestamp) {
        before = point;
      }
      if (point.timestamp >= timestamp && !after) {
        after = point;
      }
    }

    if (!before) return { x: points[0].x, y: points[0].y };
    if (!after) return { x: points[points.length - 1].x, y: points[points.length - 1].y };
    if (before === after) return { x: before.x, y: before.y };

    // Linear interpolation
    const t = (timestamp - before.timestamp) / (after.timestamp - before.timestamp);
    return {
      x: before.x + (after.x - before.x) * t,
      y: before.y + (after.y - before.y) * t,
    };
  }

  /**
   * Clear all tracking data
   */
  public clear(): void {
    this.trackingPoints.clear();
    this.prevFrame = null;
  }

  /**
   * Remove tracking for a specific point
   */
  public removeTracking(pointId: string): void {
    this.trackingPoints.delete(pointId);
  }

  /**
   * Export tracking data
   */
  public exportTrackingData(): Record<string, TrackingPath> {
    const result: Record<string, TrackingPath> = {};

    this.trackingPoints.forEach((points, pointId) => {
      if (points.length > 0) {
        result[pointId] = {
          id: pointId,
          name: points[0].name,
          points,
          startFrame: points[0].timestamp,
          endFrame: points[points.length - 1].timestamp,
        };
      }
    });

    return result;
  }

  /**
   * Import tracking data
   */
  public importTrackingData(data: Record<string, TrackingPath>): void {
    Object.entries(data).forEach(([pointId, path]) => {
      this.trackingPoints.set(pointId, path.points);
    });
  }
}

/**
 * Singleton instance
 */
export const motionTracker = new MotionTracker();
