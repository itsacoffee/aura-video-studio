import { usePlaybackStore } from "@/stores/playback-store";
import { Button } from "../ui/button";
import { Label } from "../ui/label";
import { Slider } from "../ui/slider";

const SPEED_PRESETS = [
  { label: "0.25x", value: 0.25 },
  { label: "0.5x", value: 0.5 },
  { label: "1x", value: 1.0 },
  { label: "2x", value: 2.0 },
  { label: "4x", value: 4.0 },
  { label: "8x", value: 8.0 },
];

export function SpeedControl() {
  const { speed, setSpeed } = usePlaybackStore();

  return (
    <div className="space-y-4">
      <h3 className="text-sm font-medium">Playback Speed</h3>
      <div className="space-y-4">
        <div className="flex gap-2">
          {SPEED_PRESETS.map((preset) => (
            <Button
              key={preset.value}
              variant={speed === preset.value ? "default" : "outline"}
              className="flex-1"
              onClick={() => setSpeed(preset.value)}
            >
              {preset.label}
            </Button>
          ))}
        </div>
        <div className="space-y-1">
          <Label>Custom ({speed.toFixed(1)}x)</Label>
          <Slider
            value={[speed]}
            min={0.1}
            max={8.0}
            step={0.1}
            onValueChange={(value) => setSpeed(value[0])}
            className="mt-2"
          />
        </div>
      </div>
    </div>
  );
}
