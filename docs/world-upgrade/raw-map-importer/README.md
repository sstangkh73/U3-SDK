# Raw Map Importer และ Phase 1 Decoder

เครื่องมือมีสองชั้นแบบ read-only ตาม ADR-001:

1. inventory generator บันทึกรายการไฟล์ ขนาด category และ dependency
2. Phase 1 decoder อ่านโครงสร้าง binary/text ที่ยืนยันจาก source reader เดิม และสร้าง validation summary

ผลลัพธ์ยังไม่ใช่ `WorldCell` และยังไม่ถูกโหลดเป็นโลกใหม่ใน runtime

## ใช้งานใน Unity

1. ออกจาก Play Mode
2. เปิด `Window > Unturned > World Upgrade > Raw Map Importer`
3. กด `Generate Configured Raw Map Manifests`

หรือเลือก `Window > Unturned > World Upgrade > Generate Raw Map Manifests` โดยตรง

จากนั้นเลือก `Decode and Validate Configured Maps` ในหน้าต่างเดิม หรือใช้เมนู `Window > Unturned > World Upgrade > Generate Decoded Map Summaries`

Source catalog อยู่ที่ `Builds/WorldUpgrade/WorldSourceCatalog.json` และ output อยู่ที่ `Builds/WorldUpgrade/RawMapManifests/`

Decoded summaries อยู่ที่ `Builds/WorldUpgrade/DecodedMapSummaries/`

## Format ที่ Phase 1 decode แล้ว

- `Spawns/Items.dat` และ `Spawns/Jars.dat`
- `Spawns/Zombies.dat` และ `Spawns/Animals.dat` ซึ่งเป็นจุดเกิดซอมบี้
- `Spawns/Fauna.dat`, `Vehicles.dat` และ `Players.dat`
- landscape heightmaps, splatmaps และ holes พร้อม tile coordinates/bounds
- `Level/Objects.dat` รวม object version 12 fields
- `Environment/Roads.dat` และ `Environment/Paths.dat` ถึง paths version 6
- `Level.hierarchy` เป็น item/type inventory

Reader หยุดด้วย validation error เมื่อข้อมูล truncated, GUID length ผิด, float ไม่ finite หรือ reference index ออกนอก table แทนการคืนค่า zero แบบ reader runtime เดิม

ทุกไฟล์ที่ decode มี SHA-256 ของ byte content แยกจาก inventory fingerprint ที่ใช้เพียง path และ file size

## สิ่งที่ manifest เก็บ

- level format version, size/type และ world units
- map version, level asset GUID และ workshop dependency IDs
- relative path, byte size และ category ของทุกไฟล์ใน map root
- category summaries และ deterministic SHA-256 inventory fingerprint
- dependency root summary/fingerprint โดยไม่คัดลอก asset
- legacy gameplay override **keys** สำหรับ audit เท่านั้น
- flags `CopiesSourceContent=false` และ `ImportsLegacyGameplayRules=false`

## ผลการ generate 2026-07-14

| Zone | Map files | Dependency summaries | Level |
|---|---:|---:|---|
| California 2 | 272 | 1 | Insane / Survival |
| Limestone | 7,179 | 0 | Medium / Survival |
| PEI | 106 | 0 | Medium / Survival |
| Washington | 106 | 0 | Medium / Survival |
| Yukon | 102 | 0 | Medium / Survival |
| Russia | 216 | 0 | Large / Survival |
| Germany | 144 | 0 | Large / Survival |

Limestone มี asset content อยู่ภายใน map root จึงมีจำนวนไฟล์สูง ส่วน California แยก dependency pack ออกจาก map root

## Safety boundary

- อ่าน source files เท่านั้น
- เขียนเฉพาะ derived JSON ภายใน Unity project
- output directory ถูกบังคับให้อยู่ใต้ project root
- Zone ID จำกัดเป็น lowercase/digit/hyphen เพื่อใช้เป็นชื่อไฟล์ปลอดภัย
- ไม่ deserialize legacy override values ไปเป็น runtime policy

## ผล Phase 1 วันที่ 2026-07-18

- Unity compile ผ่าน
- Unity EditMode tests ผ่าน 9/9
- decode แมพ 7/7 สำเร็จโดยไม่มี validation error
- รวม item points 34,348 จุด, zombie points 14,690 จุด, objects 148,400 รายการ และ landscape heightmap 222 tiles
- summary ที่ regenerate หลัง Phase 3 พบ warnings 20 รายการ: black splat 2, source-only landscape tiles 15 และ missing-height diagnostics 3 รายการที่ซ้อนอยู่ในกลุ่ม source-only

Warning เหล่านี้ไม่ทำให้ decoder ล้ม `Level.hierarchy` เป็น authority ของ active terrain; source-only files ถูก inventory แต่ไม่ import และ black splat pixels ใช้ explicit layer 0 fallback โดยไม่แก้ Workshop source

## ขั้นถัดไป

1. ใช้ Phase 3 single-zone parity เป็น baseline ของ two-zone streamer
2. เพิ่ม Limestone และ proximity-based cell activation
3. decode navigation, environment volumes และ hierarchy records เพิ่ม
4. วัด native memory/frame-time ด้วย Unity Profiler soak
5. ทดสอบ transition safety ก่อนทำรอยต่อศิลป์จริง

รายละเอียด schema, identity rules, migration policy และ output อยู่ใน [`docs/world-upgrade/world-schema/README.md`](../world-schema/README.md)
