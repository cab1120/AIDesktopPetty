# Sakura Weather Benchmark

Unity: 2021.3
Render Pipeline: URP 12.1.10

## Test Environment

Resolution: 1920*1080

VSync: Every V Blank

Target Frame Rate:

Graphics API:

Quality Level:

Scene: 

Camera:

Profile:

## Baseline

| Case | CPU Median | GPU Median | Batches | SetPass | Triangles | Vertices | BG Alive | MID Alive | FG Alive |
|------|-----------:|-----------:|--------:|--------:|----------:|---------:|---------:|----------:|---------:|
| A No VFX | 7.09ms | 3.46ms | 9 | 7 | 434 | 310 | 0 | 0 | 0 |
| B BG Only | | | 10 | 8 | 434 | 310 | 795 | 0 | 0 |
| C MID Only | | | 11 | 9 | 57.78k | 80.18k | 0 | 316 | 0 |
| D FG Only | | | 11 | 9 | 7.60k | 10.29k| 0 | 0 | 17 |
| E Full | 7.38ms | 4.31ms | 14 | 12 | 64.95k | 90.17k | | | |

## Spikes

CPU:约 12~15ms 约每68帧出现一次

GPU:约 8~11ms 约每68帧出现一次

## Frame Debugger

### Background
Simulation Compute:有

Transparent Color Pass:1

Depth Pass:无

Shadow Pass:无

### Midground
Simulation Compute:有

Depth Pass:1

Opaque Color Pass:<Unnamed Pass 4> (UniversalForwardOnly)

Shadow Pass:无

### Foreground
Simulation Compute:有

Depth Pass:1

Opaque Color Pass:<Unnamed Pass 4> (UniversalForwardOnly)

Shadow Pass:无

## Notes
