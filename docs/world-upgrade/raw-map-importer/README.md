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
- พบ source-data warning 5 รายการ: black splat pixel 2 จุด และ splat/hole tiles ที่ไม่มี heightmap คู่กัน 3 tiles ใน California 2

Warning เหล่านี้ไม่ทำให้ decoder ล้ม แต่ runtime terrain ระยะถัดไปต้องมี fallback material/height policy และห้ามแก้ Workshop source โดยอัตโนมัติ

## ขั้นถัดไป

1. สร้าง world schema และ stable zone/cell/entity IDs จาก decoded records
2. แยก output เป็น deterministic records ที่ diff และ migrate ได้
3. กำหนด fallback สำหรับ orphan landscape data และ black splat pixels
4. สร้าง world layout manifest
5. เริ่ม single-zone runtime parity ก่อน cell streamer สอง zone
