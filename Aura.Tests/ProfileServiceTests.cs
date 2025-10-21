using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Profiles;
using Aura.Core.Services.Profiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class ProfileServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly Mock<ILogger<ProfilePersistence>> _persistenceLoggerMock;
    private readonly Mock<ILogger<ProfileService>> _serviceLoggerMock;
    private readonly ProfilePersistence _persistence;
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        // Create a unique test directory for each test run
        _testDirectory = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);

        _persistenceLoggerMock = new Mock<ILogger<ProfilePersistence>>();
        _serviceLoggerMock = new Mock<ILogger<ProfileService>>();

        _persistence = new ProfilePersistence(_persistenceLoggerMock.Object, _testDirectory);
        _profileService = new ProfileService(_serviceLoggerMock.Object, _persistence);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateProfile_ShouldCreateProfileWithDefaults()
    {
        // Arrange
        var userId = "user123";
        var request = new CreateProfileRequest(
            UserId: userId,
            ProfileName: "Test Profile",
            Description: "Test description",
            FromTemplateId: null
        );

        // Act
        var profile = await _profileService.CreateProfileAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(userId, profile.UserId);
        Assert.Equal("Test Profile", profile.ProfileName);
        Assert.Equal("Test description", profile.Description);
        Assert.True(profile.IsDefault); // First profile should be default
        Assert.True(profile.IsActive); // First profile should be active
    }

    [Fact]
    public async Task CreateProfile_FromTemplate_ShouldUseTemplatePreferences()
    {
        // Arrange
        var userId = "user123";
        var request = new CreateProfileRequest(
            UserId: userId,
            ProfileName: "Gaming Profile",
            Description: null,
            FromTemplateId: "youtube-gaming"
        );

        // Act
        var profile = await _profileService.CreateProfileAsync(request, CancellationToken.None);
        var preferences = await _profileService.GetPreferencesAsync(profile.ProfileId, CancellationToken.None);

        // Assert
        Assert.NotNull(profile);
        Assert.NotNull(preferences);
        Assert.Equal("gaming", preferences.ContentType);
        Assert.Equal(90, preferences.Tone?.Energy); // Gaming template should have high energy
    }

    [Fact]
    public async Task GetUserProfiles_ShouldReturnAllUserProfiles()
    {
        // Arrange
        var userId = "user123";
        await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Profile 1", null, null),
            CancellationToken.None);
        await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Profile 2", null, null),
            CancellationToken.None);

        // Act
        var profiles = await _profileService.GetUserProfilesAsync(userId, CancellationToken.None);

        // Assert
        Assert.Equal(2, profiles.Count);
        Assert.Contains(profiles, p => p.ProfileName == "Profile 1");
        Assert.Contains(profiles, p => p.ProfileName == "Profile 2");
    }

    [Fact]
    public async Task ActivateProfile_ShouldDeactivateOtherProfiles()
    {
        // Arrange
        var userId = "user123";
        var profile1 = await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Profile 1", null, null),
            CancellationToken.None);
        var profile2 = await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Profile 2", null, null),
            CancellationToken.None);

        // Act - Activate profile 2
        await _profileService.ActivateProfileAsync(profile2.ProfileId, CancellationToken.None);

        // Assert
        var profiles = await _profileService.GetUserProfilesAsync(userId, CancellationToken.None);
        var p1 = profiles.First(p => p.ProfileId == profile1.ProfileId);
        var p2 = profiles.First(p => p.ProfileId == profile2.ProfileId);

        Assert.False(p1.IsActive);
        Assert.True(p2.IsActive);
    }

    [Fact]
    public async Task UpdateProfile_ShouldUpdateMetadata()
    {
        // Arrange
        var userId = "user123";
        var profile = await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Original Name", "Original Description", null),
            CancellationToken.None);

        // Act
        var updated = await _profileService.UpdateProfileAsync(
            profile.ProfileId,
            new UpdateProfileRequest("Updated Name", "Updated Description"),
            CancellationToken.None);

        // Assert
        Assert.Equal("Updated Name", updated.ProfileName);
        Assert.Equal("Updated Description", updated.Description);
    }

    [Fact]
    public async Task DeleteProfile_ShouldNotDeleteOnlyProfile()
    {
        // Arrange
        var userId = "user123";
        var profile = await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Only Profile", null, null),
            CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _profileService.DeleteProfileAsync(profile.ProfileId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteProfile_ShouldPromoteAnotherAsDefault()
    {
        // Arrange
        var userId = "user123";
        var profile1 = await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Profile 1", null, null),
            CancellationToken.None);
        var profile2 = await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Profile 2", null, null),
            CancellationToken.None);

        // Act - Delete the default profile
        await _profileService.DeleteProfileAsync(profile1.ProfileId, CancellationToken.None);

        // Assert
        var profiles = await _profileService.GetUserProfilesAsync(userId, CancellationToken.None);
        Assert.Single(profiles);
        Assert.True(profiles[0].IsDefault);
        Assert.True(profiles[0].IsActive);
    }

    [Fact]
    public async Task DuplicateProfile_ShouldCopyPreferences()
    {
        // Arrange
        var userId = "user123";
        var original = await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Original", null, null),
            CancellationToken.None);

        // Update preferences on original
        await _profileService.UpdatePreferencesAsync(
            original.ProfileId,
            new UpdatePreferencesRequest(
                ContentType: "tutorial",
                Tone: new TonePreferences(70, 60, new System.Collections.Generic.List<string> { "friendly" }, null),
                Visual: null,
                Audio: null,
                Editing: null,
                Platform: null,
                AIBehavior: null
            ),
            CancellationToken.None);

        // Act
        var duplicate = await _profileService.DuplicateProfileAsync(
            original.ProfileId,
            "Duplicate",
            CancellationToken.None);

        // Assert
        var duplicatePrefs = await _profileService.GetPreferencesAsync(duplicate.ProfileId, CancellationToken.None);
        Assert.Equal("tutorial", duplicatePrefs.ContentType);
        Assert.Equal(70, duplicatePrefs.Tone?.Formality);
    }

    [Fact]
    public async Task UpdatePreferences_ShouldMergeWithExisting()
    {
        // Arrange
        var userId = "user123";
        var profile = await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Test Profile", null, null),
            CancellationToken.None);

        // Act - Update only tone preferences
        await _profileService.UpdatePreferencesAsync(
            profile.ProfileId,
            new UpdatePreferencesRequest(
                ContentType: null,
                Tone: new TonePreferences(80, 40, new System.Collections.Generic.List<string> { "professional" }, null),
                Visual: null,
                Audio: null,
                Editing: null,
                Platform: null,
                AIBehavior: null
            ),
            CancellationToken.None);

        // Assert - Other preferences should remain unchanged
        var preferences = await _profileService.GetPreferencesAsync(profile.ProfileId, CancellationToken.None);
        Assert.NotNull(preferences.Visual); // Should still have default visual preferences
        Assert.Equal(80, preferences.Tone?.Formality);
    }

    [Fact]
    public async Task RecordDecision_ShouldTrackUserChoices()
    {
        // Arrange
        var userId = "user123";
        var profile = await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Test Profile", null, null),
            CancellationToken.None);

        // Act
        await _profileService.RecordDecisionAsync(
            new RecordDecisionRequest(
                ProfileId: profile.ProfileId,
                SuggestionType: "tone_adjustment",
                Decision: "accepted",
                Context: new System.Collections.Generic.Dictionary<string, object>
                {
                    { "originalTone", "casual" },
                    { "suggestedTone", "professional" }
                }
            ),
            CancellationToken.None);

        // Assert
        var history = await _profileService.GetDecisionHistoryAsync(profile.ProfileId, CancellationToken.None);
        Assert.Single(history);
        Assert.Equal("tone_adjustment", history[0].SuggestionType);
        Assert.Equal("accepted", history[0].Decision);
    }

    [Fact]
    public async Task GetActiveProfile_ShouldReturnActiveProfile()
    {
        // Arrange
        var userId = "user123";
        var profile1 = await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Profile 1", null, null),
            CancellationToken.None);
        var profile2 = await _profileService.CreateProfileAsync(
            new CreateProfileRequest(userId, "Profile 2", null, null),
            CancellationToken.None);

        await _profileService.ActivateProfileAsync(profile2.ProfileId, CancellationToken.None);

        // Act
        var active = await _profileService.GetActiveProfileAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(active);
        Assert.Equal(profile2.ProfileId, active.ProfileId);
    }
}
