# รายงานฉบับเต็ม Phase 6: Environment, Presentation และ Expansion Foundation

วันที่ทดสอบ: 18 กรกฎาคม 2026  
สาขา: `codex/world-travel-mvp`  
โลก: `u3-connected-world`  
สถานะ: **ผ่าน automated environment/readiness foundation; production expansion gate ยังปิด**

## 1. บทสรุปผู้บริหาร

Phase 6 เพิ่มตัวถอด `Environment/Lighting.dat` แบบ strict, schema โปรไฟล์สภาพแวดล้อม, deterministic smooth blending, per-cell performance budget audit และ readiness gate สำหรับ PEI, Washington, Yukon, Russia และ Germany โดยอ่านจากไฟล์แผนที่ที่ติดตั้งจริงและไม่คัดลอก source assets เข้า repository

ผล final integration:

- validation errors: **0**
- warnings: **5**
- source maps ที่มี exact environment profile: **7/7**
- `Lighting.dat` version/size: **v12 / 268 bytes ทุกแมพ**
- blend pairs: **21**
- time profiles ต่อคู่: **4**
- blend samples: **21,588**
- maximum adjacent normalized delta: **0.005859256**
- endpoint exact: **ผ่าน**
- abrupt numeric transition detected: **0**
- current runtime zones ที่ audit: **2**
- expansion candidates ที่ผ่าน inventory/decode/environment readiness: **5/5**
- expansion candidates ที่พร้อมรวม runtime: **0/5**
- full EditMode suite: **1,358/1,358 ผ่าน**

ผลนี้พิสูจน์ว่าโปรไฟล์แสง/หมอก/สีท้องฟ้า/weather capability และระดับน้ำ/หิมะถูกอ่านจาก source ได้ครบและ blend ต่อเนื่องใน data layer แต่ไม่พิสูจน์ rendering จริง, audio ambience, oxygen gameplay, HLOD/occlusion, target-hardware performance หรือ parity ของห้าแมพใหม่

## 2. Source-derived environment profile

ตัวถอดใหม่อ่านรูปแบบเดียวกับ `LevelLighting`:

| ส่วน | จำนวน/ชนิด |
|---|---:|
| version | 1 byte; ต้องเป็น 12 |
| azimuth, bias, fade, time | 4 floats |
| moon | 1 byte |
| sea level, snow level | 2 floats |
| rain/snow capability | 2 booleans |
| rain/snow frequency + duration | 4 floats |
| time profiles | 4 ชุด |
| colors ต่อ time profile | 12 RGB colors |
| scalar values ต่อ time profile | 5 floats |
| ขนาดรวม | 268 bytes |

สี 12 ช่องคือ sun, sea, fog, sky สามส่วน, ambient สามส่วน, clouds, rays และ particle lighting ส่วน scalar 5 ช่องคือ intensity, fog, clouds, shadows และ rays

decoder ปฏิเสธทันทีเมื่อ:

- version ไม่ใช่ 12
- payload สั้นกว่าที่ schema ต้องใช้
- boolean ไม่ใช่ 0/1
- float เป็น NaN/Infinity
- ขนาดไม่ใช่ 268 bytes หรือมี trailing bytes
- จำนวน time/color/scalar records ไม่ครบ

ทุก profile บันทึก SHA-256 ของ source file เพื่อให้ตรวจการเปลี่ยนแปลงภายหลังได้

## 3. Deterministic environment blending

`WorldEnvironmentBlender` ใช้ clamped smoothstep:

```text
t = x²(3 - 2x), x in [0, 1]
```

แล้ว blend ตัวเลขทั้งหมด รวม RGB 36 ช่องต่อช่วงเวลา, scalar 5 ช่อง, lighting header, sea/snow level และ rain/snow capability ในรูปน้ำหนัก 0–1 แทนการสลับ boolean กลางทาง

validation ทดสอบทุก unordered pair ของ 7 profiles:

```text
7 choose 2 = 21 pairs
21 pairs × 4 time profiles × 257 samples = 21,588 samples
```

ผล:

| Metric | ผล |
|---|---:|
| Blend endpoints exact | ผ่าน |
| Maximum adjacent normalized delta | 0.005859256 |
| Abrupt threshold | 0.02 |
| Abrupt transition | ไม่พบ |

นี่เป็น continuity test ของค่าข้อมูล ไม่ใช่การจับภาพ frame-to-frame ใน game renderer

## 4. Per-cell performance budgets

Phase 6 กำหนด budget contract เริ่มต้น:

| Budget | ค่า |
|---|---:|
| entity records/cell | 8,000 |
| static object records/cell | 5,000 |
| estimated record residency/cell | 8 MiB |

ค่าหน่วยความจำใช้ deterministic estimate **192 bytes ต่อ entity record** ไม่ใช่ Unity Profiler capture

ผล audit ของสองโซนปัจจุบัน:

| Zone | Cells | เกิน entity budget | เกิน static budget | Max entities | Max static | Max estimated bytes | HLOD required |
|---|---:|---:|---:|---:|---:|---:|---|
| California 2 | 49 | 2 | 2 | 14,370 | 11,013 | 2,759,040 | ใช่ |
| Limestone | 19 | 1 | 3 | 9,216 | 7,648 | 1,769,472 | ใช่ |

การเกิน budget ไม่ทำให้ foundation validator ล้ม เพราะเป้าหมายรอบนี้คือทำให้ hotspot ถูกค้นพบและบันทึกอย่าง deterministic; production gate ยังปิดจนกว่าจะมี HLOD/occlusion และ profiler evidence

## 5. Expansion readiness audit

| Map | Active landscape tiles | Objects | Spawn points | Hierarchy | Road joints | Inventory | Decode | Environment | Runtime included |
|---|---:|---:|---:|---:|---:|---|---|---|---|
| PEI | 16 | 4,329 | 4,103 | 73 | 98 | ผ่าน | ผ่าน | ผ่าน | ไม่ |
| Washington | 16 | 5,271 | 4,121 | 76 | 123 | ผ่าน | ผ่าน | ผ่าน | ไม่ |
| Yukon | 16 | 1,913 | 1,828 | 13 | 88 | ผ่าน | ผ่าน | ผ่าน | ไม่ |
| Russia | 64 | 15,282 | 9,149 | 138 | 705 | ผ่าน | ผ่าน | ผ่าน | ไม่ |
| Germany | 36 | 13,499 | 6,522 | 197 | 234 | ผ่าน | ผ่าน | ผ่าน | ไม่ |

ทุก candidate ถูกกำหนดสถานะ `BlockedPendingAuthoredRouteAndRuntimeValidation` พร้อม blocker เดียวกันสามข้อ:

1. ยังไม่มี world-layout offset และ geographic transition route ที่ level design อนุมัติ
2. ยังไม่ผ่าน single-zone runtime parity ของแมพนั้น
3. ยังไม่ผ่าน target-hardware runtime profiling ของแมพนั้น

จึงไม่มีการเดาพิกัด, สร้างทางเรือ/อุโมงค์ปลอม หรือเพิ่มแมพเข้า active manifest เพื่อทำให้ตัวเลขดูเหมือนเสร็จ

## 6. Generated artifacts

- `Builds/WorldUpgrade/EnvironmentProfiles.json`
- `Builds/WorldUpgrade/WorldSchemas/u3-connected-world/phase-6-environment-expansion-evidence.json`
- `Assets/Runtime/Assembly-CSharp/Unturned/WorldUpgrade/WorldEnvironmentData.cs`
- `Assets/Runtime/Assembly-CSharp/Unturned/WorldUpgrade/WorldPhase6EvidenceData.cs`
- `Assets/Editor/Assembly-CSharp-Editor/WorldUpgrade/WorldLightingProfileDecoder.cs`
- `Assets/Editor/Assembly-CSharp-Editor/WorldUpgrade/WorldUpgradePhase6Validator.cs`
- `Assets/Editor/Assembly-CSharp-Editor/Tests/WorldEnvironmentExpansionTests.cs`

## 7. Automated tests ที่เพิ่ม

เพิ่ม 4 tests:

1. exact v12/268-byte Lighting payload decode
2. trailing-byte rejection
3. exact blend endpoints และ midpoint
4. dense-cell performance budget classification

ผล full regression:

```text
total: 1,358
passed: 1,358
failed: 0
skipped: 0
duration: 0.9044036 seconds
```

## 8. Warning register

| Code | ความหมาย | การตัดสินใจ |
|---|---|---|
| `HlodRequired` | active cells บางส่วนเกิน record budget | ห้าม production จนสร้างและ profile HLOD |
| `OxygenAndAmbienceNotDecoded` | oxygen rules และ ambience asset graph ยังไม่มี decoder | ไม่เติมค่าเดา; บันทึก source-derived flags เป็น false |
| `HlodOcclusionNotGenerated` | มี audit contract แต่ยังไม่มี generated assets | เปิดเป็น blocker |
| `RuntimeProfilerNotExecuted` | ยังไม่มี CPU/GPU/memory capture บน target | ห้ามเรียก estimate ว่า measured performance |
| `ExpansionGatesClosed` | 5 candidates ยังไม่ผ่าน route/parity/performance | ไม่เพิ่มเข้า runtime manifest |

ระหว่างยกระดับ endpoint gate พบ false negative จากการเทียบ normalized RGB expression กับ materialized float โดยตรง จึงเปิดและแก้ Incident Phase 6-001 ก่อน final regression; ไม่มีหลักฐานรอบที่ล้มถูกนำมาใช้เป็น final evidence

## 9. Gate assessment

| เกณฑ์ | ผล | ขอบเขต |
|---|---|---|
| environment numeric values ไม่เปลี่ยนกะทันหัน | ผ่าน data-layer automated gate | 21,588 samples; ยังไม่ใช่ rendered frames |
| exact source decode ทุกแมพ | ผ่าน | 7/7, v12, 268 bytes, trailing 0 |
| per-cell budgets | ผ่านระดับ audit | พบ hotspot; HLOD ยังไม่สร้าง |
| PEI/Washington/Yukon/Russia/Germany import readiness | ผ่าน | inventory + binary summary + environment profile |
| แต่ละแมพผ่าน parity/performance ก่อนรวม | บังคับใช้ gate สำเร็จ | 0/5 ถูกเพิ่ม เพราะยังไม่ผ่าน |
| geographic routes | ยังไม่ผ่าน | ต้องมี authored design; ไม่เดาพิกัด |
| oxygen/ambience integration | ยังไม่ผ่าน | source-specific decoder/runtime hooks ยังไม่มี |
| HLOD/occlusion | ยังไม่ผ่าน | contract/audit เท่านั้น |
| target-hardware performance | ยังไม่ผ่าน | deterministic estimate เท่านั้น |
| full regression | ผ่าน | 1,358/1,358 |
| code/report บน GitHub | ผ่าน | implementation/report commit `40f2798`, Draft PR #1 |

## 10. ข้อจำกัด

- blender ยังไม่ hook เข้ากับ `LevelLighting`, RenderSettings, weather manager, audio หรือ water renderer
- color continuity threshold ครอบคลุมค่าที่ normalize แล้ว; ไม่ใช่ perceptual flicker metric
- azimuth ยัง blend เชิงเส้น ไม่ได้ wrap มุมตาม shortest arc
- sea/snow level ถูกอ่านจาก source แต่ geometry/collider ของน้ำยังไม่ blend
- oxygen gameplay rules และ ambience asset references ยังไม่ถูก decode
- ไม่มี HLOD mesh/material generation, occlusion bake หรือ runtime visibility service
- budget เป็น record-count contract ไม่รวม texture, shader, animation, physics หรือ asset sharing
- ห้าแมพใหม่ยังไม่มี world schema/cells ใน connected-world manifest
- ไม่มี authored route, art seam, traversal gameplay หรือ balance pass
- ยังไม่ทดสอบ manual gameplay หรือ target hardware

## 11. งานถัดไป

Phase 7 จะสร้าง multiplayer/production contract แบบ server-authoritative ที่ทดสอบได้ใน deterministic simulation:

- server-owned entity revision และ mutation authorization
- cell-based replication scopes
- reconnect snapshot และ migration
- anti-duplication transaction rules
- multi-client/load simulation และ packaging/license readiness audit

สิ่งที่ต้องใช้ live dedicated server หรือ external legal/design approval จะถูกแยกเป็น production blockers ไม่สร้างหลักฐานจำลองแทน

## 12. ข้อสรุป

Phase 6 ผ่านเฉพาะ automated environment/readiness foundation: source-derived profiles และ transition math ทำซ้ำได้, hotspot budgets ถูกเปิดเผย และ expansion gate ป้องกันการรวมแมพก่อนหลักฐานครบ ส่วน production environment, HLOD, authored routes และการรวม 5 แมพยังไม่เสร็จและถูกบันทึกเป็น blocker อย่างชัดเจน
