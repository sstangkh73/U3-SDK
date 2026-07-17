# รายงานฉบับเต็ม Phase 4: Two-zone Streaming Prototype

วันที่ทดสอบ: 18 กรกฎาคม 2026
สาขา: `codex/world-travel-mvp`
โลก: `u3-connected-world`
โซน: `california-2` และ `limestone`
สถานะ: **ผ่าน automated prototype gate; ยังไม่ผ่าน manual gameplay/production gate**

## 1. บทสรุปผู้บริหาร

Phase 4 เพิ่ม prototype สำหรับ resolve ตำแหน่งระหว่างสองโซน, stream cell bundles ด้วย preload/unload hysteresis, แปลง world ↔ zone-local coordinates และสร้าง transition collision bridge ระหว่าง California2 กับ Limestone โดยไม่เรียก scene load หรือ teleport API

final integration evidence:

- validation errors: **0**
- warnings: **2** — navigation data และ manual player traversal ยังไม่พร้อม
- round trips: **20**
- movement samples: **820** ที่ step สูงสุด **128 เมตร/update**
- zone-resolution changes: **40**
- corridor entries: **40**
- scene-handle changes: **0**
- teleport detections: **0**
- boundary preload misses: **0**
- logical ground-coverage misses: **0**
- physical raycast samples: **41**, misses **0**
- logical residency plateau: **ผ่านทุก 20 รอบ**
- final residency หลัง unload: **0 cells / 0 entities / 0 bytes**
- full EditMode suite: **1,349/1,349 ผ่าน**

หลักฐานนี้เป็น deterministic Editor prototype ไม่ใช่การบันทึกผู้เล่นหรือรถขับในเกมจริง จึงห้ามสรุปว่า player/vehicle seamless traversal, AI navigation หรือ production streaming พร้อมใช้งาน

## 2. Implementation

### 2.1 WorldZoneResolver

`WorldZoneResolver`:

- resolve zone ที่ครอบตำแหน่ง world-space
- ถ้าอยู่ในช่องว่างระหว่างโซน เลือก zone ที่มีระยะถึง bounds ใกล้ที่สุดแบบ deterministic
- คืน zone-local position จาก explicit layout offset
- tie-break ด้วย zone key เพื่อให้ผลซ้ำได้

### 2.2 Local-coordinate strategy

`WorldCoordinateStrategy` ใช้ manifest layout offset เป็นสัญญาหลัก:

```text
local = world - zone.LayoutOffset
world = local + zone.LayoutOffset
```

โลกปัจจุบันมีขอบเขตประมาณ 10 กิโลเมตร จึงยังไม่ต้อง origin shift ใน prototype นี้ แต่ entity/cell records ยังคง local coordinates ต่อ zone เพื่อไม่ผูก persistence กับ global float origin ระยะยาว

ผล world → local → world error สูงสุด: **0 เมตร** ใน 820 samples

### 2.3 WorldCellStreamer

`WorldCellStreamer`:

- รองรับหลาย `WorldCellBundleRepository`
- preload cell เมื่อระยะ XZ ถึง bounds ไม่เกิน 1,536 เมตร
- unload เมื่อเกิน 2,048 เมตร
- ใช้ hysteresis ลด load/unload thrashing
- ติดตาม cell/entity/estimated-byte residency
- ตรวจ active landscape coverage และ record counts แยกตาม zone/kind

streamer โหลด cell-owned records ทุกชนิดที่อยู่ใน JSON รวม static objects และ spawn records แต่ prototype physical activation สร้างเฉพาะ boundary terrain สอง tile และ transition bridge ไม่ได้ instantiate static-object GameObjects ทุก record

### 2.4 TransitionSafetyService

layout ปัจจุบันมีช่องว่าง X ระหว่าง:

- California2 boundary: `x=4096`
- Limestone boundary: `x=5120`
- raw gap: **1,024 เมตร**

transition span สุดท้าย:

- start: `x=3840`
- end: `x=5376`
- landing overlap: **256 เมตรต่อฝั่ง**
- centerline: `z=512` ซึ่งอยู่กลาง terrain cell
- width: **256 เมตร**

service สร้าง slope `BoxCollider` จาก sampled surface height ของ terrain ฝั่งต้นทางไปปลายทาง พร้อม overlap เพื่อครอบ terrain holes ใกล้ Limestone landing edge

## 3. Protocol การทดสอบ

### 3.1 Unit tests

เพิ่ม 3 tests:

1. zone resolution และ world/local round-trip
2. two-zone cell preload + final unload
3. transition collider bridge creation

### 3.2 Real-schema integration

1. อ่าน current world manifest และ cell bundles ของ California2/Limestone
2. สร้าง boundary terrain tile ฝั่งละหนึ่งจาก raw height/splat/hole source
3. sample surface height ที่ landing points
4. สร้าง transition bridge collider
5. raycast ต่อเนื่อง 64 เมตรต่อ sample ตลอด boundary terrain → bridge → boundary terrain
6. จำลอง probe ข้ามไปกลับ 20 รอบ ที่ movement step 128 เมตร
7. ทุก sample อัปเดต streamer, resolver, coordinate conversion, scene handle และ coverage
8. ตรวจ per-loop logical residency plateau
9. unload ทั้งหมดและตรวจ final residency

machine-readable output: `Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-4-two-zone-streaming-evidence.json`

## 4. ผล streaming

| Metric | ผล |
|---|---:|
| Round trips | 20 |
| Position samples | 820 |
| Corridor entries | 40 |
| Zone-resolution changes | 40 |
| Preload radius | 1,536 m |
| Unload radius | 2,048 m |
| Peak active cells | 14 |
| Peak active entities | 33,503 |
| Peak estimated resident bytes | 27,096,762 |
| Activations | 314 |
| Deactivations | 314 |
| Final cells/entities/bytes | 0 / 0 / 0 |
| Plateau | ผ่าน; loop 1–20 เท่ากัน |

static-object record visibility ระหว่าง run:

- California2 peak: 19,529 records
- Limestone peak: 24,080 records

ตัวเลขนี้ยืนยันว่า cell records ของทั้งสองโซนเข้าชุด active ระหว่าง traversal แต่ไม่ใช่จำนวน instantiated renderers/colliders

## 5. ผล continuity และ collision

| Metric | ผล |
|---|---:|
| Scene handle changes | 0 |
| Teleport detections | 0 |
| Preload misses ใน corridor | 0 |
| Logical ground misses | 0 |
| Physical collision samples | 41 |
| Physical collision misses | 0 |
| Source terrain collider | สร้างได้ |
| Destination terrain collider | สร้างได้ |
| Transition bridge collider | สร้างได้ |

`TeleportCount=0` หมายถึงระยะระหว่าง probe samples ไม่เกิน configured continuous step ไม่ได้หมายถึงเกมได้ hook ระบบ anti-cheat หรือ player movement แล้ว

## 6. Incident และการแก้

integration รุ่นแรกพบ physical misses 8/33 เพราะ centerline อยู่บน terrain tile seam `z=0` หลัง snap ไป cell center `z=512` เหลือ miss 2 จุดบน Limestone terrain holes ใกล้ landing edge การเพิ่ม landing overlap 256 เมตรต่อฝั่งและสร้าง bridge จาก landing surface จริงทำให้ final misses เป็น 0/41

รายละเอียด: [Incident Phase 4-001](phase-4-incident-001-transition-collision-gaps-2026-07-18.th.md)

## 7. Gate assessment

| เกณฑ์ | ผล | ขอบเขตหลักฐาน |
|---|---|---|
| สองโซนอยู่ใน world layout เดียว | ผ่าน | manifest + resolver |
| ไม่มี scene reload ระหว่างข้าม | ผ่านใน deterministic probe | scene handle คงเดิม 820 samples; ยังไม่มี player session |
| ไม่มี teleport | ผ่านใน deterministic probe | step continuity 128 m, detections 0 |
| destination preloaded ก่อน/ระหว่าง corridor | ผ่าน | misses 0/40 entries |
| collision ไม่หายตรง transition | ผ่าน automated physical prototype | raycast 41/41; ยังไม่มี vehicle-speed gameplay capture |
| ไปกลับแล้ว residency คงที่ | ผ่าน | 20/20 loops plateau, final 0 |
| object records ของทั้งสอง zone ถูก stream | ผ่านระดับ data | มี static records ทั้งสองฝั่ง; GameObject activation ทั้งชุดยังไม่ทำ |
| navigation stream | ยังไม่ผ่าน | schema ไม่มี nav records; warning ชัดเจน |
| manual player/vehicle traversal | ยังไม่ผ่าน | deterministic probe เท่านั้น |
| full regression | ผ่าน | 1,349/1,349 |
| code/report บน GitHub | รอ metadata commit | อัปเดตหลัง push |

## 8. ข้อจำกัด

- ยังไม่มี runtime player controller ผูกกับ streamer
- ยังไม่มี vehicle-speed test ใน gameplay scene
- boundary terrain สร้างเฉพาะ tile ตัวแทนฝั่งละหนึ่ง
- transition bridge เป็น geometry ทดสอบ ไม่ใช่รอยต่อศิลป์หรือ level design สุดท้าย
- static-object records ถูก preload แต่ยังไม่ instantiate/unload GameObjects ทั้ง cell ใน Phase 4 integration
- ไม่มี navigation records, nav border links หรือ AI crossing
- estimated bytes เป็น logical schema estimate ไม่ใช่ Unity native-memory profile
- ไม่มี frame-time budget, async I/O, job scheduling หรือ cancellation
- ไม่มี multiplayer authority/replication

## 9. งานถัดไป

Phase 5 จะเพิ่ม population และ persistence foundation โดยใช้ cell ownership ที่ Phase 2–4 พิสูจน์แล้ว:

- deterministic population ownership
- unload/reload โดยไม่ duplicate
- journaled save transaction และ recovery
- dynamic entity owner-cell migration
- nav border contract เป็น warning/blocker จนกว่าจะมี decoder/runtime data จริง

## 10. ข้อสรุป

Phase 4 ผ่าน automated two-zone prototype gate สำหรับ resolver, coordinate strategy, cell-data streaming, transition collider continuity และ repeat traversal residency แต่ยังไม่ผ่าน manual gameplay, navigation หรือ production performance gate รายงานจึงคงคำว่า “prototype” และไม่ใช้คำว่า seamless world แบบสมบูรณ์
