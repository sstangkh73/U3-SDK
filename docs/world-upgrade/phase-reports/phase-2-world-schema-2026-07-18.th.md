# รายงานผลระยะ 2: World Schema และ Stable Identity

**วันที่ทดสอบ:** 18 กรกฎาคม 2026  
**Branch:** `codex/world-travel-mvp`  
**ผล gate:** ผ่านและเผยแพร่บน GitHub แล้ว  
**ฐานจากระยะก่อน:** Phase 1 commit `b9c5c19`  
**Phase 2 commit:** [`03cdfee`](https://github.com/sstangkh73/U3-SDK/commit/03cdfee)  
**Draft PR:** [sstangkh73/U3-SDK#1](https://github.com/sstangkh73/U3-SDK/pull/1)  

## 1. วัตถุประสงค์

Phase 2 สร้างสัญญาข้อมูลระหว่าง raw map importer กับ runtime โลกใหม่ ก่อนเริ่ม terrain/object streaming เพื่อป้องกันการผูก persistence กับ active map, array index หรือ instance ที่สร้างใหม่ทุกครั้ง

เกณฑ์หลักจากแผน:

- มี `WorldManifest`, `ZoneDefinition`, `WorldCell` และ schema versioning
- มี stable world/zone/cell/entity IDs
- มี migration table สำหรับ asset GUID/legacy ID
- มี explicit layout ของ California 2 และ Limestone
- static objects, spawn records และ landscape tiles มี owner cell
- generate จาก source เดิมซ้ำแล้ว ID ไม่เปลี่ยน
- duplicate/collision และ zone overlap ถูกตรวจล่วงหน้า
- เปรียบเทียบ source revision แล้วจำแนก entity เพิ่ม ลบ ย้าย หรือแก้ได้

## 2. สิ่งที่ implement

### 2.1 Runtime-neutral schema

เพิ่ม serializable data model:

- `WorldLayoutSourceData`
- `WorldManifestData`
- `WorldZoneDefinitionData`
- `WorldCellData`
- `WorldEntityRecordData`
- `WorldAssetMigrationData`
- `WorldSchemaValidationSummaryData`
- `WorldSchemaDiffData`

schema ปัจจุบันเป็น version 1 และ generator version 1 ไม่มี timestamp ใน canonical fingerprint เพื่อให้ output identity ไม่เปลี่ยนเพราะเวลารัน

### 2.2 Detailed records จาก decoder

Phase 1 summary decoder ยังให้ output เดิม แต่ภายใน Editor สามารถส่ง entity seeds เพิ่มเติมสำหรับ:

- landscape tiles
- static objects
- item spawn points
- zombie spawn points
- animal spawn points
- vehicle spawn points
- player spawn points

record เก็บ source key, local position, rotation/scale ที่เกี่ยวข้อง, source instance ID, table index, legacy asset ID และ GUID โดยไม่คัดลอก source binary เข้า repository

### 2.3 Stable ID rules

ใช้ SHA-256 จาก namespace + canonical key แล้วเก็บ 128 bits แรกพร้อม type prefix:

- `wld_...`: world key
- `zon_...`: world ID + zone key
- `cel_...`: zone ID + integer grid coordinates
- `ent_...`: zone ID + record kind + source key
- `mig_...`: zone ID + migration key

object format ใหม่ใช้ source instance ID เป็น key หลัก ส่วน format เก่าที่ไม่มี instance ID ใช้ region/index source key จุด spawn ใช้ file + region/index หรือ point index และ landscape ใช้ tile coordinates

### 2.4 Owner cells

cell size คือ 1,024 world units ตรงกับ landscape tile size ใช้ mathematical floor ทั้งแกน X/Z ทำให้พิกัดติดลบถูกจัด cell ถูกต้อง

ทุก entity record มี:

- `OwnerCellId`
- local position
- world position หลังบวก layout offset
- content fingerprint แยกจาก stable entity ID

การแยก identity กับ content ทำให้ entity เดิมที่ย้ายตำแหน่งยังถูกจำแนกเป็น `Moved`/`Modified` แทนการกลายเป็น removed + added โดยไม่จำเป็น

### 2.5 Explicit two-zone layout

`Builds/WorldUpgrade/WorldLayout.json` กำหนด:

| Zone | Offset X | World X bounds | Cells | ผล overlap |
|---|---:|---:|---:|---|
| California 2 | 0 | -4,096 ถึง 4,096 | 57 | ไม่ overlap |
| Limestone | 7,168 | 5,120 ถึง 10,240 | 22 | ไม่ overlap |

ระหว่าง bounds มีช่องว่าง 1,024 หน่วยหรือหนึ่ง cell ช่องว่างนี้ยังไม่ใช่ playable connector; Phase 4 ต้องออกแบบ transition cells/streaming boundary

### 2.6 Migration table

GUID เป็น primary asset identity ส่วน legacy ID เป็น compatibility alias migration มีสถานะ:

- `Unambiguous`
- `AmbiguousGuidPrimary`
- `GuidOnly`

ambiguous alias ถูกตรวจแต่ห้าม legacy-only resolution เพื่อไม่เลือก Workshop asset ผิดแบบเงียบ

### 2.7 Source update diff

ก่อนเขียน schema ใหม่ generator โหลด cell bundles ของ revision ก่อนหน้าและเปรียบเทียบด้วย stable entity ID:

- `Added`
- `Removed`
- `Moved` — owner cell เปลี่ยน
- `Modified` — content fingerprint เปลี่ยนแต่ owner cell เดิม
- `Unchanged`

### 2.8 Output boundary

commit เฉพาะ manifest, generation evidence, source diff และ zone indices ส่วน cell bundle ราย entity 79 ไฟล์มีขนาดประมาณ 131 MiB จึงอยู่ใน `.gitignore` และสร้างใหม่จาก source ภายนอกได้

ไม่มี absolute source path และไม่มี raw Workshop content ใน generated schema ที่ commit

## 3. Automated tests

เพิ่ม Phase 2 EditMode test cases 11 รายการ ครอบคลุม:

1. stable ID ซ้ำได้และ namespace แยก zone
2. cell ownership ที่พิกัด 0
3. ขอบบวกก่อน 1,024
4. ขอบบวกที่ 1,024
5. พิกัดลบใกล้ศูนย์
6. ขอบลบที่ -1,024
7. พิกัดต่ำกว่า -1,024
8. repeated build รักษา world/zone/cell/entity IDs และ fingerprints
9. เปลี่ยน zone layout แล้ว identity เดิมคงอยู่แต่ content fingerprint เปลี่ยน
10. duplicate entity ID ถูก reject และ ambiguous migration ถูก quarantine
11. diff จำแนก added/removed/moved/modified/unchanged

ผลสุดท้าย:

| ชุดทดสอบ | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Phase 2 tests | 11 | 0 | 0 |
| EditMode suite ทั้งหมด | 1,339 | 0 | 0 |

Unity compile ผ่านด้วย Tundra build success บน Unity `2022.3.62f3`

## 4. Integration validation

รันกับ California 2 และ Limestone จริง:

| Zone | Cells | Entities | Landscape | Objects | Item | Zombie | Animal | Vehicle | Player |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| California 2 | 57 | 101,721 | 55 | 81,392 | 13,491 | 5,918 | 522 | 283 | 60 |
| Limestone | 22 | 31,903 | 22 | 26,714 | 3,638 | 1,242 | 157 | 111 | 19 |
| **รวม** | **79** | **133,624** | **77** | **108,106** | **17,129** | **7,160** | **679** | **394** | **79** |

ผล validator:

| รายการ | ผล |
|---|---:|
| Validation errors | 0 |
| Duplicate stable IDs | 0 |
| Owner cell mismatches | 0 |
| Zone overlaps | 0 |
| Migration entries | 4,559 |
| Warnings | 180 |

warnings แบ่งเป็น ambiguous legacy alias groups 177 กลุ่มและ California orphan landscape cells 3 cell ทั้งสองประเภทถูกบันทึกพร้อม fail-safe policy

## 5. Determinism evidence

generator build schema สองครั้งใน process เดียวกันและได้ค่าเดียวกัน:

```text
World ID:
wld_7ef79abf4110d9747c72eb3eac927f56

Manifest content fingerprint:
8999396661f43effde76db1b6d480ffa14cf99ca13cbe5ba5e037de0d3d763c5

Entity identity fingerprint:
5b60f445ffc219ca757a0b59f153c7116066122a47a23331b9d98525230ae007
```

หลัง generate ซ้ำจาก source เดิม:

| Diff class | Count |
|---|---:|
| Added | 0 |
| Removed | 0 |
| Moved | 0 |
| Modified | 0 |
| Unchanged | 133,624 |

## 6. ปัญหาที่พบและการแก้

integration รอบแรก deterministic แต่ล้ม gate ด้วย `MigrationAliasCollision` 177 errors เพราะ Workshop content ใช้ legacy object IDs ซ้ำกับหลาย GUID

การแก้คือปรับ schema ให้สะท้อน runtime source-of-truth: GUID เป็น primary, legacy ID เป็น fallback เฉพาะกลุ่ม unambiguous และ alias ที่กำกวมถูก quarantine ผลรอบถัดไป error เป็นศูนย์

รายละเอียดเต็มอยู่ที่ [Incident Report Phase 2-001](phase-2-incident-001-legacy-alias-collisions-2026-07-18.th.md)

## 7. ข้อจำกัดที่ยังเหลือ

- cell bundles ยังเป็น Editor-generated JSON ไม่ใช่ runtime binary/Addressable package
- hierarchy items ยังมีเพียง type inventory จาก Phase 1 ไม่ได้มี transform/owner cell records
- roads ยังเป็น zone-level summary ไม่ได้แบ่ง joints/segments ตาม owner cell
- environment volumes, foliage, navmesh และ map images ยังไม่อยู่ใน schema
- spawn point stable key ใช้ source ordering; การ insert record กลาง legacy array อาจทำให้ record หลังจุด insert เปลี่ยน identity
- object format เก่าที่ไม่มี instance ID ใช้ region/index fallback และมีข้อจำกัดเดียวกัน
- orphan landscape cells 3 จุดยังไม่มี visual/collider fallback จนถึง Phase 3
- explicit layout ยังมีช่องว่าง ไม่ใช่ seamless route
- cell bundles ต้อง regenerate บนเครื่องที่มี source maps และไม่ถูก commit เพราะขนาดใหญ่

## 8. เกณฑ์ผ่าน

| เกณฑ์ | ผล |
|---|---|
| WorldManifest/ZoneDefinition/WorldCell + schema version | ผ่าน |
| Stable world/zone/cell/entity IDs | ผ่าน |
| Asset GUID/legacy migration table | ผ่าน พร้อม quarantine policy |
| California 2 + Limestone layout | ผ่าน; overlap 0 |
| Static/dynamic owner cells | ผ่านสำหรับ landscape/object/spawn 133,624 records |
| Generate ซ้ำ IDs ไม่เปลี่ยน | ผ่าน 133,624/133,624 |
| Duplicate/collision detection | ผ่าน; duplicate 0, alias collision 177 กลุ่มถูก quarantine |
| Source update classification | ผ่าน automated test และ unchanged integration rerun |
| Unity compile | ผ่าน |
| EditMode tests | ผ่าน 1,339/1,339 |
| Code/report อยู่บน GitHub | ผ่าน — commit `03cdfee`, Draft PR #1 |

## 9. ข้อสรุป

Phase 2 ผ่าน gate และเผยแพร่บน GitHub แล้ว โลกใหม่มี identity และ ownership contract ที่ deterministic สำหรับ California 2 กับ Limestone โดยยังไม่กล่าวอ้างว่า runtime streaming ทำงาน ขั้นถัดไปคือ Phase 3: ใช้ cell schema โหลด California 2 แบบ single-zone ให้ terrain, collision และ static objects เทียบกับ baseline เดิม พร้อมกำหนด fallback สำหรับ orphan heightmaps และ black splat pixels ก่อนเริ่ม two-zone streaming
