# Population and Persistence Foundation

Phase 5 เพิ่ม deterministic dynamic-state ownership และ journaled persistence บน world/cell schema

## Components

- `WorldSimulationPolicyData`: population/survival/economy policy validation
- `WorldPopulationService`: deterministic spawn-state activation
- `WorldPersistenceStore`: unique entity state store และ fingerprinted snapshots
- `WorldEntityOwnershipResolver`: world position → zone/cell migration
- `WorldPersistenceTransaction`: prepare/commit/recover journal workflow
- `WorldNavBorderLinkBuilder`: deterministic logical adjacency contracts
- `WorldUpgradePhase5Validator`: real-schema cycles/save/recovery evidence

## Validate

```text
Unity.exe -batchmode -projectPath <repo> -executeMethod SDG.Unturned.WorldUpgrade.Editor.WorldUpgradePhase5Validator.GenerateFromCommandLine -logFile <log>
```

output:

```text
Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-5-population-persistence-evidence.json
```

validation saves ใช้ `Library/WorldUpgrade/Phase5Validation` และไม่ถูก commit

## Evidence boundary

service ปัจจุบันจัดการ data states ไม่ใช่ live GameObjects; nav links เป็น logical contracts และ `IsRuntimeBaked=false`
