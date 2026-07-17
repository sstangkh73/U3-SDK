# รายงานฉบับเต็ม Phase 5: Population, Economy และ Persistence Foundation

วันที่ทดสอบ: 18 กรกฎาคม 2026
สาขา: `codex/world-travel-mvp`
โลก: `u3-connected-world`
สถานะ: **ผ่าน automated data-foundation gate; live gameplay integration ยังไม่เสร็จ**

## 1. บทสรุปผู้บริหาร

Phase 5 เพิ่ม deterministic population states, global simulation policy, cell ownership migration, journaled persistence transaction, crash recovery และ logical nav-border link contracts บน schema ของ California2 + Limestone

ผล final integration:

- validation errors: **0**
- warnings: **2** — population ยังเป็น data-only และ nav links ยังไม่ bake เข้า NavMesh
- population activation cycles: **12**
- deterministic population states: **219**
- active/persistent count ทุก cycle: **219/219**
- growth หลัง cycle แรก: **0**
- duplicate entity IDs: **0**
- cross-zone owner migration: **ผ่าน**
- save/reload entity count: **ผ่าน**
- save/reload owner zone/cell: **ผ่าน**
- prepared-journal crash recovery: **ผ่าน**
- deterministic nav-border contracts: **106**, cross-zone **1**
- runtime-baked nav links: **0**
- full EditMode suite: **1,354/1,354 ผ่าน**

ผลนี้พิสูจน์ state ownership และ persistence semantics แต่ไม่พิสูจน์ live zombies/animals/vehicles, dropped-item physics, buildable GameObjects, actual Unturned save integration หรือ AI เดินข้าม border

## 2. Global simulation policy

เพิ่ม `Builds/WorldUpgrade/WorldSimulationPolicy.json` เป็น policy ใหม่ที่ไม่ import legacy per-map gameplay override โดยอัตโนมัติ

population limits ต่อ cell:

| Kind | Max deterministic states/cell |
|---|---:|
| Item/DroppedItem | 2 |
| Zombie | 2 |
| Animal | 1 |
| Vehicle | 1 |
| Global persistent population cap | 10,000 |

survival/economy baseline:

- food/water/virus drain multiplier = 1.0
- item respawn ticks = 600
- vehicle respawn ticks = 3,600
- loot abundance multiplier = 1.0

policy validator ปฏิเสธ schema version, negative caps, zero multipliers และ invalid respawn intervals

## 3. WorldPopulationService

service อ่าน spawn records ใน cell schema แล้วเลือก deterministic seeds ตาม entity ID และ per-kind cap

mapping:

| Source record | Persistent state kind |
|---|---|
| `ItemSpawn` | `DroppedItem` |
| `ZombieSpawn` | `Zombie` |
| `AnimalSpawn` | `Animal` |
| `VehicleSpawn` | `Vehicle` |

dynamic ID สร้างจาก `population|<source-entity-id>` ด้วย SHA-256-derived ID ทำให้ activate cell เดิมซ้ำได้ state เดิม ไม่สร้าง instance ใหม่

service แยกสองแนวคิด:

- active residency: cell ใดกำลังใช้งาน state
- persistent identity: state ที่ยังอยู่แม้ cell unload

ดังนั้น deactivate ไม่ลบ persistent state และ reactivate ใช้ ID เดิม

## 4. Cell ownership และ migration

`WorldEntityOwnershipResolver`:

- resolve zone จาก world bounds
- เลือก owner cell ที่ครอบหรืออยู่ใกล้ตำแหน่งที่สุดแบบ deterministic
- อัปเดต zone ID, owner cell ID, world position และ revision

integration สร้าง buildable state ใน California2 แล้วเคลื่อนไป Limestone ผลหลัง save/reload ยังได้ Limestone zone/cell เดิม

## 5. Persistence store และ snapshot

`WorldPersistenceStore`:

- enforce unique entity ID
- add/upsert/lookup
- snapshot เรียง entity IDs
- canonical float serialization แบบ invariant `R` format
- SHA-256 content fingerprint
- restore ปฏิเสธ fingerprint mismatch และ duplicate IDs

final recovered snapshot fingerprint:

```text
08c44f745a438757f3806f249d608312feedf8885e717e6325e02a56ace96b67
```

## 6. Journaled transaction และ recovery

`WorldPersistenceTransaction` ใช้ไฟล์สี่บทบาท:

```text
world-save.json
world-save.json.pending
world-save.json.journal
world-save.json.backup
```

ลำดับ save:

1. serialize snapshot ไป pending
2. flush writer และ `FileStream.Flush(true)`
3. เขียน journal พร้อม payload SHA-256 และ sequence
4. atomic replace/move pending → target
5. ลบ backup และ journal หลัง commit

recovery test จงใจหยุดหลัง `Prepare` ก่อน commit แล้วสร้าง transaction object ใหม่:

- journal ถูกพบ
- pending hash ตรง
- commit ถูกทำต่อ
- snapshot โหลดและ restore ได้ครบ
- entity count หลัง recovery ตรงกับก่อน crash

test นี้จำลอง process interruption ในระดับ file transaction แต่ไม่ได้ตัดไฟหรือ kill OS ระหว่าง disk flush จริง

## 7. Nav-border contract

`WorldNavBorderLinkBuilder` สร้าง stable logical links:

- cardinal adjacency ภายใน zone
- transition link California2 → Limestone หนึ่งรายการ
- link ID deterministic
- `IsRuntimeBaked=false` ชัดเจน

ผล:

| Metric | Count |
|---|---:|
| Logical nav-border links | 106 |
| Cross-zone links | 1 |
| Runtime-baked links | 0 |

นี่คือ ownership/connectivity contract สำหรับงานต่อ ไม่ใช่หลักฐานว่า AI เดินข้ามได้

## 8. Integration protocol

1. โหลด policy และ validate
2. เปิด repositories ของ 68 cells ในสองโซน
3. activate population seeds ทุก cell
4. deactivate/unload แล้วทำซ้ำ 12 cycles
5. ตรวจ created/persistent/duplicate/growth
6. เพิ่ม dropped item และ buildable states
7. migrate buildable California2 → Limestone
8. save + reload snapshot
9. เพิ่ม recovery state และ prepare transaction โดยไม่ commit
10. recover จาก journal ด้วย transaction instance ใหม่
11.ตรวจ count/owner/fingerprint
12. สร้าง logical nav links และ cross-zone contract

machine-readable output: `Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-5-population-persistence-evidence.json`

## 9. Population results

population-only หลัง cycle แรก: **219** และคงที่ถึง cycle 12

final recovered state kinds หลังเพิ่ม validation states 3 รายการ:

| Kind | Count |
|---|---:|
| DroppedItem | 88 |
| Zombie | 72 |
| Animal | 30 |
| Vehicle | 31 |
| Buildable | 1 |
| **รวม** | **222** |

สามรายการที่เพิ่มหลัง population seed คือ dropped item ทดสอบ, buildable ที่ migrate และ recovery item

## 10. Gate assessment

| เกณฑ์ | ผล | ขอบเขต |
|---|---|---|
| entity ไม่ duplicate หลัง unload/reload | ผ่านระดับ state | 12 cycles, growth 0, duplicate 0 |
| save/reload count ตรง | ผ่าน | fingerprint-validated snapshot |
| save/reload owner cell/position | ผ่าน | cross-zone buildable state |
| population ไม่เพิ่มตามการข้าม/activation | ผ่าน | 219 ทุก cycle |
| journal/recovery | ผ่าน deterministic crash simulation | ยังไม่ใช่ power-loss test |
| dropped item/vehicle/buildable persistence schema | ผ่านระดับ data | live GameObjects ยังไม่ผูก |
| global survival/economy policy | ผ่าน config + validation | gameplay systems ยังไม่ consume policy |
| nav border links | ผ่าน logical contract | runtime-baked 0; AI gate ยังไม่ผ่าน |
| full regression | ผ่าน | 1,354/1,354 |
| code/report บน GitHub | รอ metadata commit | อัปเดตหลัง push |

## 11. ข้อจำกัด

- population service ยังไม่สร้างหรือควบคุม live game entities
- ไม่มี despawn/respawn timers ที่เดินตาม game clock
- ไม่มี item pickup, zombie death, animal AI หรือ vehicle damage mutation handlers
- buildable persistence ยังไม่มี structure graph/ownership permissions
- global survival/economy policy ยังไม่ hook Provider/game mode systems
- transaction ยังไม่มี slot rotation, cloud sync, schema migration หรือ file-lock coordination หลาย process
- journal recovery ผ่าน process-level simulation แต่ยังไม่มี fault injection ระหว่าง filesystem calls
- nav links ยังไม่ bake และไม่มี runtime pathfinding test
- ไม่มี server authority หรือ replication

## 12. งานถัดไป

Phase 6 จะเพิ่ม environment/presentation expansion foundation:

- environment profile schema และ deterministic blending
- per-cell performance budgets/HLOD contracts
- inventory/decode readiness สำหรับ PEI, Washington, Yukon, Russia และ Germany
- route-candidate validation โดยยังไม่ copy source assets

## 13. ข้อสรุป

Phase 5 ผ่าน automated data-foundation gate สำหรับ deterministic population identity, cell ownership, save/reload และ journal recovery โดยยังไม่อ้างว่า live population, AI navigation หรือ production saves พร้อมใช้งาน
