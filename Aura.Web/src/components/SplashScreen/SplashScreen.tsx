import { useEffect, useState } from 'react';
import type { FC } from 'react';
import './SplashScreen.css';

interface SplashScreenProps {
  onComplete: () => void;
  minDisplayTime?: number;
}

export const SplashScreen: FC<SplashScreenProps> = ({
  onComplete,
  minDisplayTime: _minDisplayTime = 2500,
}) => {
  const [progress, setProgress] = useState(0);
  const [stage, setStage] = useState<'loading' | 'complete' | 'fadeout'>('loading');
  const [statusMessage, setStatusMessage] = useState('Initializing Aura Video Studio');

  useEffect(() => {
    const stages = [
      { progress: 20, message: 'Loading workspace', delay: 300 },
      { progress: 40, message: 'Initializing video engine', delay: 400 },
      { progress: 60, message: 'Loading effects library', delay: 350 },
      { progress: 80, message: 'Preparing timeline', delay: 300 },
      { progress: 100, message: 'Ready to create', delay: 400 },
    ];

    let currentStage = 0;
    const progressInterval = setInterval(() => {
      if (currentStage < stages.length) {
        const { progress: newProgress, message } = stages[currentStage];
        setProgress(newProgress);
        setStatusMessage(message);
        currentStage++;
      } else {
        clearInterval(progressInterval);
        setStage('complete');
        setTimeout(() => {
          setStage('fadeout');
          setTimeout(onComplete, 600);
        }, 500);
      }
    }, stages[currentStage]?.delay || 300);

    return () => clearInterval(progressInterval);
  }, [onComplete]);

  return (
    <div className={`splash-screen ${stage === 'fadeout' ? 'splash-screen--fadeout' : ''}`}>
      <div className="splash-content">
        {/* Logo with gradient animation */}
        <div className="splash-logo">
          <div className="splash-logo-icon">
            <svg width="120" height="120" viewBox="0 0 120 120" fill="none">
              <defs>
                <linearGradient id="auraGradient" x1="0%" y1="0%" x2="100%" y2="100%">
                  <stop offset="0%" stopColor="#3b82f6" />
                  <stop offset="50%" stopColor="#a855f7" />
                  <stop offset="100%" stopColor="#ec4899" />
                </linearGradient>
                <filter id="glow">
                  <feGaussianBlur stdDeviation="4" result="coloredBlur" />
                  <feMerge>
                    <feMergeNode in="coloredBlur" />
                    <feMergeNode in="SourceGraphic" />
                  </feMerge>
                </filter>
              </defs>

              {/* Stylized "A" for Aura - video play button inspired */}
              <path
                d="M 60 20 L 90 50 L 90 70 L 60 90 L 30 70 L 30 50 Z"
                fill="url(#auraGradient)"
                filter="url(#glow)"
                className="splash-logo-path"
              />
              <path
                d="M 45 55 L 60 45 L 75 55 L 75 65 L 60 75 L 45 65 Z"
                fill="#0a0a0a"
                className="splash-logo-cutout"
              />
            </svg>
          </div>

          <h1 className="splash-title">Aura</h1>
          <p className="splash-subtitle">Video Studio</p>
        </div>

        {/* Progress bar */}
        <div className="splash-progress">
          <div className="splash-progress-track">
            <div className="splash-progress-fill" style={{ width: `${progress}%` }} />
          </div>
          <p className="splash-status">{statusMessage}</p>
        </div>

        {/* Version info */}
        <div className="splash-footer">
          <p className="splash-version">Version 1.0.0</p>
        </div>
      </div>

      {/* Animated particles background */}
      <div className="splash-particles">
        {Array.from({ length: 20 }).map((_, i) => (
          <div
            key={i}
            className="splash-particle"
            style={{
              left: `${Math.random() * 100}%`,
              animationDelay: `${Math.random() * 3}s`,
              animationDuration: `${3 + Math.random() * 4}s`,
            }}
          />
        ))}
      </div>
    </div>
  );
};
