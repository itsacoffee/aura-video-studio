import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Card,
  RadioGroup,
  Radio,
  Badge,
  Divider,
  Spinner,
  Link,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Filled,
  DismissCircle24Filled,
  Lightbulb24Regular,
  Info24Regular,
  Warning24Regular,
  ArrowRight24Regular,
} from '@fluentui/react-icons';
import { useEffect, useState, useCallback } from 'react';
import * as providerProfilesApi from '../../api/providerProfiles';
import { useProviderProfilesStore } from '../../state/providerProfiles';
import type { ProviderProfileDto } from '../../types/api-v1';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalXL,
  },
  infoBox: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
  },
  recommendBox: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorBrandBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  profileCard: {
    marginBottom: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  selectedProfile: {
    outline: `2px solid ${tokens.colorBrandStroke1}`,
  },
  profileHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalS,
  },
  profileTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  validationStatus: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalXS,
  },
  validationDetails: {
    marginTop: tokens.spacingVerticalS,
    marginLeft: '32px',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  errorList: {
    marginTop: tokens.spacingVerticalXS,
    marginLeft: tokens.spacingHorizontalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  fixLink: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
    cursor: 'pointer',
    color: tokens.colorBrandForeground1,
  },
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalL,
  },
  changesSummary: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
});

export function ProviderProfilesTab() {
  const styles = useStyles();
  const {
    profiles,
    activeProfile,
    recommendation,
    validationResults,
    loading,
    setProfiles,
    setActiveProfile,
    setRecommendation,
    setValidationResult,
    setLoading,
    setError,
  } = useProviderProfilesStore();

  const [selectedProfileId, setSelectedProfileId] = useState<string | null>(null);
  const [validating, setValidating] = useState<Record<string, boolean>>({});
  const [appliedChanges, setAppliedChanges] = useState<string | null>(null);

  useEffect(() => {
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      const [profilesData, activeProfileData, recommendationData] = await Promise.all([
        providerProfilesApi.getProfiles(),
        providerProfilesApi.getActiveProfile(),
        providerProfilesApi.getRecommendedProfile(),
      ]);

      setProfiles(profilesData);
      setActiveProfile(activeProfileData);
      setSelectedProfileId(activeProfileData.id);
      setRecommendation(recommendationData);
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to load profiles';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleProfileSelect = (profileId: string) => {
    setSelectedProfileId(profileId);
  };

  const handleApplyProfile = async () => {
    if (!selectedProfileId) return;

    setLoading(true);
    try {
      const previousProfile = activeProfile;
      const result = await providerProfilesApi.setActiveProfile(selectedProfileId);
      setActiveProfile(result.profile);
      setError(null);

      const changesDescription = generateChangesDescription(previousProfile, result.profile);
      setAppliedChanges(changesDescription);

      setTimeout(() => setAppliedChanges(null), 10000);
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to apply profile';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const generateChangesDescription = (
    previous: ProviderProfileDto | null,
    current: ProviderProfileDto
  ): string => {
    if (!previous) return `Applied profile: ${current.name}`;

    const changes: string[] = [];

    if (previous.tier !== current.tier) {
      changes.push(`Tier changed from ${previous.tier} to ${current.tier}`);
    }

    const previousKeys = new Set(previous.requiredApiKeys);
    const currentKeys = new Set(current.requiredApiKeys);
    const addedKeys = [...currentKeys].filter((k) => !previousKeys.has(k));
    const removedKeys = [...previousKeys].filter((k) => !currentKeys.has(k));

    if (addedKeys.length > 0) {
      changes.push(`Now requires: ${addedKeys.join(', ')}`);
    }
    if (removedKeys.length > 0) {
      changes.push(`No longer requires: ${removedKeys.join(', ')}`);
    }

    return changes.length > 0
      ? `Applied ${current.name}. Changes: ${changes.join('; ')}`
      : `Applied profile: ${current.name}`;
  };

  const handleValidateProfile = useCallback(
    async (profileId: string) => {
      setValidating((prev) => ({ ...prev, [profileId]: true }));
      try {
        const result = await providerProfilesApi.validateProfile(profileId);
        setValidationResult(profileId, result);
      } catch (error: unknown) {
        console.error('Validation failed:', error);
      } finally {
        setValidating((prev) => ({ ...prev, [profileId]: false }));
      }
    },
    [setValidationResult]
  );

  const getTierBadge = (tier: string) => {
    switch (tier) {
      case 'FreeOnly':
        return (
          <Badge appearance="tint" color="success">
            Free
          </Badge>
        );
      case 'BalancedMix':
        return (
          <Badge appearance="tint" color="informative">
            Balanced
          </Badge>
        );
      case 'ProMax':
        return (
          <Badge appearance="tint" color="important">
            Premium
          </Badge>
        );
      default:
        return null;
    }
  };

  const navigateToApiKeys = () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
    setTimeout(() => {
      const apiKeysCard = document.querySelector('[data-category="apikeys"]');
      if (apiKeysCard) {
        (apiKeysCard as HTMLElement).click();
      }
    }, 100);
  };

  const navigateToEngines = () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
    setTimeout(() => {
      const enginesCard = document.querySelector('[data-category="localengines"]');
      if (enginesCard) {
        (enginesCard as HTMLElement).click();
      }
    }, 100);
  };

  const renderValidationStatus = (profile: ProviderProfileDto) => {
    const result = validationResults[profile.id];
    const isValidating = validating[profile.id];

    if (isValidating) {
      return (
        <div className={styles.validationStatus}>
          <Spinner size="tiny" />
          <Text size={200}>Validating...</Text>
        </div>
      );
    }

    if (!result) {
      return (
        <Button size="small" onClick={() => handleValidateProfile(profile.id)}>
          Validate
        </Button>
      );
    }

    return (
      <>
        <div className={styles.validationStatus}>
          {result.isValid ? (
            <>
              <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
              <Text size={200}>{result.message}</Text>
            </>
          ) : (
            <>
              <DismissCircle24Filled style={{ color: tokens.colorPaletteRedForeground1 }} />
              <Text size={200}>{result.message}</Text>
            </>
          )}
          <Button size="small" onClick={() => handleValidateProfile(profile.id)}>
            Re-validate
          </Button>
        </div>

        {!result.isValid && (result.errors.length > 0 || result.warnings.length > 0) && (
          <div className={styles.validationDetails}>
            {result.errors.length > 0 && (
              <>
                <Text
                  size={200}
                  weight="semibold"
                  style={{ color: tokens.colorPaletteRedForeground1 }}
                >
                  Issues found:
                </Text>
                <div className={styles.errorList}>
                  {result.errors.map((error, index) => (
                    <Text key={index} size={200}>
                      • {error}
                    </Text>
                  ))}
                </div>
              </>
            )}

            {result.missingKeys.length > 0 && (
              <div style={{ marginTop: tokens.spacingVerticalS }}>
                <Link className={styles.fixLink} onClick={navigateToApiKeys}>
                  <Text size={200} weight="semibold">
                    Configure API Keys
                  </Text>
                  <ArrowRight24Regular fontSize={16} />
                </Link>
              </div>
            )}

            {result.errors.some(
              (e) =>
                e.toLowerCase().includes('engine') ||
                e.toLowerCase().includes('ollama') ||
                e.toLowerCase().includes('ffmpeg')
            ) && (
              <div style={{ marginTop: tokens.spacingVerticalS }}>
                <Link className={styles.fixLink} onClick={navigateToEngines}>
                  <Text size={200} weight="semibold">
                    Install/Configure Engines
                  </Text>
                  <ArrowRight24Regular fontSize={16} />
                </Link>
              </div>
            )}

            {result.warnings.length > 0 && (
              <div style={{ marginTop: tokens.spacingVerticalS }}>
                <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />
                <Text size={200} style={{ marginLeft: tokens.spacingHorizontalXS }}>
                  {result.warnings.join('; ')}
                </Text>
              </div>
            )}
          </div>
        )}
      </>
    );
  };

  if (loading && profiles.length === 0) {
    return (
      <Card className={styles.section}>
        <div
          style={{ display: 'flex', justifyContent: 'center', padding: tokens.spacingVerticalXXXL }}
        >
          <Spinner label="Loading profiles..." />
        </div>
      </Card>
    );
  }

  return (
    <Card className={styles.section}>
      <Title2>Provider Profiles</Title2>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        Choose a provider profile to configure which services are used for video generation. Each
        profile offers different trade-offs between cost, quality, and requirements.
      </Text>

      {appliedChanges && (
        <MessageBar intent="success" style={{ marginBottom: tokens.spacingVerticalL }}>
          <MessageBarBody>
            <MessageBarTitle>Profile Applied Successfully</MessageBarTitle>
            {appliedChanges}
          </MessageBarBody>
        </MessageBar>
      )}

      {recommendation && (
        <div className={styles.recommendBox}>
          <Lightbulb24Regular style={{ fontSize: '24px', color: tokens.colorBrandForeground1 }} />
          <div style={{ flex: 1 }}>
            <Text weight="semibold" size={300}>
              Smart Recommendation: {recommendation.recommendedProfileName}
            </Text>
            <br />
            <Text size={200}>{recommendation.reason}</Text>
            {recommendation.missingKeysForProMax.length > 0 && (
              <>
                <br />
                <Text size={200} style={{ fontStyle: 'italic' }}>
                  For Pro-Max, you still need: {recommendation.missingKeysForProMax.join(', ')}
                </Text>
              </>
            )}
          </div>
        </div>
      )}

      <div className={styles.infoBox}>
        <Text weight="semibold" size={300}>
          💡 About Provider Profiles
        </Text>
        <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
          Provider profiles determine which AI services are used for script generation, voice
          synthesis, and visual creation. The active profile applies to all new video generation
          jobs.
        </Text>
      </div>

      <div style={{ marginBottom: tokens.spacingVerticalL }}>
        <Title3>Available Profiles</Title3>
        <RadioGroup
          value={selectedProfileId || ''}
          onChange={(_, data) => handleProfileSelect(data.value)}
        >
          {profiles.map((profile) => (
            <Card
              key={profile.id}
              className={`${styles.profileCard} ${selectedProfileId === profile.id ? styles.selectedProfile : ''}`}
              onClick={() => handleProfileSelect(profile.id)}
            >
              <div className={styles.profileHeader}>
                <div className={styles.profileTitle}>
                  <Radio value={profile.id} />
                  <Title3>{profile.name}</Title3>
                  {getTierBadge(profile.tier)}
                  {activeProfile?.id === profile.id && (
                    <Badge appearance="filled" color="brand">
                      Active
                    </Badge>
                  )}
                </div>
              </div>

              <Text
                size={300}
                style={{ marginLeft: '32px', marginBottom: tokens.spacingVerticalS }}
              >
                {profile.description}
              </Text>

              <Text
                size={200}
                style={{
                  marginLeft: '32px',
                  color: tokens.colorNeutralForeground3,
                  display: 'flex',
                  alignItems: 'center',
                  gap: tokens.spacingHorizontalXS,
                }}
              >
                <Info24Regular style={{ fontSize: '16px' }} />
                {profile.usageNotes}
              </Text>

              {profile.requiredApiKeys.length > 0 && (
                <Text
                  size={200}
                  style={{
                    marginLeft: '32px',
                    marginTop: tokens.spacingVerticalS,
                    color: tokens.colorNeutralForeground3,
                  }}
                >
                  Required API keys: {profile.requiredApiKeys.join(', ')}
                </Text>
              )}

              <div style={{ marginLeft: '32px', marginTop: tokens.spacingVerticalS }}>
                {renderValidationStatus(profile)}
              </div>
            </Card>
          ))}
        </RadioGroup>
      </div>

      <Divider />

      <div className={styles.buttonGroup}>
        <Button
          appearance="primary"
          onClick={handleApplyProfile}
          disabled={!selectedProfileId || selectedProfileId === activeProfile?.id || loading}
        >
          {loading ? 'Applying...' : 'Apply Profile'}
        </Button>
        <Button onClick={loadData} disabled={loading}>
          Refresh
        </Button>
      </div>
    </Card>
  );
}
