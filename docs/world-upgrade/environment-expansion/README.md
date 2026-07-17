# Phase 6 Environment and Expansion Foundation

โฟลเดอร์/ระบบนี้เพิ่ม contract สำหรับ environment profile, deterministic transition blending, per-cell performance budgets และ readiness gate ของแมพที่จะเพิ่มในอนาคต

## Generated artifacts

- `Builds/WorldUpgrade/EnvironmentProfiles.json` — source-derived `Lighting.dat` v12 profiles ของ 7 แมพ
- `Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-6-environment-expansion-evidence.json` — validation, budget และ expansion-gate evidence

## Run validation

```powershell
& 'C:\Program Files\Unity 2022.3.62f3\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath 'C:\Unturned\work\U3-SDK' `
  -executeMethod SDG.Unturned.WorldUpgrade.Editor.WorldUpgradePhase6Validator.GenerateFromCommandLine `
  -logFile 'C:\Unturned\work\U3-SDK\Library\WorldUpgrade\phase6-generate.log'
```

## Claim boundary

ระบบนี้พิสูจน์ exact binary decode, numeric blend continuity และ deterministic record-budget audit เท่านั้น ยังไม่สร้าง HLOD/occlusion, ไม่ profile บน target hardware และไม่รวม 5 แมพ official เข้า runtime จนกว่า authored route, single-zone parity และ performance gate ของแต่ละแมพจะผ่าน
