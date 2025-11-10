import React, { useState, useEffect } from 'react';
import {
  Card,
  Text,
  Title,
  Subtitle,
  TabList,
  Tab,
  TabPanel,
  TabGroup,
  Grid,
  Flex,
  Badge,
  Button,
  Metric,
  ProgressBar,
} from '@tremor/react';
import {
  Person24Regular,
  ShieldTask24Regular,
  ChartMultiple24Regular,
  Settings24Regular,
  DocumentBulletList24Regular,
  People24Regular,
  Database24Regular,
  Alert24Regular,
} from '@fluentui/react-icons';
import { adminApiClient, SystemMetrics, User, Role, AuditLog } from '../../api/adminClient';
import { UserManagementPanel } from './UserManagementPanel';
import { RoleManagementPanel } from './RoleManagementPanel';
import { SystemMetricsPanel } from './SystemMetricsPanel';
import { AuditLogPanel } from './AuditLogPanel';
import { ConfigurationPanel } from './ConfigurationPanel';

export const AdminDashboardPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [metrics, setMetrics] = useState<SystemMetrics | null>(null);
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [recentLogs, setRecentLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadDashboardData();
    const interval = setInterval(loadDashboardData, 30000); // Refresh every 30s
    return () => clearInterval(interval);
  }, []);

  const loadDashboardData = async () => {
    try {
      setLoading(true);
      const [metricsData, usersData, rolesData, logsData] = await Promise.all([
        adminApiClient.getSystemMetrics(),
        adminApiClient.getUsers({ page: 1, pageSize: 10 }),
        adminApiClient.getRoles(),
        adminApiClient.getAuditLogs({ page: 1, pageSize: 10 }),
      ]);
      
      setMetrics(metricsData);
      setUsers(usersData.users);
      setRoles(rolesData);
      setRecentLogs(logsData.logs);
      setError(null);
    } catch (err) {
      console.error('Error loading admin dashboard data:', err);
      setError('Failed to load dashboard data');
    } finally {
      setLoading(false);
    }
  };

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  };

  const formatPercent = (value: number): string => {
    return `${Math.round(value * 10) / 10}%`;
  };

  if (loading && !metrics) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto mb-4"></div>
          <Text>Loading admin dashboard...</Text>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <Title>Admin Dashboard</Title>
          <Subtitle>System administration and monitoring</Subtitle>
        </div>
        <div className="flex gap-2">
          <Button
            variant="secondary"
            icon={Alert24Regular}
            onClick={loadDashboardData}
          >
            Refresh
          </Button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <Text className="text-red-800">{error}</Text>
        </div>
      )}

      {/* Overview Metrics */}
      {metrics && (
        <Grid numItems={1} numItemsSm={2} numItemsLg={4} className="gap-6">
          <Card decoration="top" decorationColor="blue">
            <Flex alignItems="start">
              <div>
                <Text>Total Users</Text>
                <Metric>{metrics.application.totalUsers}</Metric>
              </div>
              <People24Regular className="text-blue-500" />
            </Flex>
            <Flex className="mt-4">
              <Text className="truncate">Active: {metrics.application.activeUsers}</Text>
              <Text>{metrics.application.activeUsers} users</Text>
            </Flex>
            <ProgressBar
              value={(metrics.application.activeUsers / metrics.application.totalUsers) * 100}
              color="blue"
              className="mt-2"
            />
          </Card>

          <Card decoration="top" decorationColor="green">
            <Flex alignItems="start">
              <div>
                <Text>CPU Usage</Text>
                <Metric>{formatPercent(metrics.resources.cpuUsagePercent)}</Metric>
              </div>
              <ChartMultiple24Regular className="text-green-500" />
            </Flex>
            <ProgressBar
              value={metrics.resources.cpuUsagePercent}
              color={metrics.resources.cpuUsagePercent > 80 ? 'red' : 'green'}
              className="mt-4"
            />
          </Card>

          <Card decoration="top" decorationColor="amber">
            <Flex alignItems="start">
              <div>
                <Text>Memory Usage</Text>
                <Metric>{formatPercent(metrics.resources.memoryUsagePercent)}</Metric>
              </div>
              <Database24Regular className="text-amber-500" />
            </Flex>
            <Flex className="mt-4">
              <Text className="truncate">
                {formatBytes(metrics.resources.memoryUsedBytes)} / {formatBytes(metrics.resources.memoryTotalBytes)}
              </Text>
            </Flex>
            <ProgressBar
              value={metrics.resources.memoryUsagePercent}
              color={metrics.resources.memoryUsagePercent > 80 ? 'red' : 'amber'}
              className="mt-2"
            />
          </Card>

          <Card decoration="top" decorationColor="violet">
            <Flex alignItems="start">
              <div>
                <Text>Active Projects</Text>
                <Metric>{metrics.application.activeProjects}</Metric>
              </div>
              <ShieldTask24Regular className="text-violet-500" />
            </Flex>
            <Flex className="mt-4">
              <Text className="truncate">Total: {metrics.application.totalProjects}</Text>
              <Text>{metrics.application.totalProjects} projects</Text>
            </Flex>
            <ProgressBar
              value={(metrics.application.activeProjects / Math.max(metrics.application.totalProjects, 1)) * 100}
              color="violet"
              className="mt-2"
            />
          </Card>
        </Grid>
      )}

      {/* Main Content Tabs */}
      <Card>
        <TabGroup onIndexChange={setActiveTab}>
          <TabList>
            <Tab icon={Person24Regular}>User Management</Tab>
            <Tab icon={ShieldTask24Regular}>Roles & Permissions</Tab>
            <Tab icon={ChartMultiple24Regular}>System Metrics</Tab>
            <Tab icon={Settings24Regular}>Configuration</Tab>
            <Tab icon={DocumentBulletList24Regular}>Audit Logs</Tab>
          </TabList>

          <TabPanel>
            <UserManagementPanel 
              users={users} 
              roles={roles}
              onUpdate={loadDashboardData} 
            />
          </TabPanel>

          <TabPanel>
            <RoleManagementPanel 
              roles={roles} 
              onUpdate={loadDashboardData} 
            />
          </TabPanel>

          <TabPanel>
            <SystemMetricsPanel metrics={metrics} />
          </TabPanel>

          <TabPanel>
            <ConfigurationPanel />
          </TabPanel>

          <TabPanel>
            <AuditLogPanel logs={recentLogs} />
          </TabPanel>
        </TabGroup>
      </Card>

      {/* Recent Activity Summary */}
      <Grid numItems={1} numItemsLg={2} className="gap-6">
        <Card>
          <Title>Recent Users</Title>
          <div className="mt-4 space-y-3">
            {users.slice(0, 5).map((user) => (
              <Flex key={user.id} justifyContent="start" className="space-x-2">
                <Person24Regular className="text-blue-500" />
                <div className="flex-1">
                  <Text className="font-medium">{user.username}</Text>
                  <Text className="text-xs text-gray-500">{user.email}</Text>
                </div>
                <div className="flex gap-2">
                  {user.isActive ? (
                    <Badge color="green" size="xs">Active</Badge>
                  ) : (
                    <Badge color="red" size="xs">Inactive</Badge>
                  )}
                  {user.isSuspended && (
                    <Badge color="amber" size="xs">Suspended</Badge>
                  )}
                </div>
              </Flex>
            ))}
          </div>
        </Card>

        <Card>
          <Title>Recent Audit Logs</Title>
          <div className="mt-4 space-y-3">
            {recentLogs.slice(0, 5).map((log) => (
              <Flex key={log.id} justifyContent="start" className="space-x-2">
                <DocumentBulletList24Regular 
                  className={log.success ? 'text-green-500' : 'text-red-500'} 
                />
                <div className="flex-1">
                  <Text className="font-medium">{log.action}</Text>
                  <Text className="text-xs text-gray-500">
                    {log.username || 'System'} • {new Date(log.timestamp).toLocaleString()}
                  </Text>
                </div>
                <Badge color={log.success ? 'green' : 'red'} size="xs">
                  {log.success ? 'Success' : 'Failed'}
                </Badge>
              </Flex>
            ))}
          </div>
        </Card>
      </Grid>
    </div>
  );
};
