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
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Filled,
  DismissCircle24Filled,
  Lightbulb24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { useEffect, useState, useCallback } from 'react';
import { useProviderProfilesStore } from '../../state/providerProfiles';
import * as providerProfilesApi from '../../api/providerProfiles';
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
    borderColor: tokens.colorBrandStroke1,
    borderWidth: '2px',
    borderStyle: 'solid',
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
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalL,
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

  useEffect(() => {
    loadData();
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
      const result = await providerProfilesApi.setActiveProfile(selectedProfileId);
      setActiveProfile(result.profile);
      setError(null);
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error ? error.message : 'Failed to apply profile';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
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
        return <Badge appearance="tint" color="success">Free</Badge>;
      case 'BalancedMix':
        return <Badge appearance="tint" color="informative">Balanced</Badge>;
      case 'ProMax':
        return <Badge appearance="tint" color="important">Premium</Badge>;
      default:
        return null;
    }
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
    );
  };

  if (loading && profiles.length === 0) {
    return (
      <Card className={styles.section}>
        <div style={{ display: 'flex', justifyContent: 'center', padding: tokens.spacingVerticalXXXL }}>
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

      {recommendation && (
        <div className={styles.recommendBox}>
          <Lightbulb24Regular style={{ fontSize: '24px', color: tokens.colorBrandForeground1 }} />
          <div style={{ flex: 1 }}>
            <Text weight="semibold" size={300}>
              Recommended: {recommendation.recommendedProfileName}
            </Text>
            <br />
            <Text size={200}>{recommendation.reason}</Text>
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

              <Text size={300} style={{ marginLeft: '32px', marginBottom: tokens.spacingVerticalS }}>
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
