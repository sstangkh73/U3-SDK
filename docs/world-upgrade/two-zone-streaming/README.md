# Two-zone Streaming Prototype

Phase 4 เชื่อม California2 และ Limestone ใน deterministic world layout ด้วย data-cell streamer และ generated collision bridge

## Components

- `WorldZoneResolver`: world bounds → zone + local coordinates
- `WorldCoordinateStrategy`: explicit zone-local/world conversion
- `WorldCellStreamer`: preload/unload hysteresis สำหรับหลาย zone repositories
- `WorldTransitionSafetyService`: corridor/preload/ground coverage และ collision bridge
- `WorldUpgradePhase4Validator`: real-schema probe simulation และ evidence writer

## Validate

```text
Unity.exe -batchmode -projectPath <repo> -executeMethod SDG.Unturned.WorldUpgrade.Editor.WorldUpgradePhase4Validator.GenerateFromCommandLine -logFile <log>
```

output:

```text
Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-4-two-zone-streaming-evidence.json
```

## Evidence boundary

prototype สร้าง terrain colliders ฝั่งละหนึ่ง, generated bridge และ stream cell-owned records สองโซน การจำลองไม่เรียก scene load/teleport และตรวจ 20 round trips แต่ยังไม่มี actual player/vehicle, nav streaming, full object GameObject activation, async I/O หรือ multiplayer
