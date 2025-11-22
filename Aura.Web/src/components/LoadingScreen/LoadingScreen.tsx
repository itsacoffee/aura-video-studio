import { useEffect, useRef, useState } from 'react';
import type { FC } from 'react';
import './LoadingScreen.css';

interface Particle {
  x: number;
  y: number;
  size: number;
  speedY: number;
  speedX: number;
  opacity: number;
  color: string;
}

interface LoadingScreenProps {
  onComplete: () => void;
  message?: string;
  progress?: number;
  minDisplayTime?: number;
}

/**
 * Premium Loading Screen with Particle System
 * Features:
 * - Canvas-based particle animation (200+ particles)
 * - Animated gradient background
 * - Glowing logo with pulse effect
 * - Smooth fade transitions
 * - Performance optimized with requestAnimationFrame
 * - Accessibility: respects prefers-reduced-motion
 */
export const LoadingScreen: FC<LoadingScreenProps> = ({
  onComplete,
  message: externalMessage,
  progress: externalProgress,
  minDisplayTime: _minDisplayTime = 2500,
}) => {
  const [progress, setProgress] = useState(externalProgress ?? 0);
  const [stage, setStage] = useState<'loading' | 'complete' | 'fadeout'>('loading');
  const [statusMessage, setStatusMessage] = useState(
    externalMessage ?? 'Initializing Aura Video Studio'
  );

  const canvasRef = useRef<HTMLCanvasElement>(null);
  const particlesRef = useRef<Particle[]>([]);
  const animationFrameRef = useRef<number>();
  const prefersReducedMotion = useRef(
    window.matchMedia('(prefers-reduced-motion: reduce)').matches
  );

  // Initialize particles
  useEffect(() => {
    if (prefersReducedMotion.current) {
      return;
    }

    const particleCount = 200;
    const particles: Particle[] = [];
    const colors = ['#FF6B35', '#FF8960', '#6366F1', '#818CF8', '#3B82F6', '#60A5FA'];

    for (let i = 0; i < particleCount; i++) {
      particles.push({
        x: Math.random() * window.innerWidth,
        y: Math.random() * window.innerHeight + window.innerHeight,
        size: Math.random() * 3 + 1,
        speedY: -(Math.random() * 2 + 0.5),
        speedX: (Math.random() - 0.5) * 0.5,
        opacity: Math.random() * 0.5 + 0.3,
        color: colors[Math.floor(Math.random() * colors.length)] || colors[0],
      });
    }

    particlesRef.current = particles;
  }, []);

  // Animate particles
  useEffect(() => {
    if (prefersReducedMotion.current || !canvasRef.current) {
      return;
    }

    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const updateCanvasSize = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };

    updateCanvasSize();
    window.addEventListener('resize', updateCanvasSize);

    const animate = () => {
      if (!ctx) return;

      ctx.clearRect(0, 0, canvas.width, canvas.height);

      particlesRef.current.forEach((particle) => {
        // Update position
        particle.y += particle.speedY;
        particle.x += particle.speedX;

        // Reset particle if it goes off screen
        if (particle.y < -10) {
          particle.y = canvas.height + 10;
          particle.x = Math.random() * canvas.width;
        }

        // Draw particle
        ctx.beginPath();
        ctx.arc(particle.x, particle.y, particle.size, 0, Math.PI * 2);
        ctx.fillStyle = particle.color;
        ctx.globalAlpha = particle.opacity;
        ctx.fill();

        // Add glow
        ctx.shadowBlur = 10;
        ctx.shadowColor = particle.color;
        ctx.fill();
        ctx.shadowBlur = 0;
        ctx.globalAlpha = 1;
      });

      animationFrameRef.current = requestAnimationFrame(animate);
    };

    animate();

    return () => {
      window.removeEventListener('resize', updateCanvasSize);
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, []);

  // Progress simulation (if not controlled externally)
  useEffect(() => {
    if (externalProgress !== undefined) {
      setProgress(externalProgress);
      return;
    }

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
  }, [onComplete, externalProgress]);

  // Update message if provided externally
  useEffect(() => {
    if (externalMessage) {
      setStatusMessage(externalMessage);
    }
  }, [externalMessage]);

  // Complete when progress reaches 100
  useEffect(() => {
    if (progress >= 100 && stage === 'loading') {
      setStage('complete');
      setTimeout(() => {
        setStage('fadeout');
        setTimeout(onComplete, 600);
      }, 500);
    }
  }, [progress, stage, onComplete]);

  return (
    <div className={`loading-screen ${stage === 'fadeout' ? 'loading-screen--fadeout' : ''}`}>
      {/* Particle canvas */}
      {!prefersReducedMotion.current && (
        <canvas ref={canvasRef} className="loading-particles-canvas" />
      )}

      {/* Animated background gradient */}
      <div className="loading-background-gradient" />

      {/* Grid overlay */}
      <div className="loading-grid-overlay" />

      <div className="loading-content">
        {/* Logo with glow effect */}
        <div className="loading-logo">
          <div className="loading-logo-icon">
            <svg width="140" height="140" viewBox="0 0 120 120" fill="none">
              <defs>
                <linearGradient id="loadingGradient" x1="0%" y1="0%" x2="100%" y2="100%">
                  <stop offset="0%" stopColor="#ff6b35" />
                  <stop offset="50%" stopColor="#6366F1" />
                  <stop offset="100%" stopColor="#3b82f6" />
                </linearGradient>
                <linearGradient id="loadingGradientAnimated" x1="0%" y1="0%" x2="100%" y2="100%">
                  <stop offset="0%" stopColor="#ff6b35">
                    <animate
                      attributeName="stop-color"
                      values="#ff6b35;#3b82f6;#ff6b35"
                      dur="4s"
                      repeatCount="indefinite"
                    />
                  </stop>
                  <stop offset="50%" stopColor="#6366F1">
                    <animate
                      attributeName="stop-color"
                      values="#6366F1;#60a5fa;#6366F1"
                      dur="4s"
                      repeatCount="indefinite"
                    />
                  </stop>
                  <stop offset="100%" stopColor="#3b82f6">
                    <animate
                      attributeName="stop-color"
                      values="#3b82f6;#ff6b35;#3b82f6"
                      dur="4s"
                      repeatCount="indefinite"
                    />
                  </stop>
                </linearGradient>
                <filter id="loadingGlow">
                  <feGaussianBlur stdDeviation="8" result="coloredBlur" />
                  <feMerge>
                    <feMergeNode in="coloredBlur" />
                    <feMergeNode in="SourceGraphic" />
                  </feMerge>
                </filter>
              </defs>

              {/* Outer glow ring */}
              <circle
                cx="60"
                cy="60"
                r="58"
                fill="none"
                stroke="url(#loadingGradientAnimated)"
                strokeWidth="2"
                opacity="0.3"
                className="loading-logo-ring"
              />

              {/* Main icon shape */}
              <path
                d="M 60 20 L 90 50 L 90 70 L 60 90 L 30 70 L 30 50 Z"
                fill="url(#loadingGradientAnimated)"
                filter="url(#loadingGlow)"
                className="loading-logo-path"
              />
              <path
                d="M 45 55 L 60 45 L 75 55 L 75 65 L 60 75 L 45 65 Z"
                fill="#050505"
                className="loading-logo-cutout"
              />
            </svg>
          </div>

          <h1 className="loading-title">Aura</h1>
          <p className="loading-subtitle">Video Studio</p>
        </div>

        {/* Progress bar with gradient */}
        <div className="loading-progress">
          <div className="loading-progress-track">
            <div className="loading-progress-fill" style={{ width: `${progress}%` }}>
              <div className="loading-progress-shine" />
            </div>
          </div>
          <p className="loading-status">{statusMessage}</p>
        </div>

        {/* Version info */}
        <div className="loading-footer">
          <p className="loading-version">Version 1.0.0</p>
        </div>
      </div>
    </div>
  );
};
