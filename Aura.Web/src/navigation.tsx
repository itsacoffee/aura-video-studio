import {
  Home24Regular,
  VideoClip24Regular,
  Settings24Regular,
  Play24Regular,
  Document24Regular,
  CloudArrowDown24Regular,
  Share24Regular,
  Timeline24Regular,
  DocumentBulletList24Regular,
  Folder24Regular,
  TaskListSquareLtr24Regular,
  HeartPulse24Regular,
  Image24Regular,
  Lightbulb24Regular,
  DataTrending24Regular,
  ChannelShare24Regular,
  ChartMultiple24Regular,
} from '@fluentui/react-icons';

export interface NavItem {
  key: string;
  name: string;
  icon: React.ComponentType;
  path: string;
}

export const navItems: NavItem[] = [
  { key: 'home', name: 'Welcome', icon: Home24Regular, path: '/' },
  { key: 'dashboard', name: 'Dashboard', icon: Document24Regular, path: '/dashboard' },
  { key: 'ideation', name: 'Ideation', icon: Lightbulb24Regular, path: '/ideation' },
  { key: 'trending', name: 'Trending Topics', icon: DataTrending24Regular, path: '/trending' },
  { key: 'create', name: 'Create', icon: VideoClip24Regular, path: '/create' },
  { key: 'projects', name: 'Projects', icon: Folder24Regular, path: '/projects' },
  { key: 'assets', name: 'Asset Library', icon: Image24Regular, path: '/assets' },
  { key: 'timeline', name: 'Timeline', icon: Timeline24Regular, path: '/timeline' },
  { key: 'render', name: 'Render', icon: Play24Regular, path: '/render' },
  { key: 'platform', name: 'Platform Optimizer', icon: ChannelShare24Regular, path: '/platform' },
  { key: 'quality', name: 'Quality Dashboard', icon: ChartMultiple24Regular, path: '/quality' },
  { key: 'publish', name: 'Publish', icon: Share24Regular, path: '/publish' },
  { key: 'jobs', name: 'Recent Jobs', icon: TaskListSquareLtr24Regular, path: '/jobs' },
  { key: 'downloads', name: 'Downloads', icon: CloudArrowDown24Regular, path: '/downloads' },
  { key: 'health', name: 'Provider Health', icon: HeartPulse24Regular, path: '/health' },
  { key: 'logs', name: 'Logs', icon: DocumentBulletList24Regular, path: '/logs' },
  { key: 'settings', name: 'Settings', icon: Settings24Regular, path: '/settings' },
];
