# Phase 7 Multiplayer Authority and Production Readiness

ระบบนี้เป็น deterministic server-authority foundation สำหรับ connected-world schema:

- server-owned player zone/cell position
- cell-scoped replication snapshots
- revision-checked pickup/drop transactions
- request replay protection scoped per client
- reconnect token และ server-state restoration
- explicit rejection ของ client save/travel commits
- protocol/schema compatibility contract
- hashed production-input manifest

## Generated evidence

- `Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-7-multiplayer-production-evidence.json`
- `Builds/WorldUpgrade/ProductionPackageManifest.json`

## Run validation

```powershell
& 'C:\Program Files\Unity 2022.3.62f3\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath 'C:\Unturned\work\U3-SDK' `
  -executeMethod SDG.Unturned.WorldUpgrade.Editor.WorldUpgradePhase7Validator.GenerateFromCommandLine `
  -logFile 'C:\Unturned\work\U3-SDK\Library\WorldUpgrade\phase7-generate.log'
```

## Claim boundary

การทดสอบ 64 clients เป็น in-process deterministic model ไม่มี network transport, latency, packet loss, process isolation หรือ dedicated-server executable จึงใช้พิสูจน์ authority semantics และ anti-replay invariants เท่านั้น ไม่ใช่ production multiplayer certification
