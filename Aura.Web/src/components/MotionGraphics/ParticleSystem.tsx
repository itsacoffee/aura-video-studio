/**
 * Particle System Component
 * Generate and render various particle effects
 */

import { makeStyles, tokens, Button, Label, Card, Slider } from '@fluentui/react-components';
import {
  Sparkle24Regular,
  WeatherSnow24Regular,
  Fire24Regular,
  Play24Regular,
  Stop24Regular,
  Checkmark24Regular,
} from '@fluentui/react-icons';
import { useState, useRef, useEffect, useCallback } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  presets: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(120px, 1fr))',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
  },
  presetButton: {
    height: '80px',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  canvas: {
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  controls: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  controlRow: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'space-between',
  },
});

export type ParticleEffectType = 'confetti' | 'snow' | 'rain' | 'sparkles' | 'fire' | 'smoke';

interface Particle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  size: number;
  color: string;
  life: number;
  maxLife: number;
  rotation?: number;
  rotationSpeed?: number;
}

interface ParticleSystemConfig {
  type: ParticleEffectType;
  emissionRate: number;
  particleLife: number;
  particleSize: number;
  gravity: number;
  spread: number;
  velocity: number;
  colors: string[];
}

interface ParticleSystemProps {
  onSystemCreated?: (config: ParticleSystemConfig) => void;
  canvasWidth?: number;
  canvasHeight?: number;
}

const PRESET_CONFIGS: Record<ParticleEffectType, Partial<ParticleSystemConfig>> = {
  confetti: {
    emissionRate: 50,
    particleLife: 3,
    particleSize: 8,
    gravity: 200,
    spread: 180,
    velocity: 400,
    colors: ['#ff6b6b', '#4ecdc4', '#45b7d1', '#f9ca24', '#6c5ce7', '#a29bfe'],
  },
  snow: {
    emissionRate: 30,
    particleLife: 5,
    particleSize: 5,
    gravity: 30,
    spread: 90,
    velocity: 50,
    colors: ['#ffffff', '#e3f2fd', '#f0f4ff'],
  },
  rain: {
    emissionRate: 100,
    particleLife: 2,
    particleSize: 3,
    gravity: 800,
    spread: 20,
    velocity: 600,
    colors: ['#64b5f6', '#42a5f5', '#1e88e5'],
  },
  sparkles: {
    emissionRate: 20,
    particleLife: 1.5,
    particleSize: 4,
    gravity: 0,
    spread: 360,
    velocity: 100,
    colors: ['#ffd700', '#ffed4e', '#fff176', '#ffff8d'],
  },
  fire: {
    emissionRate: 60,
    particleLife: 2,
    particleSize: 10,
    gravity: -150,
    spread: 45,
    velocity: 200,
    colors: ['#ff6b35', '#ff8e53', '#f7931e', '#fdc830', '#ff4b1f'],
  },
  smoke: {
    emissionRate: 20,
    particleLife: 4,
    particleSize: 20,
    gravity: -50,
    spread: 60,
    velocity: 50,
    colors: ['#90a4ae', '#78909c', '#607d8b', '#546e7a'],
  },
};

export function ParticleSystem({
  onSystemCreated,
  canvasWidth = 800,
  canvasHeight = 600,
}: ParticleSystemProps) {
  const styles = useStyles();
  const [selectedType, setSelectedType] = useState<ParticleEffectType>('confetti');
  const [isPlaying, setIsPlaying] = useState(false);
  const [particles, setParticles] = useState<Particle[]>([]);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const animationRef = useRef<number>();
  const lastTimeRef = useRef<number>(0);

  // Configuration
  const [config, setConfig] = useState<ParticleSystemConfig>({
    type: 'confetti',
    ...PRESET_CONFIGS.confetti,
  } as ParticleSystemConfig);

  const createParticle = useCallback((): Particle => {
    const spreadAngle = (Math.random() - 0.5) * config.spread * (Math.PI / 180);
    const angle = -Math.PI / 2 + spreadAngle; // Default upward direction
    const speed = config.velocity * (0.5 + Math.random() * 0.5);

    return {
      x: canvasWidth / 2,
      y: canvasHeight - 50,
      vx: Math.cos(angle) * speed,
      vy: Math.sin(angle) * speed,
      size: config.particleSize * (0.5 + Math.random() * 0.5),
      color: config.colors[Math.floor(Math.random() * config.colors.length)],
      life: config.particleLife,
      maxLife: config.particleLife,
      rotation: Math.random() * 360,
      rotationSpeed: (Math.random() - 0.5) * 360,
    };
  }, [config, canvasWidth, canvasHeight]);

  const updateParticles = useCallback(
    (deltaTime: number) => {
      // Emit new particles
      const particlesToEmit = Math.floor(config.emissionRate * deltaTime);
      const newParticles: Particle[] = [];

      for (let i = 0; i < particlesToEmit; i++) {
        newParticles.push(createParticle());
      }

      // Update existing particles
      const updatedParticles = [...particles, ...newParticles]
        .map((particle) => {
          particle.life -= deltaTime;

          if (particle.life <= 0) {
            return null;
          }

          // Apply velocity
          particle.x += particle.vx * deltaTime;
          particle.y += particle.vy * deltaTime;

          // Apply gravity
          particle.vy += config.gravity * deltaTime;

          // Update rotation
          if (particle.rotation !== undefined && particle.rotationSpeed !== undefined) {
            particle.rotation += particle.rotationSpeed * deltaTime;
          }

          return particle;
        })
        .filter((p) => p !== null) as Particle[];

      setParticles(updatedParticles);
    },
    [config, particles, createParticle]
  );

  const drawParticles = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Draw particles
    particles.forEach((particle) => {
      const opacity = particle.life / particle.maxLife;

      ctx.save();
      ctx.globalAlpha = opacity;

      if (config.type === 'confetti' && particle.rotation !== undefined) {
        ctx.translate(particle.x, particle.y);
        ctx.rotate((particle.rotation * Math.PI) / 180);
        ctx.fillStyle = particle.color;
        ctx.fillRect(-particle.size / 2, -particle.size / 2, particle.size, particle.size);
      } else if (config.type === 'rain') {
        ctx.strokeStyle = particle.color;
        ctx.lineWidth = particle.size;
        ctx.beginPath();
        ctx.moveTo(particle.x, particle.y);
        ctx.lineTo(particle.x, particle.y + particle.size * 3);
        ctx.stroke();
      } else if (config.type === 'fire' || config.type === 'smoke') {
        const gradient = ctx.createRadialGradient(
          particle.x,
          particle.y,
          0,
          particle.x,
          particle.y,
          particle.size
        );
        gradient.addColorStop(0, particle.color);
        gradient.addColorStop(1, 'transparent');
        ctx.fillStyle = gradient;
        ctx.beginPath();
        ctx.arc(particle.x, particle.y, particle.size, 0, 2 * Math.PI);
        ctx.fill();
      } else {
        ctx.fillStyle = particle.color;
        ctx.beginPath();
        ctx.arc(particle.x, particle.y, particle.size, 0, 2 * Math.PI);
        ctx.fill();
      }

      ctx.restore();
    });
  }, [particles, config]);

  const animate = useCallback(() => {
    const now = performance.now();
    const deltaTime = (now - lastTimeRef.current) / 1000;
    lastTimeRef.current = now;

    updateParticles(deltaTime);
    drawParticles();

    animationRef.current = requestAnimationFrame(animate);
  }, [updateParticles, drawParticles]);

  useEffect(() => {
    if (isPlaying) {
      lastTimeRef.current = performance.now();
      animate();
    } else {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
      }
    }

    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
      }
    };
  }, [isPlaying, animate]);

  const handlePresetSelect = (type: ParticleEffectType) => {
    setSelectedType(type);
    setConfig({
      type,
      ...PRESET_CONFIGS[type],
    } as ParticleSystemConfig);
    setParticles([]);
  };

  const handleTogglePlay = () => {
    setIsPlaying(!isPlaying);
  };

  const handleCreate = () => {
    onSystemCreated?.(config);
    setIsPlaying(false);
    setParticles([]);
  };

  return (
    <div className={styles.container}>
      <Card>
        <div className={styles.header}>
          <Label weight="semibold">Particle System Presets</Label>
          <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
            <Button
              appearance={isPlaying ? 'secondary' : 'primary'}
              icon={isPlaying ? <Stop24Regular /> : <Play24Regular />}
              onClick={handleTogglePlay}
            >
              {isPlaying ? 'Stop' : 'Preview'}
            </Button>
            <Button appearance="primary" icon={<Checkmark24Regular />} onClick={handleCreate}>
              Create
            </Button>
          </div>
        </div>

        <div className={styles.presets}>
          <Button
            className={styles.presetButton}
            appearance={selectedType === 'confetti' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('confetti')}
          >
            <Sparkle24Regular />
            Confetti
          </Button>
          <Button
            className={styles.presetButton}
            appearance={selectedType === 'snow' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('snow')}
          >
            <WeatherSnow24Regular />
            Snow
          </Button>
          <Button
            className={styles.presetButton}
            appearance={selectedType === 'rain' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('rain')}
          >
            <WeatherSnow24Regular />
            Rain
          </Button>
          <Button
            className={styles.presetButton}
            appearance={selectedType === 'sparkles' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('sparkles')}
          >
            <Sparkle24Regular />
            Sparkles
          </Button>
          <Button
            className={styles.presetButton}
            appearance={selectedType === 'fire' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('fire')}
          >
            <Fire24Regular />
            Fire
          </Button>
          <Button
            className={styles.presetButton}
            appearance={selectedType === 'smoke' ? 'primary' : 'secondary'}
            onClick={() => handlePresetSelect('smoke')}
          >
            <WeatherSnow24Regular />
            Smoke
          </Button>
        </div>
      </Card>

      <canvas ref={canvasRef} width={canvasWidth} height={canvasHeight} className={styles.canvas} />

      <Card>
        <div className={styles.controls}>
          <div className={styles.controlRow}>
            <Label>Emission Rate: {config.emissionRate}</Label>
            <Slider
              min={10}
              max={200}
              value={config.emissionRate}
              onChange={(_, data) => setConfig({ ...config, emissionRate: data.value })}
            />
          </div>

          <div className={styles.controlRow}>
            <Label>Particle Life: {config.particleLife}s</Label>
            <Slider
              min={0.5}
              max={10}
              step={0.5}
              value={config.particleLife}
              onChange={(_, data) => setConfig({ ...config, particleLife: data.value })}
            />
          </div>

          <div className={styles.controlRow}>
            <Label>Particle Size: {config.particleSize}</Label>
            <Slider
              min={2}
              max={30}
              value={config.particleSize}
              onChange={(_, data) => setConfig({ ...config, particleSize: data.value })}
            />
          </div>

          <div className={styles.controlRow}>
            <Label>Velocity: {config.velocity}</Label>
            <Slider
              min={50}
              max={1000}
              step={10}
              value={config.velocity}
              onChange={(_, data) => setConfig({ ...config, velocity: data.value })}
            />
          </div>

          <div className={styles.controlRow}>
            <Label>Gravity: {config.gravity}</Label>
            <Slider
              min={-300}
              max={1000}
              step={10}
              value={config.gravity}
              onChange={(_, data) => setConfig({ ...config, gravity: data.value })}
            />
          </div>
        </div>
      </Card>
    </div>
  );
}
