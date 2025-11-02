import {
  makeStyles,
  tokens,
  Card,
  Button,
  Body1,
  Caption1,
  Spinner,
} from '@fluentui/react-components';
import { ArrowUploadRegular, DocumentAddRegular } from '@fluentui/react-icons';
import { useState, useRef, DragEvent, ChangeEvent } from 'react';

const useStyles = makeStyles({
  dropzone: {
    border: `2px dashed ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalXXL,
    textAlign: 'center',
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  dropzoneActive: {
    border: `2px dashed ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
    transform: 'scale(1.02)',
  },
  dropzoneDisabled: {
    opacity: 0.6,
    cursor: 'not-allowed',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalM,
  },
  icon: {
    fontSize: '48px',
    color: tokens.colorBrandForeground1,
  },
  fileInput: {
    display: 'none',
  },
  uploadingState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalXL,
  },
});

const ACCEPTED_VIDEO_FORMATS = ['.mp4', '.mov', '.avi', '.mkv', '.webm', '.flv', '.wmv', '.m4v'];

const ACCEPTED_AUDIO_FORMATS = ['.mp3', '.wav', '.aac', '.m4a', '.flac', '.ogg', '.wma'];

const ACCEPTED_FORMATS = [...ACCEPTED_VIDEO_FORMATS, ...ACCEPTED_AUDIO_FORMATS];

const MAX_FILE_SIZE = 100 * 1024 * 1024 * 1024; // 100 GB - increased for professional video workflows

interface FileDropzoneProps {
  onFileSelected: (file: File) => void;
  disabled?: boolean;
}

export function FileDropzone({ onFileSelected, disabled = false }: FileDropzoneProps) {
  const styles = useStyles();
  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const validateFile = (file: File): { valid: boolean; error?: string } => {
    const extension = '.' + file.name.split('.').pop()?.toLowerCase();

    if (!ACCEPTED_FORMATS.includes(extension)) {
      return {
        valid: false,
        error: `File type ${extension} is not supported. Please upload a video or audio file.`,
      };
    }

    if (file.size > MAX_FILE_SIZE) {
      const fileSizeGB = (file.size / (1024 * 1024 * 1024)).toFixed(2);
      const maxSizeGB = (MAX_FILE_SIZE / (1024 * 1024 * 1024)).toFixed(0);
      return {
        valid: false,
        error: `File size (${fileSizeGB} GB) exceeds ${maxSizeGB} GB limit. For files this large, consider:\n• Compressing the video using HandBrake or similar tools\n• Using a more efficient codec (H.265/HEVC instead of H.264)\n• Reducing resolution if source is higher than needed`,
      };
    }

    return { valid: true };
  };

  const handleFile = (file: File) => {
    if (disabled) return;

    const validation = validateFile(file);

    if (!validation.valid) {
      alert(validation.error);
      return;
    }

    setIsUploading(true);

    // Simulate file processing
    setTimeout(() => {
      onFileSelected(file);
      setIsUploading(false);
    }, 500);
  };

  const handleDragOver = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    if (!disabled) {
      setIsDragging(true);
    }
  };

  const handleDragLeave = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
  };

  const handleDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);

    if (disabled) return;

    const files = Array.from(e.dataTransfer.files);

    if (files.length > 0) {
      handleFile(files[0]);
    }
  };

  const handleFileInputChange = (e: ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (files && files.length > 0) {
      handleFile(files[0]);
    }
  };

  const handleClick = () => {
    if (!disabled && !isUploading && fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  if (isUploading) {
    return (
      <Card>
        <div className={styles.uploadingState}>
          <Spinner size="large" />
          <Body1>Processing file...</Body1>
          <Caption1>Analyzing file metadata and preparing for re-encoding</Caption1>
        </div>
      </Card>
    );
  }

  return (
    <div
      className={`${styles.dropzone} ${isDragging ? styles.dropzoneActive : ''} ${disabled ? styles.dropzoneDisabled : ''}`}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      onClick={handleClick}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          handleClick();
        }
      }}
      role="button"
      tabIndex={0}
    >
      <div className={styles.content}>
        {isDragging ? (
          <ArrowUploadRegular className={styles.icon} />
        ) : (
          <DocumentAddRegular className={styles.icon} />
        )}

        <Body1>{isDragging ? 'Drop file here' : 'Drag and drop a video or audio file here'}</Body1>

        <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
          or click to browse your computer
        </Caption1>

        <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
          Supported formats: MP4, MOV, AVI, MKV, WebM, MP3, WAV, AAC, FLAC
        </Caption1>

        <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
          Maximum file size: 100 GB
        </Caption1>

        <Button appearance="primary" disabled={disabled}>
          Browse Files
        </Button>

        <input
          ref={fileInputRef}
          type="file"
          accept={ACCEPTED_FORMATS.join(',')}
          onChange={handleFileInputChange}
          className={styles.fileInput}
          disabled={disabled}
        />
      </div>
    </div>
  );
}
