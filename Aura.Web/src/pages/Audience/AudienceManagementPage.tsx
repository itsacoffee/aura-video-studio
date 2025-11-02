import {
  Button,
  Input,
  Card,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Spinner,
  MessageBar,
  MessageBarBody,
  Badge,
  tokens,
} from '@fluentui/react-components';
import {
  AddRegular,
  DeleteRegular,
  EditRegular,
  SearchRegular,
  StarRegular,
  StarFilled,
} from '@fluentui/react-icons';
import React, { useState, useEffect } from 'react';
import apiClient from '../../services/api/apiClient';
import type { AudienceProfileDto } from '../../types/api-v1';

interface AudienceProfile {
  id: string;
  name: string;
  description: string;
  ageRange?: {
    minAge: number;
    maxAge: number;
    displayName: string;
  };
  educationLevel?: string;
  profession?: string;
  industry?: string;
  expertiseLevel?: string;
  isFavorite: boolean;
  isTemplate: boolean;
  usageCount: number;
  tags?: string[];
}

export const AudienceManagementPage: React.FC = () => {
  const [profiles, setProfiles] = useState<AudienceProfile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [filterFavorites, setFilterFavorites] = useState(false);
  const [selectedProfile, setSelectedProfile] = useState<AudienceProfile | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  useEffect(() => {
    loadProfiles();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadProfiles = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await apiClient.get<{ profiles: AudienceProfileDto[]; totalCount: number }>(
        '/api/audience/profiles'
      );
      setProfiles(response.data.profiles.map(mapToProfile));
    } catch (err) {
      setError('Failed to load audience profiles');
      console.error('Error loading profiles:', err);
    } finally {
      setLoading(false);
    }
  };

  const mapToProfile = (dto: AudienceProfileDto): AudienceProfile => ({
    id: dto.id || '',
    name: dto.name,
    description: dto.description || '',
    ageRange: dto.ageRange
      ? {
          minAge: dto.ageRange.minAge,
          maxAge: dto.ageRange.maxAge,
          displayName: dto.ageRange.displayName,
        }
      : undefined,
    educationLevel: dto.educationLevel ?? undefined,
    profession: dto.profession ?? undefined,
    industry: dto.industry ?? undefined,
    expertiseLevel: dto.expertiseLevel ?? undefined,
    isFavorite: dto.isFavorite,
    isTemplate: dto.isTemplate,
    usageCount: dto.usageCount,
    tags: dto.tags ?? undefined,
  });

  const handleToggleFavorite = async (profileId: string) => {
    try {
      await apiClient.post(`/api/audience/profiles/${profileId}/favorite`);
      await loadProfiles();
    } catch (err) {
      console.error('Error toggling favorite:', err);
    }
  };

  const handleDeleteProfile = async () => {
    if (!selectedProfile) return;

    try {
      await apiClient.delete(`/api/audience/profiles/${selectedProfile.id}`);
      setDeleteDialogOpen(false);
      setSelectedProfile(null);
      await loadProfiles();
    } catch (err) {
      setError('Failed to delete profile');
      console.error('Error deleting profile:', err);
    }
  };

  const filteredProfiles = profiles.filter((profile) => {
    const matchesSearch =
      profile.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      profile.description.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesFavorites = !filterFavorites || profile.isFavorite;
    return matchesSearch && matchesFavorites;
  });

  if (loading) {
    return (
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          minHeight: '400px',
        }}
      >
        <Spinner size="large" label="Loading audience profiles..." />
      </div>
    );
  }

  return (
    <div style={{ padding: tokens.spacingVerticalXL }}>
      <div style={{ marginBottom: tokens.spacingVerticalXL }}>
        <h1>Audience Profile Management</h1>
        <p style={{ color: tokens.colorNeutralForeground3 }}>
          Create and manage audience profiles to tailor your content for specific demographics.
        </p>
      </div>

      {error && (
        <MessageBar intent="error" style={{ marginBottom: tokens.spacingVerticalM }}>
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      <div
        style={{
          display: 'flex',
          gap: tokens.spacingHorizontalM,
          marginBottom: tokens.spacingVerticalL,
          flexWrap: 'wrap',
        }}
      >
        <Input
          placeholder="Search profiles..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          contentBefore={<SearchRegular />}
          style={{ flexGrow: 1, minWidth: '200px' }}
        />
        <Button
          appearance={filterFavorites ? 'primary' : 'secondary'}
          icon={filterFavorites ? <StarFilled /> : <StarRegular />}
          onClick={() => setFilterFavorites(!filterFavorites)}
        >
          {filterFavorites ? 'Show All' : 'Favorites Only'}
        </Button>
        <Button
          appearance="primary"
          icon={<AddRegular />}
          onClick={() => (window.location.href = '/audience/create')}
        >
          Create Profile
        </Button>
      </div>

      <Card>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Name</TableHeaderCell>
              <TableHeaderCell>Description</TableHeaderCell>
              <TableHeaderCell>Age Range</TableHeaderCell>
              <TableHeaderCell>Education</TableHeaderCell>
              <TableHeaderCell>Expertise</TableHeaderCell>
              <TableHeaderCell>Usage</TableHeaderCell>
              <TableHeaderCell>Tags</TableHeaderCell>
              <TableHeaderCell>Actions</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredProfiles.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={8}
                  style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}
                >
                  {searchQuery || filterFavorites
                    ? 'No profiles match your filters'
                    : 'No audience profiles yet. Create one to get started!'}
                </TableCell>
              </TableRow>
            ) : (
              filteredProfiles.map((profile) => (
                <TableRow key={profile.id}>
                  <TableCell>
                    <div
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: tokens.spacingHorizontalS,
                      }}
                    >
                      {profile.isFavorite && (
                        <StarFilled style={{ color: tokens.colorPaletteYellowForeground1 }} />
                      )}
                      <strong>{profile.name}</strong>
                      {profile.isTemplate && (
                        <Badge appearance="outline" color="informative">
                          Template
                        </Badge>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>{profile.description}</TableCell>
                  <TableCell>{profile.ageRange?.displayName || '-'}</TableCell>
                  <TableCell>{profile.educationLevel || '-'}</TableCell>
                  <TableCell>{profile.expertiseLevel || '-'}</TableCell>
                  <TableCell>{profile.usageCount}</TableCell>
                  <TableCell>
                    <div
                      style={{ display: 'flex', gap: tokens.spacingHorizontalXS, flexWrap: 'wrap' }}
                    >
                      {profile.tags?.slice(0, 3).map((tag) => (
                        <Badge key={tag} size="small">
                          {tag}
                        </Badge>
                      ))}
                      {(profile.tags?.length || 0) > 3 && (
                        <Badge size="small" color="informative">
                          +{(profile.tags?.length || 0) - 3}
                        </Badge>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
                      <Button
                        size="small"
                        icon={profile.isFavorite ? <StarFilled /> : <StarRegular />}
                        appearance="subtle"
                        onClick={() => handleToggleFavorite(profile.id)}
                        title={profile.isFavorite ? 'Remove from favorites' : 'Add to favorites'}
                      />
                      <Button
                        size="small"
                        icon={<EditRegular />}
                        appearance="subtle"
                        onClick={() => (window.location.href = `/audience/edit/${profile.id}`)}
                        title="Edit profile"
                      />
                      <Button
                        size="small"
                        icon={<DeleteRegular />}
                        appearance="subtle"
                        onClick={() => {
                          setSelectedProfile(profile);
                          setDeleteDialogOpen(true);
                        }}
                        title="Delete profile"
                      />
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </Card>

      <Dialog open={deleteDialogOpen} onOpenChange={(_, data) => setDeleteDialogOpen(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Delete Audience Profile</DialogTitle>
            <DialogContent>
              Are you sure you want to delete the profile &quot;{selectedProfile?.name}&quot;? This
              action cannot be undone.
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setDeleteDialogOpen(false)}>
                Cancel
              </Button>
              <Button appearance="primary" onClick={handleDeleteProfile}>
                Delete
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
};

export default AudienceManagementPage;
