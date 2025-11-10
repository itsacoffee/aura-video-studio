import React, { useState, useEffect } from 'react';
import {
  Table,
  TableHead,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Badge,
  Button,
  TextInput,
  Select,
  SelectItem,
  Text,
  Title,
  Flex,
  Card,
  Grid,
} from '@tremor/react';
import { Search24Regular, Filter24Regular } from '@fluentui/react-icons';
import { adminApiClient, AuditLog, AuditLogQueryRequest } from '../../api/adminClient';

interface AuditLogPanelProps {
  logs: AuditLog[];
}

export const AuditLogPanel: React.FC<AuditLogPanelProps> = ({ logs: initialLogs }) => {
  const [logs, setLogs] = useState<AuditLog[]>(initialLogs);
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<AuditLogQueryRequest>({
    page: 1,
    pageSize: 50,
  });
  const [totalCount, setTotalCount] = useState(0);

  useEffect(() => {
    loadLogs();
  }, [filters]);

  const loadLogs = async () => {
    try {
      setLoading(true);
      const response = await adminApiClient.getAuditLogs(filters);
      setLogs(response.logs);
      setTotalCount(response.totalCount);
    } catch (err) {
      console.error('Error loading audit logs:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleFilterChange = (key: keyof AuditLogQueryRequest, value: any) => {
    setFilters({ ...filters, [key]: value, page: 1 });
  };

  const handlePageChange = (newPage: number) => {
    setFilters({ ...filters, page: newPage });
  };

  const getSeverityColor = (severity?: string) => {
    switch (severity?.toLowerCase()) {
      case 'error':
      case 'critical':
        return 'red';
      case 'warning':
        return 'amber';
      case 'information':
      case 'info':
        return 'blue';
      default:
        return 'gray';
    }
  };

  return (
    <div className="space-y-4">
      <Flex justifyContent="between">
        <Title>Audit Logs</Title>
        <Text className="text-gray-500">
          Showing {logs.length} of {totalCount} logs
        </Text>
      </Flex>

      {/* Filters */}
      <Card>
        <Grid numItems={1} numItemsSm={2} numItemsLg={4} className="gap-4">
          <div>
            <Text>User ID</Text>
            <TextInput
              icon={Search24Regular}
              placeholder="Filter by user ID"
              value={filters.userId || ''}
              onChange={(e) => handleFilterChange('userId', e.target.value)}
            />
          </div>
          <div>
            <Text>Action</Text>
            <TextInput
              icon={Search24Regular}
              placeholder="Filter by action"
              value={filters.action || ''}
              onChange={(e) => handleFilterChange('action', e.target.value)}
            />
          </div>
          <div>
            <Text>Resource Type</Text>
            <TextInput
              placeholder="Filter by resource"
              value={filters.resourceType || ''}
              onChange={(e) => handleFilterChange('resourceType', e.target.value)}
            />
          </div>
          <div>
            <Text>Success Only</Text>
            <Select
              value={filters.successOnly?.toString() || 'all'}
              onValueChange={(value) => 
                handleFilterChange('successOnly', value === 'all' ? undefined : value === 'true')
              }
            >
              <SelectItem value="all">All</SelectItem>
              <SelectItem value="true">Success Only</SelectItem>
              <SelectItem value="false">Failures Only</SelectItem>
            </Select>
          </div>
        </Grid>
        <div className="flex gap-2 mt-4">
          <Button
            size="xs"
            variant="secondary"
            icon={Filter24Regular}
            onClick={() => setFilters({ page: 1, pageSize: 50 })}
          >
            Clear Filters
          </Button>
          <Button size="xs" onClick={loadLogs}>
            Apply Filters
          </Button>
        </div>
      </Card>

      {/* Audit Log Table */}
      <Table>
        <TableHead>
          <TableRow>
            <TableHeaderCell>Timestamp</TableHeaderCell>
            <TableHeaderCell>User</TableHeaderCell>
            <TableHeaderCell>Action</TableHeaderCell>
            <TableHeaderCell>Resource</TableHeaderCell>
            <TableHeaderCell>Status</TableHeaderCell>
            <TableHeaderCell>IP Address</TableHeaderCell>
            <TableHeaderCell>Severity</TableHeaderCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {logs.map((log) => (
            <TableRow key={log.id}>
              <TableCell>
                <Text className="text-xs">
                  {new Date(log.timestamp).toLocaleString()}
                </Text>
              </TableCell>
              <TableCell>
                <Text className="font-medium">{log.username || 'System'}</Text>
                {log.userId && (
                  <Text className="text-xs text-gray-500">{log.userId}</Text>
                )}
              </TableCell>
              <TableCell>
                <Text>{log.action}</Text>
              </TableCell>
              <TableCell>
                {log.resourceType && (
                  <>
                    <Text className="font-medium">{log.resourceType}</Text>
                    {log.resourceId && (
                      <Text className="text-xs text-gray-500">{log.resourceId}</Text>
                    )}
                  </>
                )}
              </TableCell>
              <TableCell>
                <Badge color={log.success ? 'green' : 'red'} size="xs">
                  {log.success ? 'Success' : 'Failed'}
                </Badge>
                {log.errorMessage && (
                  <Text className="text-xs text-red-600 mt-1">{log.errorMessage}</Text>
                )}
              </TableCell>
              <TableCell>
                <Text className="text-xs">{log.ipAddress || 'N/A'}</Text>
              </TableCell>
              <TableCell>
                <Badge color={getSeverityColor(log.severity)} size="xs">
                  {log.severity || 'Info'}
                </Badge>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      {/* Pagination */}
      <Flex justifyContent="between">
        <Button
          size="xs"
          variant="secondary"
          disabled={filters.page === 1}
          onClick={() => handlePageChange((filters.page || 1) - 1)}
        >
          Previous
        </Button>
        <Text>Page {filters.page}</Text>
        <Button
          size="xs"
          variant="secondary"
          disabled={logs.length < (filters.pageSize || 50)}
          onClick={() => handlePageChange((filters.page || 1) + 1)}
        >
          Next
        </Button>
      </Flex>
    </div>
  );
};
