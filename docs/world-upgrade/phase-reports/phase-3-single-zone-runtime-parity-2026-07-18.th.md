# รายงานฉบับเต็ม Phase 3: Single-zone Runtime Parity

วันที่ทดสอบ: 18 กรกฎาคม 2026
สาขา: `codex/world-travel-mvp`
โลก: `u3-connected-world`
โซนที่ทดสอบ: `california-2` / map `California2`
สถานะ: **ผ่าน automated gate; ยังไม่ใช่ two-zone seamless runtime**

## 1. บทสรุปผู้บริหาร

Phase 3 เพิ่ม runtime path สำหรับอ่าน deterministic world schema จาก Phase 2, โหลด cell bundles แบบอิสระ, decode terrain source, สร้าง terrain/collider และ static-object preview, จัดการ object asset references และตรวจ parity กับ California2 ที่เกมเดิมโหลดจริง

ผล final runtime validation:

- validation errors: **0**
- validation warnings: **1** (`BlackSplatFallback` 1 pixel)
- cells ผ่าน load/unload: **49 cells × 5 รอบ**
- schema entities ในโซน: **101,710**
- active landscape tiles: **44**
- static objects: **81,392**
- terrain height samples เทียบ: **2,906,156**, mismatch **0**
- splat samples เทียบ: **23,068,672**, mismatch **0**
- hole samples เทียบ: **2,883,584**, mismatch **0**
- object GUID/position/rotation/scale mismatch: **0**
- final cell residency: **0 cells / 0 entities / 0 estimated bytes**
- asset registry: peak **1 asset / 1 reference**, final **0 / 0**
- full EditMode suite: **1,346/1,346 ผ่าน**

Phase นี้พิสูจน์ parity เชิงข้อมูลและ lifecycle ใน Unity Editor batch runtime แต่ยังไม่พิสูจน์ multiplayer, navigation, dynamic entities, actual player traversal session หรือการ stream สองโซนพร้อมกัน งานเหล่านี้อยู่ใน Phase 4–7

## 2. เป้าหมายและขอบเขต

เป้าหมายจากแผน:

1. เปิด California2 เป็น single-zone baseline
2. โหลด terrain, collision และ static-object records ผ่าน cell model
3. มี World Asset Registry และ reference counting
4. เปรียบเทียบ terrain/object identity และ transforms กับ runtime เดิม
5. โหลดและ unload cell ซ้ำโดยไม่มี logical residency leak

สิ่งที่ตั้งใจไม่รวมใน Phase 3:

- Limestone พร้อมกันใน runtime เดียว
- proximity streamer และ transition corridor
- origin shifting
- navmesh และ AI traversal
- persistence transaction
- dedicated-server หรือ multiplayer validation
- การ commit copyrighted Workshop map assets

## 3. Implementation ที่เพิ่ม

### 3.1 Cell bundle repository

`WorldCellBundleRepository` ทำหน้าที่:

- อ่าน `world-manifest.json` และ zone definition
- resolve relative path โดยป้องกัน path traversal
- โหลด cell JSON แบบ idempotent
- ตรวจ world/zone/cell/owner identity ก่อนรับข้อมูล
- unload ราย cell หรือทั้งหมด
- รายงาน loaded cell/entity counts และ estimated resident bytes

cell bundles ราย entity ยังถูกสร้างจาก source map ภายนอกและถูก ignore ใน Git เช่นเดิม

### 3.2 Terrain runtime decoder

`WorldRuntimeTerrainDecoder` รองรับ:

- heightmap 257×257, unsigned 16-bit big-endian
- splatmap 256×256×8
- holes bitset 256×256
- flat height fallback เมื่อไม่มี source
- layer 0 fallback เมื่อไม่มี splatmap
- layer 0 fallback เมื่อ source pixel มี material weight รวมเป็นศูนย์

หลังแก้ hierarchy importer ไม่มี active tile ใดต้องใช้ missing-height fallback ใน California2 final schema ส่วน black splat 1 pixel ยังใช้ explicit layer 0 fallback และถูกบันทึกเป็น warning

### 3.3 Runtime preview factory

`WorldRuntimePreviewFactory` สร้าง:

- Unity `Terrain` และ `TerrainCollider` จาก decoded payload
- static-object prefab จาก GUID
- position, rotation และ scale ตาม schema

smoke validation สร้าง terrain/collider และ object จริงที่ offset แยกจาก baseline แล้ว destroy ทั้งหมดหลังตรวจ

### 3.4 World Asset Registry

`WorldAssetRegistry` ใช้ GUID เป็น key และเก็บ logical reference count สำหรับ object assets ที่ cell runtime ถืออยู่

- acquire ซ้ำ GUID เดิมเพิ่ม reference count โดยไม่สร้าง registry entry ซ้ำ
- runtime object มี `WorldRuntimeAssetLease` เพื่อ release อัตโนมัติเมื่อ destroy
- entry ถูกถอดเมื่อ reference count เป็นศูนย์
- registry ไม่สั่ง unload global asset bundle เพราะ cache ดังกล่าวเป็น ownership ของ Unturned asset system ร่วมกับระบบอื่น

### 3.5 Parity validator และ command-line runner

`WorldSingleZoneParityValidator` ตรวจ schema กับ active `Landscape` และ `LevelObjects` ของ California2 ส่วน `WorldUpgradePhase3TestRunner` เปิด `GameStartup.unity`, ตั้ง auto-load เป็น Editor mode, รอ post-level-loaded event, เขียน evidence JSON และคืนค่า exit code ตาม gate

runtime bootstrap ทำงานเฉพาะเมื่อมี flag `-WorldUpgradePhase3Validation` จึงไม่เปลี่ยน behavior ปกติของเกมหรือ SDK

## 4. การแก้ schema ที่ค้นพบระหว่าง Phase 3

runtime รอบแรกพบว่า loose landscape source files บางไฟล์มีอยู่ แต่ไม่ได้อยู่ใน `Level.hierarchy` และเกมเดิมไม่สร้าง tile เหล่านั้น:

| Zone | Source-union records เดิม | Active hierarchy tiles | Source-only ที่ quarantine |
|---|---:|---:|---:|
| California2 | 55 | 44 | 11 |
| Limestone | 22 | 18 | 4 |
| **รวม** | **77** | **62** | **15** |

Importer ถูกแก้ให้ `Level.hierarchy` เป็น authoritative active-tile manifest และ loose source-only tiles เป็น diagnostic warnings ไม่ใช่ runtime entities การ regenerate schema จึงจำแนก landscape records เดิม 15 รายการเป็น `Removed`; entity ใช้งานจริงอีก 133,609 รายการเป็น `Unchanged` โดยไม่มี added, moved หรือ modified records

รายละเอียดอยู่ใน [Incident Phase 3-002](phase-3-incident-002-hierarchy-landscape-authority-2026-07-18.th.md)

## 5. Protocol การทดสอบ

### 5.1 Compile และ unit/regression tests

1. Unity batch compile หลังแก้แต่ละชุด
2. targeted tests ของ binary decoder, schema และ single-zone runtime
3. full EditMode suite หลัง implementation สุดท้าย

ผล targeted final: **27/27 ผ่าน**
ผล full final: **1,346/1,346 ผ่าน, failed 0, skipped 0**

### 5.2 Runtime integration

1. เปิด `Assets/GameStartup.unity`
2. auto-load `California2` ด้วย Editor mode เพื่อตัด dependency ต่อ Steam player session
3. รอ `Level.onPostLevelLoaded`
4. โหลด 49 schema cells ทั้งหมด แล้ว unload ทั้งหมด 5 รอบ
5. decode active terrain 44 tiles จาก source
6. เทียบ height/splat/hole arrays กับ active Landscape
7. resolve static objects 81,392 รายการด้วย source instance ID
8. เทียบ GUID และ transform กับ active LevelObjects
9. sample collider/renderer 4,096 objects
10. instantiate terrain/collider และ object จาก cell หนึ่งรายการ
11. destroy smoke objects, release asset lease และตรวจ final residency

หลักฐานเครื่องอ่านได้: `Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-3-single-zone-runtime-evidence.json`

## 6. ผล terrain parity

| Metric | ผล |
|---|---:|
| Active landscape records | 44 |
| Heightmap sources | 44 |
| Default-height fallbacks | 0 |
| Missing active baseline tiles | 0 |
| Height samples | 2,906,156 |
| Height mismatches | 0 |
| Splat samples | 23,068,672 |
| Splat mismatches | 0 |
| Hole samples | 2,883,584 |
| Hole mismatches | 0 |
| Missing terrain colliders | 0 |
| Black splat fallback pixels | 1 warning |

## 7. ผล static-object parity

| Metric | ผล |
|---|---:|
| Static-object records | 81,392 |
| Missing baseline objects | 0 |
| GUID mismatches | 0 |
| Transform unavailable | 0 |
| Transforms compared | 81,392 |
| Position mismatches | 0 |
| Rotation mismatches | 0 |
| Scale mismatches | 0 |
| Component sample size | 4,096 |
| Samples with collider | 4,094 |
| Samples with renderer | 4,089 |

จำนวน collider/renderer เป็น characterization ไม่ใช่เกณฑ์ว่า object ทุกชนิดต้องมี component ทั้งสองชนิด

## 8. ผล load/unload และ smoke runtime

แต่ละรอบโหลด 49 cells / 101,710 entities / estimated 82,061,308 bytes และกลับเป็น 0/0/0 หลัง unload ครบทั้ง 5 รอบ

smoke test ผ่าน:

- terrain สร้างได้
- TerrainCollider มี TerrainData
- object prefab สร้างได้
- smoke object มี collider 1 และ renderer 3
- terrain/object ถูก destroy
- Asset Registry peak 1 asset / 1 reference
- Asset Registry final 0 assets / 0 references

estimated bytes เป็น deterministic logical estimate ของ deserialized schema records ไม่ใช่ Unity Profiler native-memory measurement ดังนั้นผลนี้พิสูจน์ repository residency แต่ยังไม่ใช่ข้อสรุปว่า native heap ไม่มี fragmentation หรือ delayed GC

## 9. ปัญหาที่พบและการแก้

### 9.1 Validation harness ไม่สามารถเริ่ม baseline ได้อย่างถูกต้อง

พบสามอาการต่อเนื่อง: coroutine compile restriction, Editor เปิด backup scene และ single-player path ต้องใช้ player session ที่ไม่มีใน batch mode แก้ด้วย synchronous smoke cleanup, เปิด startup scene ชัดเจน, ใช้ชื่อ discovery `California2` และ auto-load Editor mode

รายละเอียด: [Incident Phase 3-001](phase-3-incident-001-runtime-validation-harness-2026-07-18.th.md)

### 9.2 Loose landscape source files ถูกนับเป็น active tiles

แก้โดยอ่าน authoritative tile list จาก `Level.hierarchy` ก่อน scan source files และ quarantine source-only data

รายละเอียด: [Incident Phase 3-002](phase-3-incident-002-hierarchy-landscape-authority-2026-07-18.th.md)

## 10. Gate assessment

| เกณฑ์ | ผล | หลักฐาน/ขอบเขต |
|---|---|---|
| เปิด California2 ด้วย runtime validation path | ผ่าน | Unity Editor batch runtime + post-level-loaded |
| terrain bounds/data ตรง baseline | ผ่าน | 28,858,412 samples รวม, mismatch 0 |
| collision ไม่หายในพื้นที่ smoke | ผ่านแบบ automated smoke | 44 baseline colliders ไม่หาย และสร้าง TerrainCollider ได้; ยังไม่มี manual walk session |
| static object identity/transforms ตรง | ผ่าน | 81,392/81,392 |
| cell load/unload ซ้ำ | ผ่าน | 5 รอบ final residency 0 |
| World Asset Registry/reference counting | ผ่าน | unit test + runtime peak 1, final 0 |
| full repository regression | ผ่าน | 1,346/1,346 |
| code/report บน GitHub | รอ metadata commit | จะอัปเดตหลัง push |

## 11. ข้อจำกัดที่ยังเหลือ

- automated smoke สร้าง terrain และ object ตัวแทน ไม่ได้ instantiate duplicate 81,392 objects ผ่าน runtime ใหม่พร้อมกัน
- object parity ทั้งหมดเทียบ schema กับ baseline LevelObjects; collider/renderer ตรวจแบบ sample 4,096 รายการ
- ไม่มี manual player-walk capture ใน batch environment
- ไม่มี Unity Profiler native-memory soak test; `EstimatedResidentBytes` เป็น logical estimate
- asset registry ไม่ unload global bundles เมื่อ reference เป็นศูนย์ เพราะ ownership อยู่ที่ Unturned asset cache
- terrain materials ใช้ active baseline TerrainLayers ใน smoke path; standalone packaging ของ material dependencies ยังไม่เสร็จ
- roads, foliage, environment volumes, navmesh, dynamic entities และ saves ยังไม่เป็น streamed cell records
- ยังไม่ทดสอบ Limestone ใน active runtime และยังไม่มี two-zone streaming

## 12. คำสั่งทำซ้ำ

สร้าง schema:

```text
Unity.exe -batchmode -projectPath <repo> -executeMethod SDG.Unturned.WorldUpgrade.Editor.WorldSchemaGenerator.GenerateFromCommandLine -logFile <log>
```

รัน Phase 3 runtime validation:

```text
Unity.exe -batchmode -projectPath <repo> -executeMethod SDG.Unturned.WorldUpgrade.Editor.WorldUpgradePhase3TestRunner.RunFromCommandLine -WorldUpgradePhase3Validation -logFile <log>
```

## 13. ข้อสรุปและงานถัดไป

Phase 3 ผ่าน automated gate สำหรับ California2 single-zone parity: schema active terrain ตรง baseline, static-object identity/transforms ตรง, cell และ asset references คืนสู่ศูนย์หลัง unload และ full regression ผ่านทั้งหมด

Phase 4 ต้องต่อยอดเป็น two-zone streaming prototype โดยใช้ California2 และ Limestone พร้อม `WorldCellStreamer`, `ZoneResolver`, transition safety และการวัด memory/frame-time จริง ภายใต้ข้อห้ามเดิมว่าอย่าเรียกระบบว่า seamless world จนกว่าจะผ่าน boundary traversal, nav, persistence และ multiplayer gates
