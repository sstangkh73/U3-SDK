# Single-zone Runtime Parity

Phase 3 ใช้ California2 เป็น baseline เพื่อทดสอบ deterministic cell schema ก่อนสร้าง two-zone streamer

## Runtime components

- `WorldCellBundleRepository`: โหลด/ตรวจ/unload world-cell JSON และรายงาน logical residency
- `WorldRuntimeTerrainDecoder`: decode height, splat และ holes พร้อม explicit fallbacks
- `WorldRuntimePreviewFactory`: สร้าง Terrain/TerrainCollider และ static-object prefab
- `WorldAssetRegistry`: logical GUID reference counts สำหรับ streamed object assets
- `WorldRuntimeAssetLease`: release reference เมื่อ runtime object ถูก destroy
- `WorldSingleZoneParityValidator`: เทียบ schema กับ active Landscape/LevelObjects
- `WorldUpgradePhase3ValidationBootstrap`: opt-in runtime evidence writer

## Run

```text
Unity.exe -batchmode -projectPath <repo> -executeMethod SDG.Unturned.WorldUpgrade.Editor.WorldUpgradePhase3TestRunner.RunFromCommandLine -WorldUpgradePhase3Validation -logFile <log>
```

output:

```text
Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-3-single-zone-runtime-evidence.json
```

## Landscape authority

`Level.hierarchy` เป็น authority ว่า landscape coordinate ใด active ส่วน loose height/splat/hole files ที่ไม่มีใน hierarchy ถูก inventory และเตือน แต่ไม่สร้าง runtime entity

## Evidence boundary

evidence ปัจจุบันพิสูจน์ data parity, representative instantiation, repository lifecycle และ asset-reference lifecycle ใน Unity Editor batch runtime ไม่ได้พิสูจน์ manual player traversal, native-memory soak, navigation, two-zone streaming หรือ multiplayer
