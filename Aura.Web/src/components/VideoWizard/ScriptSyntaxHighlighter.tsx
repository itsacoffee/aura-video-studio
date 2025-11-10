import { makeStyles, tokens, Text } from '@fluentui/react-components';
import type { FC } from 'react';

const useStyles = makeStyles({
  container: {
    fontFamily: 'Monaco, Consolas, "Courier New", monospace',
    fontSize: tokens.fontSizeBase300,
    lineHeight: '1.6',
    whiteSpace: 'pre-wrap',
    wordWrap: 'break-word',
  },
  scene: {
    marginBottom: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `4px solid ${tokens.colorBrandStroke1}`,
  },
  sceneNumber: {
    display: 'inline-block',
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    borderRadius: tokens.borderRadiusSmall,
    marginBottom: tokens.spacingVerticalS,
    fontWeight: tokens.fontWeightSemibold,
  },
  narration: {
    color: tokens.colorNeutralForeground1,
    marginBottom: tokens.spacingVerticalM,
  },
  visual: {
    color: tokens.colorPalettePurpleForeground2,
    fontStyle: 'italic',
    marginBottom: tokens.spacingVerticalS,
  },
  metadata: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalS,
  },
  duration: {
    display: 'inline-block',
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalXS}`,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    marginRight: tokens.spacingHorizontalS,
  },
  transition: {
    display: 'inline-block',
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalXS}`,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    color: tokens.colorPaletteYellowForeground2,
    borderRadius: tokens.borderRadiusSmall,
  },
  emphasis: {
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
  },
  pause: {
    color: tokens.colorPaletteDarkOrangeForeground1,
  },
});

interface ScriptScene {
  number: number;
  narration: string;
  visualPrompt?: string;
  durationSeconds?: number;
  transition?: string;
}

interface ScriptSyntaxHighlighterProps {
  script: string;
  scenes?: ScriptScene[];
}

export const ScriptSyntaxHighlighter: FC<ScriptSyntaxHighlighterProps> = ({ script, scenes }) => {
  const styles = useStyles();

  // If scenes are provided, use structured highlighting
  if (scenes && scenes.length > 0) {
    return (
      <div className={styles.container}>
        {scenes.map((scene) => (
          <div key={scene.number} className={styles.scene}>
            <div className={styles.sceneNumber}>Scene {scene.number}</div>
            
            <div className={styles.narration}>
              {highlightText(scene.narration, styles)}
            </div>
            
            {scene.visualPrompt && (
              <div className={styles.visual}>
                📹 Visual: {scene.visualPrompt}
              </div>
            )}
            
            <div className={styles.metadata}>
              {scene.durationSeconds && (
                <span className={styles.duration}>
                  ⏱️ {scene.durationSeconds.toFixed(1)}s
                </span>
              )}
              {scene.transition && (
                <span className={styles.transition}>
                  ↗️ {scene.transition}
                </span>
              )}
            </div>
          </div>
        ))}
      </div>
    );
  }

  // Fallback to simple text highlighting
  return (
    <div className={styles.container}>
      {highlightText(script, styles)}
    </div>
  );
};

function highlightText(text: string, styles: Record<string, string>) {
  const lines = text.split('\n');
  
  return lines.map((line, lineIndex) => {
    const parts: JSX.Element[] = [];
    let remainingText = line;
    let partIndex = 0;

    // Highlight emphasized text (e.g., *important*)
    const emphasisRegex = /\*([^*]+)\*/g;
    let lastIndex = 0;
    let match;

    while ((match = emphasisRegex.exec(line)) !== null) {
      // Add text before match
      if (match.index > lastIndex) {
        parts.push(
          <span key={`${lineIndex}-${partIndex++}`}>
            {line.substring(lastIndex, match.index)}
          </span>
        );
      }

      // Add emphasized text
      parts.push(
        <span key={`${lineIndex}-${partIndex++}`} className={styles.emphasis}>
          {match[1]}
        </span>
      );

      lastIndex = match.index + match[0].length;
    }

    // Add remaining text
    if (lastIndex < line.length) {
      remainingText = line.substring(lastIndex);
      
      // Highlight pauses (e.g., [pause])
      if (remainingText.includes('[pause]') || remainingText.includes('...')) {
        const pauseParts = remainingText.split(/(\[pause\]|\.\.\.)/);
        pauseParts.forEach((part, i) => {
          if (part === '[pause]' || part === '...') {
            parts.push(
              <span key={`${lineIndex}-${partIndex++}`} className={styles.pause}>
                {part}
              </span>
            );
          } else if (part) {
            parts.push(<span key={`${lineIndex}-${partIndex++}`}>{part}</span>);
          }
        });
      } else {
        parts.push(<span key={`${lineIndex}-${partIndex++}`}>{remainingText}</span>);
      }
    }

    return (
      <div key={lineIndex}>
        {parts.length > 0 ? parts : <span>{line}</span>}
        {'\n'}
      </div>
    );
  });
}
