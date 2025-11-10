import React from 'react';
import {
  Card,
  Grid,
  Title,
  Text,
  Metric,
  Flex,
  ProgressBar,
  AreaChart,
  BarChart,
} from '@tremor/react';
import { SystemMetrics } from '../../api/adminClient';

interface SystemMetricsPanelProps {
  metrics: SystemMetrics | null;
}

export const SystemMetricsPanel: React.FC<SystemMetricsPanelProps> = ({ metrics }) => {
  if (!metrics) {
    return (
      <div className="text-center py-8">
        <Text>Loading metrics...</Text>
      </div>
    );
  }

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  };

  return (
    <div className="space-y-6">
      <Title>System Metrics</Title>

      {/* Resource Utilization */}
      <Card>
        <Title>Resource Utilization</Title>
        <Grid numItems={1} numItemsSm={2} numItemsLg={3} className="gap-6 mt-4">
          <div>
            <Text>CPU Usage</Text>
            <Metric>{metrics.resources.cpuUsagePercent.toFixed(1)}%</Metric>
            <ProgressBar
              value={metrics.resources.cpuUsagePercent}
              color={metrics.resources.cpuUsagePercent > 80 ? 'red' : 'blue'}
              className="mt-2"
            />
          </div>
          <div>
            <Text>Memory Usage</Text>
            <Metric>{metrics.resources.memoryUsagePercent.toFixed(1)}%</Metric>
            <Text className="text-xs text-gray-500">
              {formatBytes(metrics.resources.memoryUsedBytes)} / {formatBytes(metrics.resources.memoryTotalBytes)}
            </Text>
            <ProgressBar
              value={metrics.resources.memoryUsagePercent}
              color={metrics.resources.memoryUsagePercent > 80 ? 'red' : 'green'}
              className="mt-2"
            />
          </div>
          {metrics.resources.gpuUsagePercent !== null && metrics.resources.gpuUsagePercent !== undefined && (
            <div>
              <Text>GPU Usage</Text>
              <Metric>{metrics.resources.gpuUsagePercent.toFixed(1)}%</Metric>
              {metrics.resources.gpuMemoryUsedBytes && metrics.resources.gpuMemoryTotalBytes && (
                <Text className="text-xs text-gray-500">
                  {formatBytes(metrics.resources.gpuMemoryUsedBytes)} / {formatBytes(metrics.resources.gpuMemoryTotalBytes)}
                </Text>
              )}
              <ProgressBar
                value={metrics.resources.gpuUsagePercent}
                color={metrics.resources.gpuUsagePercent > 80 ? 'red' : 'amber'}
                className="mt-2"
              />
            </div>
          )}
        </Grid>
      </Card>

      {/* Disk Usage */}
      <Card>
        <Title>Disk Usage</Title>
        <div className="mt-4 space-y-4">
          {metrics.resources.disks.map((disk) => (
            <div key={disk.driveName}>
              <Flex>
                <Text>{disk.driveName}</Text>
                <Text>{disk.usagePercent.toFixed(1)}%</Text>
              </Flex>
              <Text className="text-xs text-gray-500 mt-1">
                {formatBytes(disk.usedBytes)} / {formatBytes(disk.totalBytes)} ({formatBytes(disk.availableBytes)} available)
              </Text>
              <ProgressBar
                value={disk.usagePercent}
                color={disk.usagePercent > 80 ? 'red' : 'blue'}
                className="mt-2"
              />
            </div>
          ))}
        </div>
      </Card>

      {/* Application Metrics */}
      <Grid numItems={1} numItemsLg={2} className="gap-6">
        <Card>
          <Title>User Statistics</Title>
          <div className="mt-4 space-y-4">
            <Flex>
              <Text>Total Users</Text>
              <Metric className="text-2xl">{metrics.application.totalUsers}</Metric>
            </Flex>
            <Flex>
              <Text>Active Users</Text>
              <Metric className="text-2xl">{metrics.application.activeUsers}</Metric>
            </Flex>
            <ProgressBar
              value={(metrics.application.activeUsers / Math.max(metrics.application.totalUsers, 1)) * 100}
              color="blue"
              className="mt-2"
            />
          </div>
        </Card>

        <Card>
          <Title>Project Statistics</Title>
          <div className="mt-4 space-y-4">
            <Flex>
              <Text>Total Projects</Text>
              <Metric className="text-2xl">{metrics.application.totalProjects}</Metric>
            </Flex>
            <Flex>
              <Text>Active Projects</Text>
              <Metric className="text-2xl">{metrics.application.activeProjects}</Metric>
            </Flex>
            <ProgressBar
              value={(metrics.application.activeProjects / Math.max(metrics.application.totalProjects, 1)) * 100}
              color="green"
              className="mt-2"
            />
          </div>
        </Card>
      </Grid>

      {/* Job Queue Stats */}
      <Card>
        <Title>Job Queue Status</Title>
        <Grid numItems={1} numItemsSm={3} className="gap-6 mt-4">
          <div>
            <Text>In Progress</Text>
            <Metric>{metrics.application.jobsInProgress}</Metric>
          </div>
          <div>
            <Text>Queued</Text>
            <Metric>{metrics.application.jobsQueued}</Metric>
          </div>
          <div>
            <Text>Failed</Text>
            <Metric>{metrics.application.jobsFailed}</Metric>
          </div>
        </Grid>
      </Card>

      {/* Cache Performance */}
      <Card>
        <Title>Cache Performance</Title>
        <Grid numItems={1} numItemsSm={3} className="gap-6 mt-4">
          <div>
            <Text>Hit Rate</Text>
            <Metric>{(metrics.application.cacheHitRate * 100).toFixed(1)}%</Metric>
            <ProgressBar value={metrics.application.cacheHitRate * 100} color="blue" className="mt-2" />
          </div>
          <div>
            <Text>Hits</Text>
            <Metric>{metrics.application.cacheHits.toLocaleString()}</Metric>
          </div>
          <div>
            <Text>Misses</Text>
            <Metric>{metrics.application.cacheMisses.toLocaleString()}</Metric>
          </div>
        </Grid>
      </Card>
    </div>
  );
};
