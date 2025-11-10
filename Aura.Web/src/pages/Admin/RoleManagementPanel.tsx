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
  Text,
  Title,
  Flex,
  MultiSelect,
  MultiSelectItem,
} from '@tremor/react';
import { Edit24Regular, Delete24Regular, Add24Regular } from '@fluentui/react-icons';
import { adminApiClient, Role, CreateRoleRequest, UpdateRoleRequest } from '../../api/adminClient';

interface RoleManagementPanelProps {
  roles: Role[];
  onUpdate: () => void;
}

const AVAILABLE_PERMISSIONS = [
  'admin.full_access',
  'users.manage',
  'users.view',
  'config.write',
  'config.read',
  'audit.view',
  'projects.manage',
  'projects.view',
  'videos.create',
  'videos.view',
  'assets.manage',
  'assets.view',
];

export const RoleManagementPanel: React.FC<RoleManagementPanelProps> = ({ roles, onUpdate }) => {
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [showEditDialog, setShowEditDialog] = useState(false);
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [createForm, setCreateForm] = useState<CreateRoleRequest>({
    name: '',
    description: '',
    permissions: [],
  });

  const [editForm, setEditForm] = useState<UpdateRoleRequest>({});

  const handleCreateRole = async () => {
    try {
      setLoading(true);
      setError(null);
      await adminApiClient.createRole(createForm);
      setShowCreateDialog(false);
      setCreateForm({ name: '', description: '', permissions: [] });
      onUpdate();
    } catch (err) {
      setError('Failed to create role');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleEditRole = async () => {
    if (!selectedRole) return;
    try {
      setLoading(true);
      setError(null);
      await adminApiClient.updateRole(selectedRole.id, editForm);
      setShowEditDialog(false);
      setSelectedRole(null);
      onUpdate();
    } catch (err) {
      setError('Failed to update role');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteRole = async (roleId: string) => {
    if (!confirm('Are you sure you want to delete this role?')) return;
    try {
      setLoading(true);
      setError(null);
      await adminApiClient.deleteRole(roleId);
      onUpdate();
    } catch (err) {
      setError('Failed to delete role');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const openEditDialog = (role: Role) => {
    setSelectedRole(role);
    setEditForm({
      name: role.name,
      description: role.description,
      permissions: role.permissions,
    });
    setShowEditDialog(true);
  };

  return (
    <div className="space-y-4">
      <Flex justifyContent="between">
        <Title>Role Management</Title>
        <Button icon={Add24Regular} onClick={() => setShowCreateDialog(true)}>
          Create Role
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
            <TableHeaderCell>Role Name</TableHeaderCell>
            <TableHeaderCell>Description</TableHeaderCell>
            <TableHeaderCell>Users</TableHeaderCell>
            <TableHeaderCell>Permissions</TableHeaderCell>
            <TableHeaderCell>Type</TableHeaderCell>
            <TableHeaderCell>Actions</TableHeaderCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {roles.map((role) => (
            <TableRow key={role.id}>
              <TableCell>
                <Text className="font-medium">{role.name}</Text>
              </TableCell>
              <TableCell>{role.description}</TableCell>
              <TableCell>{role.userCount} users</TableCell>
              <TableCell>
                <div className="flex gap-1 flex-wrap">
                  {role.permissions.slice(0, 3).map((perm) => (
                    <Badge key={perm} size="xs" color="violet">
                      {perm}
                    </Badge>
                  ))}
                  {role.permissions.length > 3 && (
                    <Badge size="xs" color="gray">
                      +{role.permissions.length - 3}
                    </Badge>
                  )}
                </div>
              </TableCell>
              <TableCell>
                {role.isSystemRole ? (
                  <Badge color="blue" size="xs">
                    System
                  </Badge>
                ) : (
                  <Badge color="green" size="xs">
                    Custom
                  </Badge>
                )}
              </TableCell>
              <TableCell>
                <div className="flex gap-2">
                  {!role.isSystemRole && (
                    <>
                      <Button
                        size="xs"
                        variant="secondary"
                        icon={Edit24Regular}
                        onClick={() => openEditDialog(role)}
                      >
                        Edit
                      </Button>
                      <Button
                        size="xs"
                        variant="secondary"
                        color="red"
                        icon={Delete24Regular}
                        onClick={() => handleDeleteRole(role.id)}
                      >
                        Delete
                      </Button>
                    </>
                  )}
                  {role.isSystemRole && (
                    <Text className="text-gray-500 text-xs">Protected</Text>
                  )}
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      {/* Create Role Dialog */}
      <Dialog open={showCreateDialog} onClose={() => setShowCreateDialog(false)}>
        <DialogPanel>
          <Title className="mb-4">Create New Role</Title>
          <div className="space-y-4">
            <div>
              <Text>Role Name</Text>
              <TextInput
                value={createForm.name}
                onChange={(e) => setCreateForm({ ...createForm, name: e.target.value })}
                placeholder="Enter role name"
              />
            </div>
            <div>
              <Text>Description</Text>
              <TextInput
                value={createForm.description}
                onChange={(e) => setCreateForm({ ...createForm, description: e.target.value })}
                placeholder="Enter description"
              />
            </div>
            <div>
              <Text>Permissions</Text>
              <MultiSelect
                value={createForm.permissions}
                onValueChange={(value) => setCreateForm({ ...createForm, permissions: value })}
              >
                {AVAILABLE_PERMISSIONS.map((perm) => (
                  <MultiSelectItem key={perm} value={perm}>
                    {perm}
                  </MultiSelectItem>
                ))}
              </MultiSelect>
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="secondary" onClick={() => setShowCreateDialog(false)}>
                Cancel
              </Button>
              <Button onClick={handleCreateRole} disabled={loading || !createForm.name}>
                {loading ? 'Creating...' : 'Create Role'}
              </Button>
            </div>
          </div>
        </DialogPanel>
      </Dialog>

      {/* Edit Role Dialog */}
      <Dialog open={showEditDialog} onClose={() => setShowEditDialog(false)}>
        <DialogPanel>
          <Title className="mb-4">Edit Role</Title>
          <div className="space-y-4">
            <div>
              <Text>Role Name</Text>
              <TextInput
                value={editForm.name || ''}
                onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
              />
            </div>
            <div>
              <Text>Description</Text>
              <TextInput
                value={editForm.description || ''}
                onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
              />
            </div>
            <div>
              <Text>Permissions</Text>
              <MultiSelect
                value={editForm.permissions || []}
                onValueChange={(value) => setEditForm({ ...editForm, permissions: value })}
              >
                {AVAILABLE_PERMISSIONS.map((perm) => (
                  <MultiSelectItem key={perm} value={perm}>
                    {perm}
                  </MultiSelectItem>
                ))}
              </MultiSelect>
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="secondary" onClick={() => setShowEditDialog(false)}>
                Cancel
              </Button>
              <Button onClick={handleEditRole} disabled={loading}>
                {loading ? 'Saving...' : 'Save Changes'}
              </Button>
            </div>
          </div>
        </DialogPanel>
      </Dialog>
    </div>
  );
};
