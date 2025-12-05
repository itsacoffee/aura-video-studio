/**
 * LUTSelector Component
 *
 * LUT (Look-Up Table) browser for applying cinematic color grades.
 * Supports built-in LUTs and custom .cube file uploads.
 */

import {
  makeStyles,
  tokens,
  Text,
  Slider,
  Button,
  Tooltip,
  Input,
} from '@fluentui/react-components';
import {
  Search24Regular,
  ArrowUpload24Regular,
  Delete24Regular,
  Checkmark24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, useMemo } from 'react';
import type { FC, ChangeEvent } from 'react';
import { openCutTokens } from '../../../styles/designTokens';
import { EmptyState } from '../EmptyState';

export interface LUTDefinition {
  id: string;
  name: string;
  description: string;
  thumbnailUrl?: string;
  category: 'cinematic' | 'vintage' | 'creative' | 'custom';
  isBuiltIn: boolean;
}

export interface LUTSelectorProps {
  selectedLutId: string | null;
  intensity: number;
  onLutSelect: (lutId: string | null) => void;
  onIntensityChange: (intensity: number) => void;
  onUploadLut?: (file: File) => void;
  className?: string;
}

// Built-in LUT definitions
const BUILTIN_LUTS: LUTDefinition[] = [
  {
    id: 'none',
    name: 'None',
    description: 'No LUT applied',
    category: 'cinematic',
    isBuiltIn: true,
  },
  {
    id: 'cinematic-orange-teal',
    name: 'Orange & Teal',
    description: 'Classic cinematic look with warm highlights and cool shadows',
    category: 'cinematic',
    isBuiltIn: true,
  },
  {
    id: 'cinematic-muted',
    name: 'Muted Cinema',
    description: 'Desaturated cinematic look with lifted blacks',
    category: 'cinematic',
    isBuiltIn: true,
  },
  {
    id: 'cinematic-warm',
    name: 'Warm Cinema',
    description: 'Golden hour warmth with rich highlights',
    category: 'cinematic',
    isBuiltIn: true,
  },
  {
    id: 'cinematic-cool',
    name: 'Cool Cinema',
    description: 'Cool blue tones with subtle contrast',
    category: 'cinematic',
    isBuiltIn: true,
  },
  {
    id: 'vintage-70s',
    name: '70s Film',
    description: 'Warm vintage look inspired by 70s cinema',
    category: 'vintage',
    isBuiltIn: true,
  },
  {
    id: 'vintage-faded',
    name: 'Faded Film',
    description: 'Soft faded look with lifted shadows',
    category: 'vintage',
    isBuiltIn: true,
  },
  {
    id: 'vintage-sepia',
    name: 'Sepia Tone',
    description: 'Classic sepia toned vintage look',
    category: 'vintage',
    isBuiltIn: true,
  },
  {
    id: 'creative-bleach',
    name: 'Bleach Bypass',
    description: 'High contrast desaturated look',
    category: 'creative',
    isBuiltIn: true,
  },
  {
    id: 'creative-cross',
    name: 'Cross Process',
    description: 'Creative color shift effect',
    category: 'creative',
    isBuiltIn: true,
  },
  {
    id: 'creative-noir',
    name: 'Film Noir',
    description: 'High contrast black and white look',
    category: 'creative',
    isBuiltIn: true,
  },
];

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalS,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    color: tokens.colorNeutralForeground2,
  },
  searchInput: {
    flex: 1,
  },
  uploadButton: {
    minWidth: 'auto',
  },
  intensitySection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} 0`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  intensityLabel: {
    minWidth: '64px',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  intensitySlider: {
    flex: 1,
  },
  intensityValue: {
    minWidth: '40px',
    textAlign: 'right',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
  },
  lutsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))',
    gap: tokens.spacingHorizontalS,
    maxHeight: '300px',
    overflow: 'auto',
  },
  lutCard: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
    transition: 'background-color 0.15s ease, transform 0.15s ease',
    border: `2px solid transparent`,
    position: 'relative',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
      transform: 'translateY(-2px)',
    },
  },
  lutCardSelected: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3Hover,
  },
  lutThumbnail: {
    width: '80px',
    height: '45px',
    borderRadius: tokens.borderRadiusSmall,
    backgroundColor: tokens.colorNeutralBackground4,
    marginBottom: tokens.spacingVerticalXS,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  lutThumbnailImage: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  lutThumbnailPlaceholder: {
    background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    width: '100%',
    height: '100%',
  },
  lutName: {
    fontSize: tokens.fontSizeBase200,
    textAlign: 'center',
    wordBreak: 'break-word',
  },
  selectedIndicator: {
    position: 'absolute',
    top: '4px',
    right: '4px',
    width: '20px',
    height: '20px',
    borderRadius: '50%',
    backgroundColor: tokens.colorBrandBackground,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    color: 'white',
  },
  categorySection: {
    marginBottom: tokens.spacingVerticalM,
  },
  categoryHeader: {
    marginBottom: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground2,
  },
  deleteButton: {
    position: 'absolute',
    top: '4px',
    left: '4px',
    opacity: 0,
    transition: 'opacity 0.15s ease',
    minWidth: '20px',
    minHeight: '20px',
    padding: '2px',
  },
  lutCardWrapper: {
    position: 'relative',
    ':hover .delete-button': {
      opacity: 1,
    },
  },
  hiddenInput: {
    display: 'none',
  },
});

const CATEGORY_LABELS: Record<LUTDefinition['category'], string> = {
  cinematic: 'Cinematic',
  vintage: 'Vintage',
  creative: 'Creative',
  custom: 'Custom',
};

export const LUTSelector: FC<LUTSelectorProps> = ({
  selectedLutId,
  intensity,
  onLutSelect,
  onIntensityChange,
  onUploadLut,
  className,
}) => {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');
  const [customLuts, setCustomLuts] = useState<LUTDefinition[]>([]);

  const allLuts = useMemo(() => [...BUILTIN_LUTS, ...customLuts], [customLuts]);

  const filteredLuts = useMemo(() => {
    if (!searchQuery) return allLuts;
    const query = searchQuery.toLowerCase();
    return allLuts.filter(
      (lut) =>
        lut.name.toLowerCase().includes(query) || lut.description.toLowerCase().includes(query)
    );
  }, [allLuts, searchQuery]);

  const groupedLuts = useMemo(() => {
    const groups: Record<string, LUTDefinition[]> = {};
    filteredLuts.forEach((lut) => {
      if (!groups[lut.category]) {
        groups[lut.category] = [];
      }
      groups[lut.category].push(lut);
    });
    return groups;
  }, [filteredLuts]);

  const handleFileUpload = useCallback(
    (e: ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      if (file && onUploadLut) {
        onUploadLut(file);
        // Add to custom LUTs
        const newLut: LUTDefinition = {
          id: `custom-${Date.now()}`,
          name: file.name.replace(/\.(cube|3dl)$/i, ''),
          description: 'Custom uploaded LUT',
          category: 'custom',
          isBuiltIn: false,
        };
        setCustomLuts((prev) => [...prev, newLut]);
      }
      // Reset input
      e.target.value = '';
    },
    [onUploadLut]
  );

  const handleRemoveCustomLut = useCallback(
    (lutId: string) => {
      setCustomLuts((prev) => prev.filter((lut) => lut.id !== lutId));
      if (selectedLutId === lutId) {
        onLutSelect(null);
      }
    },
    [selectedLutId, onLutSelect]
  );

  return (
    <div className={`${styles.container} ${className || ''}`}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Text weight="semibold" size={200}>
            LUT
          </Text>
        </div>
        <Input
          className={styles.searchInput}
          contentBefore={<Search24Regular />}
          placeholder="Search LUTs..."
          size="small"
          value={searchQuery}
          onChange={(_, data) => setSearchQuery(data.value)}
        />
        {onUploadLut && (
          <>
            <input
              type="file"
              accept=".cube,.3dl"
              className={styles.hiddenInput}
              id="lut-upload"
              onChange={handleFileUpload}
            />
            <Tooltip content="Upload custom LUT (.cube, .3dl)" relationship="label">
              <label
                htmlFor="lut-upload"
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  cursor: 'pointer',
                  padding: '4px',
                  borderRadius: tokens.borderRadiusSmall,
                }}
                aria-label="Upload custom LUT"
              >
                <ArrowUpload24Regular />
              </label>
            </Tooltip>
          </>
        )}
      </div>

      <div className={styles.intensitySection}>
        <Text className={styles.intensityLabel}>Intensity</Text>
        <Slider
          className={styles.intensitySlider}
          min={0}
          max={100}
          value={intensity}
          onChange={(_, data) => onIntensityChange(data.value)}
          size="small"
        />
        <Text className={styles.intensityValue}>{intensity}%</Text>
      </div>

      {filteredLuts.length === 0 ? (
        <EmptyState
          icon={<Search24Regular />}
          title="No LUTs found"
          description="Try a different search term"
          size="small"
        />
      ) : (
        Object.entries(groupedLuts).map(([category, luts]) => (
          <div key={category} className={styles.categorySection}>
            <Text weight="semibold" size={200} className={styles.categoryHeader}>
              {CATEGORY_LABELS[category as LUTDefinition['category']] || category}
            </Text>
            <div className={styles.lutsGrid}>
              {luts.map((lut) => (
                <div key={lut.id} className={styles.lutCardWrapper}>
                  <Tooltip content={lut.description} relationship="description" positioning="below">
                    <div
                      className={`${styles.lutCard} ${selectedLutId === lut.id ? styles.lutCardSelected : ''}`}
                      onClick={() => onLutSelect(lut.id === 'none' ? null : lut.id)}
                      role="button"
                      tabIndex={0}
                      aria-label={`${lut.name} LUT`}
                      aria-pressed={selectedLutId === lut.id}
                    >
                      <div className={styles.lutThumbnail}>
                        {lut.thumbnailUrl ? (
                          <img
                            src={lut.thumbnailUrl}
                            alt={lut.name}
                            className={styles.lutThumbnailImage}
                          />
                        ) : (
                          <div className={styles.lutThumbnailPlaceholder} />
                        )}
                      </div>
                      <Text className={styles.lutName}>{lut.name}</Text>
                      {selectedLutId === lut.id && (
                        <div className={styles.selectedIndicator}>
                          <Checkmark24Regular style={{ fontSize: '14px' }} />
                        </div>
                      )}
                    </div>
                  </Tooltip>
                  {!lut.isBuiltIn && (
                    <Tooltip content="Remove custom LUT" relationship="label">
                      <Button
                        className={`${styles.deleteButton} delete-button`}
                        appearance="subtle"
                        size="small"
                        icon={<Delete24Regular />}
                        onClick={(e) => {
                          e.stopPropagation();
                          handleRemoveCustomLut(lut.id);
                        }}
                        aria-label={`Remove ${lut.name}`}
                      />
                    </Tooltip>
                  )}
                </div>
              ))}
            </div>
          </div>
        ))
      )}
    </div>
  );
};

export default LUTSelector;
