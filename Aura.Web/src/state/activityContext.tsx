import { createContext, useContext, useState, useCallback, ReactNode, useEffect } from 'react';

export type ActivityStatus = 'pending' | 'running' | 'paused' | 'completed' | 'failed' | 'cancelled';

export type ActivityType = 
  | 'video-generation'
  | 'api-call'
  | 'file-upload'
  | 'analysis'
  | 'render'
  | 'download'
  | 'import'
  | 'export'
  | 'effects'
  | 'other';

export type ActivityCategory = 'import' | 'export' | 'analysis' | 'effects' | 'other';

export interface OperationDetails {
  currentItem?: number;
  totalItems?: number;
  speed?: number; // MB/s or frames/s
  speedUnit?: 'MB/s' | 'frames/s' | 'items/s';
  timeElapsed?: number; // seconds
  timeRemaining?: number; // seconds
  bytesProcessed?: number;
  bytesTotal?: number;
}

export interface Activity {
  id: string;
  type: ActivityType;
  status: ActivityStatus;
  title: string;
  message: string;
  progress: number; // 0-100
  startTime: Date;
  endTime?: Date;
  error?: string;
  canCancel?: boolean;
  canRetry?: boolean;
  canPause?: boolean;
  priority?: number; // 1-10, higher is more important
  metadata?: Record<string, unknown>;
  category?: ActivityCategory;
  details?: OperationDetails;
  batchId?: string; // For grouping related operations
  artifactPath?: string; // Path to output file
}

interface ActivityContextType {
  activities: Activity[];
  activeActivities: Activity[];
  completedActivities: Activity[];
  failedActivities: Activity[];
  queuedActivities: Activity[];
  pausedActivities: Activity[];
  recentHistory: Activity[]; // Last 50 completed/failed operations
  batchOperations: Map<string, Activity[]>; // Grouped by batchId
  addActivity: (activity: Omit<Activity, 'id' | 'startTime' | 'status' | 'progress'>) => string;
  updateActivity: (id: string, updates: Partial<Activity>) => void;
  removeActivity: (id: string) => void;
  pauseActivity: (id: string) => void;
  resumeActivity: (id: string) => void;
  setPriority: (id: string, priority: number) => void;
  clearCompleted: () => void;
  clearAll: () => void;
  clearHistory: () => void;
  getActivity: (id: string) => Activity | undefined;
  getBatchOperations: (batchId: string) => Activity[];
}

const ActivityContext = createContext<ActivityContextType | undefined>(undefined);

const MAX_HISTORY = 50;

export function ActivityProvider({ children }: { children: ReactNode }) {
  const [activities, setActivities] = useState<Activity[]>([]);
  const [history, setHistory] = useState<Activity[]>([]);

  // Update history whenever activities change to completed/failed/cancelled
  useEffect(() => {
    const completedOrFailed = activities.filter(
      a => ['completed', 'failed', 'cancelled'].includes(a.status)
    );
    
    // Merge with existing history and limit to MAX_HISTORY
    setHistory(prev => {
      const merged = [...completedOrFailed, ...prev];
      const unique = Array.from(new Map(merged.map(a => [a.id, a])).values());
      return unique
        .sort((a, b) => (b.endTime?.getTime() || 0) - (a.endTime?.getTime() || 0))
        .slice(0, MAX_HISTORY);
    });
  }, [activities]);

  const addActivity = useCallback((activity: Omit<Activity, 'id' | 'startTime' | 'status' | 'progress'>) => {
    const id = `activity-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    const newActivity: Activity = {
      ...activity,
      id,
      status: 'pending',
      progress: 0,
      startTime: new Date(),
    };
    
    setActivities(prev => [...prev, newActivity]);
    return id;
  }, []);

  const updateActivity = useCallback((id: string, updates: Partial<Activity>) => {
    setActivities(prev => prev.map(activity => {
      if (activity.id === id) {
        const updated = { ...activity, ...updates };
        
        // Set endTime when status changes to completed, failed, or cancelled
        if (
          updates.status && 
          ['completed', 'failed', 'cancelled'].includes(updates.status) &&
          !updated.endTime
        ) {
          updated.endTime = new Date();
        }
        
        return updated;
      }
      return activity;
    }));
  }, []);

  const removeActivity = useCallback((id: string) => {
    setActivities(prev => prev.filter(activity => activity.id !== id));
  }, []);

  const pauseActivity = useCallback((id: string) => {
    setActivities(prev => prev.map(activity => {
      if (activity.id === id && activity.status === 'running') {
        return { ...activity, status: 'paused' as ActivityStatus };
      }
      return activity;
    }));
  }, []);

  const resumeActivity = useCallback((id: string) => {
    setActivities(prev => prev.map(activity => {
      if (activity.id === id && activity.status === 'paused') {
        return { ...activity, status: 'running' as ActivityStatus };
      }
      return activity;
    }));
  }, []);

  const setPriority = useCallback((id: string, priority: number) => {
    setActivities(prev => prev.map(activity => {
      if (activity.id === id) {
        return { ...activity, priority: Math.max(1, Math.min(10, priority)) };
      }
      return activity;
    }));
  }, []);

  const clearCompleted = useCallback(() => {
    setActivities(prev => prev.filter(activity => activity.status !== 'completed'));
  }, []);

  const clearAll = useCallback(() => {
    setActivities([]);
  }, []);

  const clearHistory = useCallback(() => {
    setHistory([]);
  }, []);

  const getActivity = useCallback((id: string) => {
    return activities.find(activity => activity.id === id);
  }, [activities]);

  const getBatchOperations = useCallback((batchId: string) => {
    return activities.filter(activity => activity.batchId === batchId);
  }, [activities]);

  const activeActivities = activities.filter(
    a => a.status === 'pending' || a.status === 'running'
  );

  const completedActivities = activities.filter(
    a => a.status === 'completed'
  );

  const failedActivities = activities.filter(
    a => a.status === 'failed'
  );

  const queuedActivities = activities.filter(
    a => a.status === 'pending'
  ).sort((a, b) => (b.priority || 5) - (a.priority || 5));

  const pausedActivities = activities.filter(
    a => a.status === 'paused'
  );

  // Group activities by batchId
  const batchOperations = new Map<string, Activity[]>();
  activities.forEach(activity => {
    if (activity.batchId) {
      const batch = batchOperations.get(activity.batchId) || [];
      batch.push(activity);
      batchOperations.set(activity.batchId, batch);
    }
  });

  const value: ActivityContextType = {
    activities,
    activeActivities,
    completedActivities,
    failedActivities,
    queuedActivities,
    pausedActivities,
    recentHistory: history,
    batchOperations,
    addActivity,
    updateActivity,
    removeActivity,
    pauseActivity,
    resumeActivity,
    setPriority,
    clearCompleted,
    clearAll,
    clearHistory,
    getActivity,
    getBatchOperations,
  };

  return (
    <ActivityContext.Provider value={value}>
      {children}
    </ActivityContext.Provider>
  );
}

export function useActivity() {
  const context = useContext(ActivityContext);
  if (!context) {
    throw new Error('useActivity must be used within an ActivityProvider');
  }
  return context;
}
