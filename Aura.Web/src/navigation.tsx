import { 
  Home24Regular,
  VideoClip24Regular,
  Settings24Regular,
  Play24Regular,
  Document24Regular,
  CloudArrowDown24Regular,
  Share24Regular,
  Timeline24Regular
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
  { key: 'create', name: 'Create', icon: VideoClip24Regular, path: '/create' },
  { key: 'timeline', name: 'Timeline', icon: Timeline24Regular, path: '/timeline' },
  { key: 'render', name: 'Render', icon: Play24Regular, path: '/render' },
  { key: 'publish', name: 'Publish', icon: Share24Regular, path: '/publish' },
  { key: 'downloads', name: 'Downloads', icon: CloudArrowDown24Regular, path: '/downloads' },
  { key: 'settings', name: 'Settings', icon: Settings24Regular, path: '/settings' },
];
