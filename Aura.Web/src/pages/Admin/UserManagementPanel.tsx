import React, { useState } from 'react';
import {
  Table,
  TableHead,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Badge,
  Button,
  Dialog,
  DialogPanel,
  TextInput,
  Select,
  SelectItem,
  MultiSelect,
  MultiSelectItem,
  NumberInput,
  Text,
  Title,
  Flex,
} from '@tremor/react';
import {
  Edit24Regular,
  Delete24Regular,
  PersonAdd24Regular,
  PersonProhibited24Regular,
  PersonAvailable24Regular,
} from '@fluentui/react-icons';
import {
  adminApiClient,
  User,
  Role,
  CreateUserRequest,
  UpdateUserRequest,
  SuspendUserRequest,
  UserQuota,
} from '../../api/adminClient';

interface UserManagementPanelProps {
  users: User[];
  roles: Role[];
  onUpdate: () => void;
}

export const UserManagementPanel: React.FC<UserManagementPanelProps> = ({
  users,
  roles,
  onUpdate,
}) => {
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [showEditDialog, setShowEditDialog] = useState(false);
  const [showSuspendDialog, setShowSuspendDialog] = useState(false);
  const [showQuotaDialog, setShowQuotaDialog] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Create user form state
  const [createForm, setCreateForm] = useState<CreateUserRequest>({
    username: '',
    email: '',
    displayName: '',
    password: '',
    roleIds: [],
  });

  // Edit user form state
  const [editForm, setEditForm] = useState<UpdateUserRequest>({});

  // Suspend user form state
  const [suspendForm, setSuspendForm] = useState<SuspendUserRequest>({
    reason: '',
    untilDate: undefined,
  });

  // Quota form state
  const [quotaForm, setQuotaForm] = useState<UserQuota>({});

  const handleCreateUser = async () => {
    try {
      setLoading(true);
      setError(null);
      await adminApiClient.createUser(createForm);
      setShowCreateDialog(false);
      setCreateForm({
        username: '',
        email: '',
        displayName: '',
        password: '',
        roleIds: [],
      });
      onUpdate();
    } catch (err) {
      setError('Failed to create user');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleEditUser = async () => {
    if (!selectedUser) return;
    try {
      setLoading(true);
      setError(null);
      await adminApiClient.updateUser(selectedUser.id, editForm);
      setShowEditDialog(false);
      setEditForm({});
      setSelectedUser(null);
      onUpdate();
    } catch (err) {
      setError('Failed to update user');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleSuspendUser = async () => {
    if (!selectedUser) return;
    try {
      setLoading(true);
      setError(null);
      await adminApiClient.suspendUser(selectedUser.id, suspendForm);
      setShowSuspendDialog(false);
      setSuspendForm({ reason: '', untilDate: undefined });
      setSelectedUser(null);
      onUpdate();
    } catch (err) {
      setError('Failed to suspend user');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleUnsuspendUser = async (userId: string) => {
    try {
      setLoading(true);
      setError(null);
      await adminApiClient.unsuspendUser(userId);
      onUpdate();
    } catch (err) {
      setError('Failed to unsuspend user');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteUser = async (userId: string) => {
    if (!confirm('Are you sure you want to delete this user?')) return;
    try {
      setLoading(true);
      setError(null);
      await adminApiClient.deleteUser(userId);
      onUpdate();
    } catch (err) {
      setError('Failed to delete user');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateQuota = async () => {
    if (!selectedUser) return;
    try {
      setLoading(true);
      setError(null);
      await adminApiClient.updateUserQuota(selectedUser.id, quotaForm);
      setShowQuotaDialog(false);
      setQuotaForm({});
      setSelectedUser(null);
      onUpdate();
    } catch (err) {
      setError('Failed to update quota');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const openEditDialog = (user: User) => {
    setSelectedUser(user);
    setEditForm({
      displayName: user.displayName,
      email: user.email,
      phoneNumber: user.phoneNumber,
      isActive: user.isActive,
      twoFactorEnabled: user.twoFactorEnabled,
    });
    setShowEditDialog(true);
  };

  const openSuspendDialog = (user: User) => {
    setSelectedUser(user);
    setShowSuspendDialog(true);
  };

  const openQuotaDialog = (user: User) => {
    setSelectedUser(user);
    setQuotaForm({
      apiRequestsPerDay: user.quota?.apiRequestsPerDay,
      videosPerMonth: user.quota?.videosPerMonth,
      storageLimitBytes: user.quota?.storageLimitBytes,
      aiTokensPerMonth: user.quota?.aiTokensPerMonth,
      maxConcurrentRenders: undefined,
      maxConcurrentJobs: undefined,
      costLimitUsd: user.quota?.costLimitUsd,
    });
    setShowQuotaDialog(true);
  };

  return (
    <div className="space-y-4">
      <Flex justifyContent="between">
        <Title>User Management</Title>
        <Button
          icon={PersonAdd24Regular}
          onClick={() => setShowCreateDialog(true)}
        >
          Create User
        </Button>
      </Flex>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-3">
          <Text className="text-red-800">{error}</Text>
        </div>
      )}

      <Table>
        <TableHead>
          <TableRow>
            <TableHeaderCell>Username</TableHeaderCell>
            <TableHeaderCell>Email</TableHeaderCell>
            <TableHeaderCell>Roles</TableHeaderCell>
            <TableHeaderCell>Status</TableHeaderCell>
            <TableHeaderCell>Created</TableHeaderCell>
            <TableHeaderCell>Actions</TableHeaderCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {users.map((user) => (
            <TableRow key={user.id}>
              <TableCell>
                <Text className="font-medium">{user.username}</Text>
                {user.displayName && (
                  <Text className="text-xs text-gray-500">{user.displayName}</Text>
                )}
              </TableCell>
              <TableCell>{user.email}</TableCell>
              <TableCell>
                <div className="flex gap-1 flex-wrap">
                  {user.roles.map((role) => (
                    <Badge key={role} size="xs" color="blue">
                      {role}
                    </Badge>
                  ))}
                </div>
              </TableCell>
              <TableCell>
                <div className="flex gap-1 flex-wrap">
                  {user.isActive ? (
                    <Badge color="green" size="xs">Active</Badge>
                  ) : (
                    <Badge color="gray" size="xs">Inactive</Badge>
                  )}
                  {user.isSuspended && (
                    <Badge color="red" size="xs">Suspended</Badge>
                  )}
                  {user.emailVerified && (
                    <Badge color="blue" size="xs">Verified</Badge>
                  )}
                </div>
              </TableCell>
              <TableCell>
                {new Date(user.createdAt).toLocaleDateString()}
              </TableCell>
              <TableCell>
                <div className="flex gap-2">
                  <Button
                    size="xs"
                    variant="secondary"
                    icon={Edit24Regular}
                    onClick={() => openEditDialog(user)}
                  >
                    Edit
                  </Button>
                  {user.isSuspended ? (
                    <Button
                      size="xs"
                      variant="secondary"
                      icon={PersonAvailable24Regular}
                      onClick={() => handleUnsuspendUser(user.id)}
                    >
                      Unsuspend
                    </Button>
                  ) : (
                    <Button
                      size="xs"
                      variant="secondary"
                      icon={PersonProhibited24Regular}
                      onClick={() => openSuspendDialog(user)}
                    >
                      Suspend
                    </Button>
                  )}
                  <Button
                    size="xs"
                    variant="secondary"
                    onClick={() => openQuotaDialog(user)}
                  >
                    Quota
                  </Button>
                  <Button
                    size="xs"
                    variant="secondary"
                    color="red"
                    icon={Delete24Regular}
                    onClick={() => handleDeleteUser(user.id)}
                  >
                    Delete
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      {/* Create User Dialog */}
      <Dialog open={showCreateDialog} onClose={() => setShowCreateDialog(false)}>
        <DialogPanel>
          <Title className="mb-4">Create New User</Title>
          <div className="space-y-4">
            <div>
              <Text>Username</Text>
              <TextInput
                value={createForm.username}
                onChange={(e) => setCreateForm({ ...createForm, username: e.target.value })}
                placeholder="Enter username"
              />
            </div>
            <div>
              <Text>Email</Text>
              <TextInput
                type="email"
                value={createForm.email}
                onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
                placeholder="Enter email"
              />
            </div>
            <div>
              <Text>Display Name</Text>
              <TextInput
                value={createForm.displayName}
                onChange={(e) => setCreateForm({ ...createForm, displayName: e.target.value })}
                placeholder="Enter display name"
              />
            </div>
            <div>
              <Text>Password</Text>
              <TextInput
                type="password"
                value={createForm.password}
                onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })}
                placeholder="Enter password"
              />
            </div>
            <div>
              <Text>Roles</Text>
              <MultiSelect
                value={createForm.roleIds || []}
                onValueChange={(value) => setCreateForm({ ...createForm, roleIds: value })}
              >
                {roles.map((role) => (
                  <MultiSelectItem key={role.id} value={role.id}>
                    {role.name}
                  </MultiSelectItem>
                ))}
              </MultiSelect>
            </div>
            <div className="flex gap-2 justify-end">
              <Button
                variant="secondary"
                onClick={() => setShowCreateDialog(false)}
              >
                Cancel
              </Button>
              <Button
                onClick={handleCreateUser}
                disabled={loading || !createForm.username || !createForm.email}
              >
                {loading ? 'Creating...' : 'Create User'}
              </Button>
            </div>
          </div>
        </DialogPanel>
      </Dialog>

      {/* Edit User Dialog */}
      <Dialog open={showEditDialog} onClose={() => setShowEditDialog(false)}>
        <DialogPanel>
          <Title className="mb-4">Edit User</Title>
          <div className="space-y-4">
            <div>
              <Text>Display Name</Text>
              <TextInput
                value={editForm.displayName || ''}
                onChange={(e) => setEditForm({ ...editForm, displayName: e.target.value })}
              />
            </div>
            <div>
              <Text>Email</Text>
              <TextInput
                type="email"
                value={editForm.email || ''}
                onChange={(e) => setEditForm({ ...editForm, email: e.target.value })}
              />
            </div>
            <div>
              <Text>Phone Number</Text>
              <TextInput
                value={editForm.phoneNumber || ''}
                onChange={(e) => setEditForm({ ...editForm, phoneNumber: e.target.value })}
              />
            </div>
            <div className="flex gap-2 justify-end">
              <Button
                variant="secondary"
                onClick={() => setShowEditDialog(false)}
              >
                Cancel
              </Button>
              <Button onClick={handleEditUser} disabled={loading}>
                {loading ? 'Saving...' : 'Save Changes'}
              </Button>
            </div>
          </div>
        </DialogPanel>
      </Dialog>

      {/* Suspend User Dialog */}
      <Dialog open={showSuspendDialog} onClose={() => setShowSuspendDialog(false)}>
        <DialogPanel>
          <Title className="mb-4">Suspend User</Title>
          <div className="space-y-4">
            <div>
              <Text>Reason</Text>
              <TextInput
                value={suspendForm.reason}
                onChange={(e) => setSuspendForm({ ...suspendForm, reason: e.target.value })}
                placeholder="Enter suspension reason"
              />
            </div>
            <div>
              <Text>Until Date (Optional)</Text>
              <TextInput
                type="datetime-local"
                value={suspendForm.untilDate || ''}
                onChange={(e) => setSuspendForm({ ...suspendForm, untilDate: e.target.value })}
              />
            </div>
            <div className="flex gap-2 justify-end">
              <Button
                variant="secondary"
                onClick={() => setShowSuspendDialog(false)}
              >
                Cancel
              </Button>
              <Button
                color="red"
                onClick={handleSuspendUser}
                disabled={loading || !suspendForm.reason}
              >
                {loading ? 'Suspending...' : 'Suspend User'}
              </Button>
            </div>
          </div>
        </DialogPanel>
      </Dialog>

      {/* Quota Management Dialog */}
      <Dialog open={showQuotaDialog} onClose={() => setShowQuotaDialog(false)}>
        <DialogPanel>
          <Title className="mb-4">Manage User Quota</Title>
          <div className="space-y-4">
            <div>
              <Text>API Requests Per Day</Text>
              <NumberInput
                value={quotaForm.apiRequestsPerDay}
                onValueChange={(value) => setQuotaForm({ ...quotaForm, apiRequestsPerDay: value })}
                placeholder="Unlimited"
              />
            </div>
            <div>
              <Text>Videos Per Month</Text>
              <NumberInput
                value={quotaForm.videosPerMonth}
                onValueChange={(value) => setQuotaForm({ ...quotaForm, videosPerMonth: value })}
                placeholder="Unlimited"
              />
            </div>
            <div>
              <Text>Storage Limit (Bytes)</Text>
              <NumberInput
                value={quotaForm.storageLimitBytes}
                onValueChange={(value) => setQuotaForm({ ...quotaForm, storageLimitBytes: value })}
                placeholder="Unlimited"
              />
            </div>
            <div>
              <Text>AI Tokens Per Month</Text>
              <NumberInput
                value={quotaForm.aiTokensPerMonth}
                onValueChange={(value) => setQuotaForm({ ...quotaForm, aiTokensPerMonth: value })}
                placeholder="Unlimited"
              />
            </div>
            <div>
              <Text>Cost Limit (USD)</Text>
              <NumberInput
                value={quotaForm.costLimitUsd}
                onValueChange={(value) => setQuotaForm({ ...quotaForm, costLimitUsd: value })}
                placeholder="Unlimited"
                enableStepper={false}
                step={0.01}
              />
            </div>
            <div className="flex gap-2 justify-end">
              <Button
                variant="secondary"
                onClick={() => setShowQuotaDialog(false)}
              >
                Cancel
              </Button>
              <Button onClick={handleUpdateQuota} disabled={loading}>
                {loading ? 'Saving...' : 'Update Quota'}
              </Button>
            </div>
          </div>
        </DialogPanel>
      </Dialog>
    </div>
  );
};
