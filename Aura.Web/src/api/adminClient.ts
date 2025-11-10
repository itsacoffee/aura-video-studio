import { typedFetch } from './typedClient';

// Type definitions
export interface User {
  id: string;
  username: string;
  email: string;
  displayName?: string;
  isActive: boolean;
  isSuspended: boolean;
  suspendedAt?: string;
  suspendedReason?: string;
  lastLoginAt?: string;
  lastLoginIp?: string;
  failedLoginAttempts: number;
  lockoutEnd?: string;
  emailVerified: boolean;
  phoneNumber?: string;
  phoneVerified: boolean;
  twoFactorEnabled: boolean;
  createdAt: string;
  updatedAt: string;
  roles: string[];
  quota?: UserQuotaSummary;
}

export interface UserQuotaSummary {
  apiRequestsPerDay?: number;
  apiRequestsUsedToday: number;
  videosPerMonth?: number;
  videosGeneratedThisMonth: number;
  storageLimitBytes?: number;
  storageUsedBytes: number;
  aiTokensPerMonth?: number;
  aiTokensUsedThisMonth: number;
  totalCostUsd: number;
  costLimitUsd?: number;
}

export interface UserQuota {
  apiRequestsPerDay?: number;
  videosPerMonth?: number;
  storageLimitBytes?: number;
  aiTokensPerMonth?: number;
  maxConcurrentRenders?: number;
  maxConcurrentJobs?: number;
  costLimitUsd?: number;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  displayName?: string;
  password?: string;
  roleIds?: string[];
  quota?: UserQuota;
}

export interface UpdateUserRequest {
  displayName?: string;
  email?: string;
  phoneNumber?: string;
  isActive?: boolean;
  twoFactorEnabled?: boolean;
}

export interface SuspendUserRequest {
  reason: string;
  untilDate?: string;
}

export interface UserListResponse {
  users: User[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface Role {
  id: string;
  name: string;
  description?: string;
  isSystemRole: boolean;
  permissions: string[];
  createdAt: string;
  updatedAt: string;
  userCount: number;
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
  permissions: string[];
}

export interface UpdateRoleRequest {
  name?: string;
  description?: string;
  permissions?: string[];
}

export interface AuditLog {
  id: string;
  timestamp: string;
  userId?: string;
  username?: string;
  action: string;
  resourceType?: string;
  resourceId?: string;
  ipAddress?: string;
  userAgent?: string;
  success: boolean;
  errorMessage?: string;
  changes?: Record<string, any>;
  severity?: string;
}

export interface AuditLogQueryRequest {
  userId?: string;
  action?: string;
  resourceType?: string;
  startDate?: string;
  endDate?: string;
  successOnly?: boolean;
  page?: number;
  pageSize?: number;
}

export interface AuditLogResponse {
  logs: AuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface SystemMetrics {
  timestamp: string;
  resources: SystemResources;
  application: ApplicationMetrics;
  providers: ProviderMetrics;
  costs: CostMetrics;
}

export interface SystemResources {
  cpuUsagePercent: number;
  memoryUsedBytes: number;
  memoryTotalBytes: number;
  memoryUsagePercent: number;
  disks: DiskUsage[];
  gpuUsagePercent?: number;
  gpuMemoryUsedBytes?: number;
  gpuMemoryTotalBytes?: number;
}

export interface DiskUsage {
  driveName: string;
  totalBytes: number;
  usedBytes: number;
  availableBytes: number;
  usagePercent: number;
}

export interface ApplicationMetrics {
  totalUsers: number;
  activeUsers: number;
  totalProjects: number;
  activeProjects: number;
  totalVideos: number;
  videosToday: number;
  jobsInProgress: number;
  jobsQueued: number;
  jobsFailed: number;
  averageRenderTimeSeconds: number;
  cacheHits: number;
  cacheMisses: number;
  cacheHitRate: number;
}

export interface ProviderMetrics {
  providers: ProviderStatus[];
  totalRequests: number;
  failedRequests: number;
  errorRate: number;
  averageLatencyMs: number;
}

export interface ProviderStatus {
  name: string;
  status: string;
  requestCount: number;
  errorCount: number;
  averageLatencyMs: number;
  lastUsed: string;
}

export interface CostMetrics {
  totalCostToday: number;
  totalCostThisMonth: number;
  totalCostAllTime: number;
  costByProvider: Record<string, number>;
  costByUser: Record<string, number>;
  topCostItems: CostBreakdown[];
}

export interface CostBreakdown {
  category: string;
  item: string;
  cost: number;
  usageCount: number;
}

export interface ConfigurationItem {
  key: string;
  value: string;
  category?: string;
  description?: string;
  isSensitive: boolean;
  isActive: boolean;
  updatedAt: string;
}

export interface UpdateConfigurationRequest {
  key: string;
  value: string;
  category?: string;
  description?: string;
  isSensitive?: boolean;
  isActive?: boolean;
}

export interface ConfigurationCategory {
  category: string;
  items: ConfigurationItem[];
}

export interface UserActivity {
  userId: string;
  username: string;
  timestamp: string;
  action: string;
  resourceType?: string;
  resourceId?: string;
  success: boolean;
}

// Admin API Client
export class AdminApiClient {
  private baseUrl = '/api/admin';

  // User Management
  async getUsers(params?: {
    page?: number;
    pageSize?: number;
    isActive?: boolean;
    isSuspended?: boolean;
    search?: string;
  }): Promise<UserListResponse> {
    const queryParams = new URLSearchParams();
    if (params?.page) queryParams.set('page', params.page.toString());
    if (params?.pageSize) queryParams.set('pageSize', params.pageSize.toString());
    if (params?.isActive !== undefined) queryParams.set('isActive', params.isActive.toString());
    if (params?.isSuspended !== undefined) queryParams.set('isSuspended', params.isSuspended.toString());
    if (params?.search) queryParams.set('search', params.search);

    const url = `${this.baseUrl}/users?${queryParams}`;
    return typedFetch<UserListResponse>(url);
  }

  async getUser(userId: string): Promise<User> {
    return typedFetch<User>(`${this.baseUrl}/users/${userId}`);
  }

  async createUser(request: CreateUserRequest): Promise<User> {
    return typedFetch<User>(`${this.baseUrl}/users`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
  }

  async updateUser(userId: string, request: UpdateUserRequest): Promise<User> {
    return typedFetch<User>(`${this.baseUrl}/users/${userId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
  }

  async suspendUser(userId: string, request: SuspendUserRequest): Promise<User> {
    return typedFetch<User>(`${this.baseUrl}/users/${userId}/suspend`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
  }

  async unsuspendUser(userId: string): Promise<User> {
    return typedFetch<User>(`${this.baseUrl}/users/${userId}/unsuspend`, {
      method: 'POST',
    });
  }

  async deleteUser(userId: string): Promise<void> {
    return typedFetch<void>(`${this.baseUrl}/users/${userId}`, {
      method: 'DELETE',
    });
  }

  async assignRoles(userId: string, roleIds: string[]): Promise<User> {
    return typedFetch<User>(`${this.baseUrl}/users/${userId}/roles`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(roleIds),
    });
  }

  async updateUserQuota(userId: string, quota: UserQuota): Promise<UserQuotaSummary> {
    return typedFetch<UserQuotaSummary>(`${this.baseUrl}/users/${userId}/quota`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(quota),
    });
  }

  async getUserActivity(userId: string, limit = 100): Promise<UserActivity[]> {
    return typedFetch<UserActivity[]>(`${this.baseUrl}/users/${userId}/activity?limit=${limit}`);
  }

  // Role Management
  async getRoles(): Promise<Role[]> {
    return typedFetch<Role[]>(`${this.baseUrl}/roles`);
  }

  async createRole(request: CreateRoleRequest): Promise<Role> {
    return typedFetch<Role>(`${this.baseUrl}/roles`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
  }

  async updateRole(roleId: string, request: UpdateRoleRequest): Promise<Role> {
    return typedFetch<Role>(`${this.baseUrl}/roles/${roleId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
  }

  async deleteRole(roleId: string): Promise<void> {
    return typedFetch<void>(`${this.baseUrl}/roles/${roleId}`, {
      method: 'DELETE',
    });
  }

  // Audit Logs
  async getAuditLogs(params?: AuditLogQueryRequest): Promise<AuditLogResponse> {
    const queryParams = new URLSearchParams();
    if (params?.userId) queryParams.set('userId', params.userId);
    if (params?.action) queryParams.set('action', params.action);
    if (params?.resourceType) queryParams.set('resourceType', params.resourceType);
    if (params?.startDate) queryParams.set('startDate', params.startDate);
    if (params?.endDate) queryParams.set('endDate', params.endDate);
    if (params?.successOnly !== undefined) queryParams.set('successOnly', params.successOnly.toString());
    if (params?.page) queryParams.set('page', params.page.toString());
    if (params?.pageSize) queryParams.set('pageSize', params.pageSize.toString());

    const url = `${this.baseUrl}/audit-logs?${queryParams}`;
    return typedFetch<AuditLogResponse>(url);
  }

  // System Metrics
  async getSystemMetrics(): Promise<SystemMetrics> {
    return typedFetch<SystemMetrics>(`${this.baseUrl}/metrics`);
  }

  // Configuration Management
  async getConfiguration(category?: string): Promise<ConfigurationItem[]> {
    const url = category
      ? `${this.baseUrl}/configuration?category=${category}`
      : `${this.baseUrl}/configuration`;
    return typedFetch<ConfigurationItem[]>(url);
  }

  async getConfigurationByCategory(): Promise<ConfigurationCategory[]> {
    return typedFetch<ConfigurationCategory[]>(`${this.baseUrl}/configuration/categories`);
  }

  async updateConfiguration(key: string, request: UpdateConfigurationRequest): Promise<ConfigurationItem> {
    return typedFetch<ConfigurationItem>(`${this.baseUrl}/configuration/${key}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
  }

  async deleteConfiguration(key: string): Promise<void> {
    return typedFetch<void>(`${this.baseUrl}/configuration/${key}`, {
      method: 'DELETE',
    });
  }
}

// Export a singleton instance
export const adminApiClient = new AdminApiClient();
