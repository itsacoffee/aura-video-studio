import React, { useState } from 'react';
import {
  Card,
  Title3,
  Body1,
  Button,
  Input,
  Textarea,
  Field,
  Spinner,
  makeStyles,
  tokens,
  Badge,
} from '@fluentui/react-components';
import { Sparkle24Regular, Copy24Regular, CheckmarkCircle24Regular } from '@fluentui/react-icons';
import type { MetadataGenerationRequest, OptimizedMetadata } from '../../types/platform';
import platformService from '../../services/platform/platformService';
import { PlatformSelector } from './PlatformSelector';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
  },
  result: {
    marginTop: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  metadataSection: {
    marginBottom: tokens.spacingVerticalM,
  },
  tagList: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  copyButton: {
    marginLeft: tokens.spacingHorizontalS,
  },
});

export const MetadataGenerator: React.FC = () => {
  const styles = useStyles();
  const [selectedPlatform, setSelectedPlatform] = useState<string>('');
  const [videoTitle, setVideoTitle] = useState('');
  const [videoDescription, setVideoDescription] = useState('');
  const [keywords, setKeywords] = useState('');
  const [targetAudience, setTargetAudience] = useState('');
  const [contentType, setContentType] = useState('');
  const [loading, setLoading] = useState(false);
  const [metadata, setMetadata] = useState<OptimizedMetadata | null>(null);
  const [copiedField, setCopiedField] = useState<string | null>(null);

  const handleGenerate = async () => {
    if (!selectedPlatform || !videoTitle) {
      return;
    }

    try {
      setLoading(true);
      const request: MetadataGenerationRequest = {
        platform: selectedPlatform,
        videoTitle,
        videoDescription,
        keywords: keywords
          .split(',')
          .map((k) => k.trim())
          .filter((k) => k),
        targetAudience,
        contentType,
      };

      const result = await platformService.generateMetadata(request);
      setMetadata(result);
    } catch (error) {
      console.error('Failed to generate metadata:', error);
    } finally {
      setLoading(false);
    }
  };

  const copyToClipboard = async (text: string, field: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopiedField(field);
      setTimeout(() => setCopiedField(null), 2000);
    } catch (error) {
      console.error('Failed to copy:', error);
    }
  };

  return (
    <div className={styles.container}>
      <div>
        <Title3>AI Metadata Generator</Title3>
        <Body1>Generate platform-optimized titles, descriptions, tags, and hashtags</Body1>
      </div>

      <PlatformSelector
        selectedPlatforms={selectedPlatform ? [selectedPlatform] : []}
        onPlatformsSelected={(platforms) => setSelectedPlatform(platforms[0] || '')}
        singleSelect
      />

      <Card>
        <div className={styles.form}>
          <Field label="Video Title" required>
            <Input
              value={videoTitle}
              onChange={(_, data) => setVideoTitle(data.value)}
              placeholder="Enter your video title"
            />
          </Field>

          <Field label="Video Description">
            <Textarea
              value={videoDescription}
              onChange={(_, data) => setVideoDescription(data.value)}
              placeholder="Describe your video content"
              rows={3}
            />
          </Field>

          <Field label="Keywords (comma-separated)">
            <Input
              value={keywords}
              onChange={(_, data) => setKeywords(data.value)}
              placeholder="keyword1, keyword2, keyword3"
            />
          </Field>

          <Field label="Target Audience">
            <Input
              value={targetAudience}
              onChange={(_, data) => setTargetAudience(data.value)}
              placeholder="e.g., video editors, content creators"
            />
          </Field>

          <Field label="Content Type">
            <Input
              value={contentType}
              onChange={(_, data) => setContentType(data.value)}
              placeholder="e.g., tutorial, entertainment, educational"
            />
          </Field>

          <div className={styles.buttonGroup}>
            <Button
              appearance="primary"
              icon={<Sparkle24Regular />}
              onClick={handleGenerate}
              disabled={!selectedPlatform || !videoTitle || loading}
            >
              {loading ? 'Generating...' : 'Generate Metadata'}
            </Button>
            {loading && <Spinner size="small" />}
          </div>
        </div>
      </Card>

      {metadata && (
        <Card className={styles.result}>
          <Title3>Generated Metadata</Title3>

          <div className={styles.metadataSection}>
            <Field label="Optimized Title">
              <div style={{ display: 'flex', alignItems: 'center' }}>
                <Input value={metadata.title} readOnly style={{ flex: 1 }} />
                <Button
                  className={styles.copyButton}
                  icon={copiedField === 'title' ? <CheckmarkCircle24Regular /> : <Copy24Regular />}
                  onClick={() => copyToClipboard(metadata.title, 'title')}
                />
              </div>
            </Field>
          </div>

          <div className={styles.metadataSection}>
            <Field label="Description">
              <Textarea value={metadata.description} readOnly rows={4} />
              <Button
                className={styles.copyButton}
                icon={
                  copiedField === 'description' ? <CheckmarkCircle24Regular /> : <Copy24Regular />
                }
                onClick={() => copyToClipboard(metadata.description, 'description')}
              >
                Copy
              </Button>
            </Field>
          </div>

          {metadata.tags.length > 0 && (
            <div className={styles.metadataSection}>
              <Field label="Tags">
                <div className={styles.tagList}>
                  {metadata.tags.map((tag, index) => (
                    <Badge key={index} appearance="outline">
                      {tag}
                    </Badge>
                  ))}
                </div>
                <Button
                  className={styles.copyButton}
                  icon={copiedField === 'tags' ? <CheckmarkCircle24Regular /> : <Copy24Regular />}
                  onClick={() => copyToClipboard(metadata.tags.join(', '), 'tags')}
                >
                  Copy All Tags
                </Button>
              </Field>
            </div>
          )}

          {metadata.hashtags.length > 0 && (
            <div className={styles.metadataSection}>
              <Field label="Hashtags">
                <div className={styles.tagList}>
                  {metadata.hashtags.map((hashtag, index) => (
                    <Badge key={index} appearance="filled" color="brand">
                      #{hashtag}
                    </Badge>
                  ))}
                </div>
                <Button
                  className={styles.copyButton}
                  icon={
                    copiedField === 'hashtags' ? <CheckmarkCircle24Regular /> : <Copy24Regular />
                  }
                  onClick={() =>
                    copyToClipboard(metadata.hashtags.map((h) => `#${h}`).join(' '), 'hashtags')
                  }
                >
                  Copy All Hashtags
                </Button>
              </Field>
            </div>
          )}

          {metadata.callToAction && (
            <div className={styles.metadataSection}>
              <Field label="Call to Action">
                <Input value={metadata.callToAction} readOnly />
              </Field>
            </div>
          )}
        </Card>
      )}
    </div>
  );
};
