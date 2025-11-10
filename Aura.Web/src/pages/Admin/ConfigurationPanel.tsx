import React, { useState, useEffect } from 'react';
import {
  Accordion,
  AccordionHeader,
  AccordionBody,
  Button,
  Dialog,
  DialogPanel,
  TextInput,
  Text,
  Title,
  Flex,
  Badge,
  Select,
  SelectItem,
} from '@tremor/react';
import {
  Edit24Regular,
  Delete24Regular,
  Add24Regular,
  Settings24Regular,
} from '@fluentui/react-icons';
import {
  adminApiClient,
  ConfigurationCategory,
  ConfigurationItem,
  UpdateConfigurationRequest,
} from '../../api/adminClient';

export const ConfigurationPanel: React.FC = () => {
  const [categories, setCategories] = useState<ConfigurationCategory[]>([]);
  const [loading, setLoading] = useState(false);
  const [showEditDialog, setShowEditDialog] = useState(false);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [selectedConfig, setSelectedConfig] = useState<ConfigurationItem | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState<UpdateConfigurationRequest>({
    key: '',
    value: '',
    category: '',
    description: '',
    isSensitive: false,
    isActive: true,
  });

  useEffect(() => {
    loadConfiguration();
  }, []);

  const loadConfiguration = async () => {
    try {
      setLoading(true);
      const data = await adminApiClient.getConfigurationByCategory();
      setCategories(data);
    } catch (err) {
      console.error('Error loading configuration:', err);
      setError('Failed to load configuration');
    } finally {
      setLoading(false);
    }
  };

  const handleSaveConfig = async () => {
    try {
      setLoading(true);
      setError(null);
      await adminApiClient.updateConfiguration(form.key, form);
      setShowEditDialog(false);
      setShowCreateDialog(false);
      setForm({
        key: '',
        value: '',
        category: '',
        description: '',
        isSensitive: false,
        isActive: true,
      });
      loadConfiguration();
    } catch (err) {
      setError('Failed to save configuration');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteConfig = async (key: string) => {
    if (!confirm(`Are you sure you want to delete configuration "${key}"?`)) return;
    try {
      setLoading(true);
      setError(null);
      await adminApiClient.deleteConfiguration(key);
      loadConfiguration();
    } catch (err) {
      setError('Failed to delete configuration');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const openEditDialog = (config: ConfigurationItem) => {
    setSelectedConfig(config);
    setForm({
      key: config.key,
      value: config.value,
      category: config.category,
      description: config.description,
      isSensitive: config.isSensitive,
      isActive: config.isActive,
    });
    setShowEditDialog(true);
  };

  const openCreateDialog = () => {
    setForm({
      key: '',
      value: '',
      category: '',
      description: '',
      isSensitive: false,
      isActive: true,
    });
    setShowCreateDialog(true);
  };

  return (
    <div className="space-y-4">
      <Flex justifyContent="between">
        <Title>Configuration Management</Title>
        <Button icon={Add24Regular} onClick={openCreateDialog}>
          Add Configuration
        </Button>
      </Flex>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-3">
          <Text className="text-red-800">{error}</Text>
        </div>
      )}

      {loading && categories.length === 0 ? (
        <div className="text-center py-8">
          <Text>Loading configuration...</Text>
        </div>
      ) : (
        <Accordion>
          {categories.map((category) => (
            <div key={category.category}>
              <AccordionHeader>
                <Flex>
                  <Text className="font-medium">{category.category}</Text>
                  <Badge size="xs">{category.items.length} items</Badge>
                </Flex>
              </AccordionHeader>
              <AccordionBody>
                <div className="space-y-3">
                  {category.items.map((item) => (
                    <div
                      key={item.key}
                      className="border rounded-lg p-4 hover:bg-gray-50 transition-colors"
                    >
                      <Flex justifyContent="between" alignItems="start">
                        <div className="flex-1">
                          <Flex className="gap-2">
                            <Settings24Regular className="text-gray-400" />
                            <div className="flex-1">
                              <Text className="font-medium">{item.key}</Text>
                              {item.description && (
                                <Text className="text-sm text-gray-600 mt-1">
                                  {item.description}
                                </Text>
                              )}
                              <div className="mt-2">
                                <Text className="text-sm text-gray-500">Value:</Text>
                                <Text className="font-mono text-sm mt-1">
                                  {item.isSensitive ? '***SENSITIVE***' : item.value}
                                </Text>
                              </div>
                              <div className="flex gap-2 mt-2">
                                {item.isSensitive && (
                                  <Badge color="red" size="xs">
                                    Sensitive
                                  </Badge>
                                )}
                                {!item.isActive && (
                                  <Badge color="gray" size="xs">
                                    Inactive
                                  </Badge>
                                )}
                                <Text className="text-xs text-gray-400">
                                  Updated: {new Date(item.updatedAt).toLocaleDateString()}
                                </Text>
                              </div>
                            </div>
                          </Flex>
                        </div>
                        <div className="flex gap-2">
                          <Button
                            size="xs"
                            variant="secondary"
                            icon={Edit24Regular}
                            onClick={() => openEditDialog(item)}
                          >
                            Edit
                          </Button>
                          <Button
                            size="xs"
                            variant="secondary"
                            color="red"
                            icon={Delete24Regular}
                            onClick={() => handleDeleteConfig(item.key)}
                          >
                            Delete
                          </Button>
                        </div>
                      </Flex>
                    </div>
                  ))}
                </div>
              </AccordionBody>
            </div>
          ))}
        </Accordion>
      )}

      {/* Edit/Create Configuration Dialog */}
      <Dialog
        open={showEditDialog || showCreateDialog}
        onClose={() => {
          setShowEditDialog(false);
          setShowCreateDialog(false);
        }}
      >
        <DialogPanel>
          <Title className="mb-4">
            {showCreateDialog ? 'Create Configuration' : 'Edit Configuration'}
          </Title>
          <div className="space-y-4">
            <div>
              <Text>Key</Text>
              <TextInput
                value={form.key}
                onChange={(e) => setForm({ ...form, key: e.target.value })}
                placeholder="config.key.name"
                disabled={showEditDialog}
              />
            </div>
            <div>
              <Text>Value</Text>
              <TextInput
                value={form.value}
                onChange={(e) => setForm({ ...form, value: e.target.value })}
                placeholder="Configuration value"
              />
            </div>
            <div>
              <Text>Category</Text>
              <TextInput
                value={form.category || ''}
                onChange={(e) => setForm({ ...form, category: e.target.value })}
                placeholder="General"
              />
            </div>
            <div>
              <Text>Description</Text>
              <TextInput
                value={form.description || ''}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                placeholder="Configuration description"
              />
            </div>
            <div>
              <Text>Sensitive</Text>
              <Select
                value={form.isSensitive ? 'true' : 'false'}
                onValueChange={(value) => setForm({ ...form, isSensitive: value === 'true' })}
              >
                <SelectItem value="false">No</SelectItem>
                <SelectItem value="true">Yes (masked in UI)</SelectItem>
              </Select>
            </div>
            <div>
              <Text>Active</Text>
              <Select
                value={form.isActive ? 'true' : 'false'}
                onValueChange={(value) => setForm({ ...form, isActive: value === 'true' })}
              >
                <SelectItem value="true">Active</SelectItem>
                <SelectItem value="false">Inactive</SelectItem>
              </Select>
            </div>
            <div className="flex gap-2 justify-end">
              <Button
                variant="secondary"
                onClick={() => {
                  setShowEditDialog(false);
                  setShowCreateDialog(false);
                }}
              >
                Cancel
              </Button>
              <Button onClick={handleSaveConfig} disabled={loading || !form.key || !form.value}>
                {loading ? 'Saving...' : 'Save'}
              </Button>
            </div>
          </div>
        </DialogPanel>
      </Dialog>
    </div>
  );
};
