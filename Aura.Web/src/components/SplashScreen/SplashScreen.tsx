import { useEffect, useRef, useState } from 'react';
import type { FC } from 'react';
import './SplashScreen.css';

interface Particle {
  x: number;
  y: number;
  size: number;
  speedY: number;
  speedX: number;
  opacity: number;
  color: string;
}

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
  const [statusMessage, setStatusMessage] = useState('Starting backend server...');

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

    const particleCount = 300;
    const particles: Particle[] = [];
    // Cosmic colors: blues, purples, and oranges for the ethereal effect
    const colors = ['#6366F1', '#818CF8', '#3B82F6', '#60A5FA', '#A855F7', '#C084FC', '#FF6B35', '#FF8960'];

    for (let i = 0; i < particleCount; i++) {
      particles.push({
        x: Math.random() * window.innerWidth,
        y: Math.random() * window.innerHeight,
        size: Math.random() * 2 + 0.5,
        speedY: (Math.random() - 0.5) * 0.3,
        speedX: (Math.random() - 0.5) * 0.3,
        opacity: Math.random() * 0.6 + 0.2,
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
        particle.y += particle.speedY;
        particle.x += particle.speedX;

        // Wrap around edges
        if (particle.y < -10) {
          particle.y = canvas.height + 10;
        } else if (particle.y > canvas.height + 10) {
          particle.y = -10;
        }
        if (particle.x < -10) {
          particle.x = canvas.width + 10;
        } else if (particle.x > canvas.width + 10) {
          particle.x = -10;
        }

        ctx.beginPath();
        ctx.arc(particle.x, particle.y, particle.size, 0, Math.PI * 2);
        ctx.fillStyle = particle.color;
        ctx.globalAlpha = particle.opacity;
        ctx.shadowBlur = 8;
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

  useEffect(() => {
    const stages = [
      { progress: 20, message: 'Starting backend server...', delay: 300 },
      { progress: 40, message: 'Initializing services...', delay: 400 },
      { progress: 60, message: 'Loading modules...', delay: 350 },
      { progress: 80, message: 'Preparing workspace...', delay: 300 },
      { progress: 100, message: 'Almost ready...', delay: 400 },
    ];

    let currentStage = 0;
    let timeoutId: number | null = null;

    const advanceStage = () => {
      if (currentStage < stages.length) {
        const { progress: newProgress, message, delay } = stages[currentStage];
        setProgress(newProgress);
        setStatusMessage(message);
        currentStage++;
        // Schedule next stage with its specific delay
        timeoutId = window.setTimeout(advanceStage, delay);
      } else {
        setStage('complete');
        setTimeout(() => {
          setStage('fadeout');
          setTimeout(onComplete, 600);
        }, 500);
      }
    };

    // Start with the first stage's delay
    timeoutId = window.setTimeout(advanceStage, stages[0]?.delay || 300);

    return () => {
      if (timeoutId !== null) {
        clearTimeout(timeoutId);
      }
    };
  }, [onComplete]);

  return (
    <div className={`splash-screen ${stage === 'fadeout' ? 'splash-screen--fadeout' : ''}`}>
      {/* Particle canvas */}
      {!prefersReducedMotion.current && (
        <canvas ref={canvasRef} className="splash-particles-canvas" />
      )}

      {/* Animated background gradient */}
      <div className="splash-background-gradient" />

      {/* Animated grid overlay */}
      <div className="splash-grid-overlay" />

      <div className="splash-content">
        {/* Logo with film clapperboard icon */}
        <div className="splash-logo">
          <div className="splash-logo-icon">
            <svg width="120" height="120" viewBox="0 0 120 120" fill="none">
              <defs>
                <linearGradient id="flameGradientBlue" x1="0%" y1="0%" x2="0%" y2="100%">
                  <stop offset="0%" stopColor="#3B82F6" stopOpacity="0.9" />
                  <stop offset="50%" stopColor="#6366F1" stopOpacity="0.6" />
                  <stop offset="100%" stopColor="#818CF8" stopOpacity="0.3" />
                </linearGradient>
                <linearGradient id="flameGradientOrange" x1="0%" y1="0%" x2="0%" y2="100%">
                  <stop offset="0%" stopColor="#FF6B35" stopOpacity="0.9" />
                  <stop offset="50%" stopColor="#FF8960" stopOpacity="0.6" />
                  <stop offset="100%" stopColor="#FFA07A" stopOpacity="0.3" />
                </linearGradient>
                <filter id="glow">
                  <feGaussianBlur stdDeviation="4" result="coloredBlur" />
                  <feMerge>
                    <feMergeNode in="coloredBlur" />
                    <feMergeNode in="SourceGraphic" />
                  </feMerge>
                </filter>
              </defs>

              {/* Flames from top - Blue flames */}
              <g className="splash-flames-blue">
                <path
                  d="M 30 20 Q 25 10, 20 15 Q 25 5, 30 10 Q 35 5, 40 15 Q 35 10, 30 20 Z"
                  fill="url(#flameGradientBlue)"
                  filter="url(#glow)"
                  opacity="0.8"
                />
                <path
                  d="M 60 20 Q 55 10, 50 15 Q 55 5, 60 10 Q 65 5, 70 15 Q 65 10, 60 20 Z"
                  fill="url(#flameGradientBlue)"
                  filter="url(#glow)"
                  opacity="0.9"
                />
                <path
                  d="M 90 20 Q 85 10, 80 15 Q 85 5, 90 10 Q 95 5, 100 15 Q 95 10, 90 20 Z"
                  fill="url(#flameGradientBlue)"
                  filter="url(#glow)"
                  opacity="0.8"
                />
              </g>

              {/* Flames from top - Orange flames */}
              <g className="splash-flames-orange">
                <path
                  d="M 35 22 Q 30 12, 25 17 Q 30 7, 35 12 Q 40 7, 45 17 Q 40 12, 35 22 Z"
                  fill="url(#flameGradientOrange)"
                  filter="url(#glow)"
                  opacity="0.7"
                />
                <path
                  d="M 65 22 Q 60 12, 55 17 Q 60 7, 65 12 Q 70 7, 75 17 Q 70 12, 65 22 Z"
                  fill="url(#flameGradientOrange)"
                  filter="url(#glow)"
                  opacity="0.8"
                />
                <path
                  d="M 95 22 Q 90 12, 85 17 Q 90 7, 95 12 Q 100 7, 105 17 Q 100 12, 95 22 Z"
                  fill="url(#flameGradientOrange)"
                  filter="url(#glow)"
                  opacity="0.7"
                />
              </g>

              {/* Film Clapperboard */}
              <g className="splash-clapperboard">
                {/* Main board */}
                <rect
                  x="20"
                  y="30"
                  width="80"
                  height="70"
                  rx="4"
                  fill="#4A5568"
                  stroke="#2D3748"
                  strokeWidth="2"
                />
                {/* Top bar */}
                <rect
                  x="20"
                  y="30"
                  width="80"
                  height="20"
                  rx="4"
                  fill="#2D3748"
                />
                {/* White stripes */}
                <rect x="25" y="35" width="70" height="3" fill="#E2E8F0" />
                <rect x="25" y="42" width="70" height="3" fill="#E2E8F0" />
                {/* Hinge circles */}
                <circle cx="30" cy="40" r="3" fill="#1A202C" />
                <circle cx="90" cy="40" r="3" fill="#1A202C" />
                {/* Slate text area */}
                <rect
                  x="30"
                  y="55"
                  width="60"
                  height="40"
                  rx="2"
                  fill="#1A202C"
                />
                {/* Slate lines */}
                <line x1="35" y1="70" x2="85" y2="70" stroke="#4A5568" strokeWidth="1" />
                <line x1="35" y1="80" x2="85" y2="80" stroke="#4A5568" strokeWidth="1" />
              </g>
            </svg>
          </div>

          <h1 className="splash-title">Aura</h1>
          <p className="splash-subtitle">AI VIDEO GENERATION SUITE</p>
        </div>

        {/* Progress bar */}
        <div className="splash-progress">
          <p className="splash-status">{statusMessage}</p>
          <div className="splash-progress-track">
            <div className="splash-progress-fill" style={{ width: `${progress}%` }} />
          </div>
        </div>
      </div>
    </div>
  );
};
