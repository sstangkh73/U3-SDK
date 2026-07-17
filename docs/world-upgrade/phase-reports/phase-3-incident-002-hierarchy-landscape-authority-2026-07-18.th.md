# Incident Report Phase 3-002: Loose Landscape Files ถูกนับเป็น Active Tiles

วันที่: 18 กรกฎาคม 2026
สถานะ: แก้แล้ว
ผลกระทบ: Phase 2 schema รุ่นแรกมี landscape records เกิน runtime baseline 15 รายการ

## 1. Detection

Phase 3 runtime validation รอบแรกหลัง harness ทำงานได้รายงาน:

- California schema landscape records: 55
- source-backed records ที่ active baseline ไม่มี: 8
- records ที่ไม่มี heightmap และ baseline ไม่มี: 3
- active Landscape ที่เปรียบเทียบได้จริง: 44
- validation errors: 8

terrain ที่ active อยู่ 44 tiles ยังมี sample mismatches เป็นศูนย์ แต่ schema รุ่นแรกอาจทำให้ future streamer สร้าง loose source-only terrain เพิ่มจากเกมเดิม จึงไม่สามารถลด error เป็น warning แล้วผ่าน gate ได้โดยไม่แก้ importer

## 2. Root cause

Phase 1 landscape decoder สร้าง tile union จากไฟล์ใน:

- `Landscape/Heightmaps`
- `Landscape/Splatmaps`
- `Landscape/Holes`

แต่ Unturned runtime สร้าง active `LandscapeTile` ตามรายการ `Tiles` ของ `SDG.Framework.Landscapes.Landscape` ใน `Level.hierarchy` เท่านั้น Workshop maps มีไฟล์ค้างที่ไม่ได้ถูกอ้างใน hierarchy

## 3. Scope

| Zone | Loose source union | Active hierarchy | Source-only |
|---|---:|---:|---:|
| California2 | 55 | 44 | 11 |
| Limestone | 22 | 18 | 4 |
| **รวม** | **77** | **62** | **15** |

California source-only 11 รายการแบ่งเป็น 8 ที่มี heightmap และ 3 ที่มี splat/holes แต่ไม่มี heightmap

## 4. Corrective action

- decode `Level.hierarchy` ก่อน landscape file scan
- อ่าน exact active coordinates จาก Landscape item แรก ซึ่งตรงกับ runtime ownership
- สร้าง landscape entity seeds เฉพาะ hierarchy tiles
- ยัง inventory loose source files เพื่อ forensic visibility
- บันทึก `UnreferencedLandscapeSourceTile` warning ต่อ source-only tile
- เพิ่ม summary fields `HasHierarchyTileManifest`, `HierarchyTileCount`, `SourceOnlyTileCount`, `HierarchyTiles` และ `IsActiveInHierarchy`
- เพิ่ม regression test ที่มี heightmaps สอง tile แต่ hierarchy อ้างเพียงหนึ่ง tile

## 5. Schema correction result

หลัง regenerate:

- entities: 133,624 → 133,609
- cells: 79 → 68
- active landscape records: 77 → 62
- removed: 15 landscape records
- unchanged: 133,609 records
- added/moved/modified: 0/0/0
- duplicate IDs: 0
- validation errors: 0
- schema warnings: 177 quarantined legacy aliases
- repeated-build deterministic: true

stable IDs ของ entity ที่ยัง active ไม่เปลี่ยน การเปลี่ยน world/entity fingerprints เป็นผลที่คาดหมายจากการถอด source-only records

## 6. Final runtime verification

California2 final evidence:

- active landscape records 44
- missing baseline tiles 0
- default-height fallback 0
- height/splat/hole mismatches 0
- validation errors 0

## 7. Prevention

- hierarchy เป็น authority สำหรับ runtime component membership; loose files เป็น source evidence ไม่ใช่ active membership โดยอัตโนมัติ
- source-only files ต้องไม่กลายเป็น runtime entities จนกว่าจะมี explicit recovery/migration decision
- regression test ป้องกันการกลับไปใช้ directory union เป็น active tile set
