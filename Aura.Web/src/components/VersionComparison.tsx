import { Badge, Button, Spinner, Text } from '@fluentui/react-components';
import { ArrowRight24Regular, Checkmark24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import React, { useCallback, useEffect, useState } from 'react';
import { compareVersions } from '@/api/versions';
import type { VersionComparisonResponse } from '@/types/api-v1';

interface VersionComparisonProps {
  projectId: string;
  version1Id: string;
  version2Id: string;
  onClose: () => void;
  className?: string;
}

/**
 * Component that displays a comparison between two project versions
 */
const VersionComparison: React.FC<VersionComparisonProps> = ({
  projectId,
  version1Id,
  version2Id,
  onClose,
  className,
}) => {
  const [comparison, setComparison] = useState<VersionComparisonResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadComparison = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await compareVersions(projectId, version1Id, version2Id);
      setComparison(data);
    } catch (err) {
      const error = err as Error;
      setError(error.message || 'Failed to load comparison');
    } finally {
      setLoading(false);
    }
  }, [projectId, version1Id, version2Id]);

  useEffect(() => {
    void loadComparison();
  }, [loadComparison]);

  const renderChangeIndicator = (changed: boolean) => {
    return changed ? (
      <Badge appearance="filled" color="warning">
        Changed
      </Badge>
    ) : (
      <Badge appearance="tint" color="success">
        Unchanged
      </Badge>
    );
  };

  const renderJsonDiff = (label: string, json1?: string | null, json2?: string | null) => {
    if (!json1 && !json2) {
      return null;
    }

    let obj1: unknown = null;
    let obj2: unknown = null;

    try {
      obj1 = json1 ? JSON.parse(json1) : null;
      obj2 = json2 ? JSON.parse(json2) : null;
    } catch {
      return (
        <div className="text-red-600">
          <Text>Error parsing JSON data for {label}</Text>
        </div>
      );
    }

    return (
      <div className="mb-6">
        <Text weight="semibold" size={400} className="mb-2 block">
          {label}
        </Text>
        <div className="grid grid-cols-2 gap-4">
          <div className="border rounded p-3 bg-gray-50">
            <Text weight="semibold" size={300} className="mb-2 block">
              Version {comparison?.version1Number}
            </Text>
            <pre className="text-xs overflow-auto max-h-60 whitespace-pre-wrap">
              {JSON.stringify(obj1, null, 2)}
            </pre>
          </div>
          <div className="border rounded p-3 bg-gray-50">
            <Text weight="semibold" size={300} className="mb-2 block">
              Version {comparison?.version2Number}
            </Text>
            <pre className="text-xs overflow-auto max-h-60 whitespace-pre-wrap">
              {JSON.stringify(obj2, null, 2)}
            </pre>
          </div>
        </div>
      </div>
    );
  };

  if (loading) {
    return (
      <div className={`flex items-center justify-center p-8 ${className || ''}`}>
        <Spinner label="Loading comparison..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className={`p-4 bg-red-50 text-red-800 rounded-md ${className || ''}`}>
        <Text weight="semibold">Error: {error}</Text>
        <Button appearance="secondary" onClick={onClose} className="mt-2">
          Close
        </Button>
      </div>
    );
  }

  if (!comparison) {
    return null;
  }

  const hasAnyChanges =
    comparison.briefChanged ||
    comparison.planChanged ||
    comparison.voiceChanged ||
    comparison.renderChanged ||
    comparison.timelineChanged;

  return (
    <div className={`flex flex-col ${className || ''}`}>
      <div className="flex items-center justify-between mb-6 pb-4 border-b">
        <div className="flex items-center gap-3">
          <Text size={500} weight="semibold">
            Version Comparison
          </Text>
          <div className="flex items-center gap-2">
            <Badge appearance="filled" color="brand">
              v{comparison.version1Number}
            </Badge>
            <ArrowRight24Regular />
            <Badge appearance="filled" color="brand">
              v{comparison.version2Number}
            </Badge>
          </div>
        </div>
        <Button appearance="subtle" icon={<Dismiss24Regular />} onClick={onClose}>
          Close
        </Button>
      </div>

      {!hasAnyChanges ? (
        <div className="p-8 text-center bg-green-50 rounded-md">
          <Checkmark24Regular className="text-green-600 mb-2" style={{ fontSize: 48 }} />
          <Text size={400}>No changes detected between these versions</Text>
        </div>
      ) : (
        <>
          <div className="mb-6">
            <Text weight="semibold" size={400} className="mb-3 block">
              Change Summary
            </Text>
            <div className="grid grid-cols-2 gap-3">
              <div className="flex items-center justify-between p-3 border rounded">
                <Text>Brief</Text>
                {renderChangeIndicator(comparison.briefChanged)}
              </div>
              <div className="flex items-center justify-between p-3 border rounded">
                <Text>Plan</Text>
                {renderChangeIndicator(comparison.planChanged)}
              </div>
              <div className="flex items-center justify-between p-3 border rounded">
                <Text>Voice Settings</Text>
                {renderChangeIndicator(comparison.voiceChanged)}
              </div>
              <div className="flex items-center justify-between p-3 border rounded">
                <Text>Render Settings</Text>
                {renderChangeIndicator(comparison.renderChanged)}
              </div>
              <div className="flex items-center justify-between p-3 border rounded">
                <Text>Timeline</Text>
                {renderChangeIndicator(comparison.timelineChanged)}
              </div>
            </div>
          </div>

          <div className="space-y-4">
            {comparison.briefChanged &&
              renderJsonDiff(
                'Brief',
                comparison.version1Data.briefJson,
                comparison.version2Data.briefJson
              )}
            {comparison.planChanged &&
              renderJsonDiff(
                'Plan',
                comparison.version1Data.planSpecJson,
                comparison.version2Data.planSpecJson
              )}
            {comparison.voiceChanged &&
              renderJsonDiff(
                'Voice Settings',
                comparison.version1Data.voiceSpecJson,
                comparison.version2Data.voiceSpecJson
              )}
            {comparison.renderChanged &&
              renderJsonDiff(
                'Render Settings',
                comparison.version1Data.renderSpecJson,
                comparison.version2Data.renderSpecJson
              )}
            {comparison.timelineChanged &&
              renderJsonDiff(
                'Timeline',
                comparison.version1Data.timelineJson,
                comparison.version2Data.timelineJson
              )}
          </div>
        </>
      )}
    </div>
  );
};

export default VersionComparison;
