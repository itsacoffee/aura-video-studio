import { createContext, useContext, useState, useCallback, ReactNode } from 'react';

export type ActivityStatus = 'pending' | 'running' | 'completed' | 'failed' | 'cancelled';

export type ActivityType = 
  | 'video-generation'
  | 'api-call'
  | 'file-upload'
  | 'analysis'
  | 'render'
  | 'download'
  | 'other';

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
  metadata?: Record<string, unknown>;
}

interface ActivityContextType {
  activities: Activity[];
  activeActivities: Activity[];
  completedActivities: Activity[];
  failedActivities: Activity[];
  addActivity: (activity: Omit<Activity, 'id' | 'startTime' | 'status' | 'progress'>) => string;
  updateActivity: (id: string, updates: Partial<Activity>) => void;
  removeActivity: (id: string) => void;
  clearCompleted: () => void;
  clearAll: () => void;
  getActivity: (id: string) => Activity | undefined;
}

const ActivityContext = createContext<ActivityContextType | undefined>(undefined);

export function ActivityProvider({ children }: { children: ReactNode }) {
  const [activities, setActivities] = useState<Activity[]>([]);

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

  const clearCompleted = useCallback(() => {
    setActivities(prev => prev.filter(activity => activity.status !== 'completed'));
  }, []);

  const clearAll = useCallback(() => {
    setActivities([]);
  }, []);

  const getActivity = useCallback((id: string) => {
    return activities.find(activity => activity.id === id);
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

  const value: ActivityContextType = {
    activities,
    activeActivities,
    completedActivities,
    failedActivities,
    addActivity,
    updateActivity,
    removeActivity,
    clearCompleted,
    clearAll,
    getActivity,
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
