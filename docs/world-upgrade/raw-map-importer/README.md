# Raw Map Importer

Importer รุ่นแรกสร้าง inventory manifests จากแมพภายนอกแบบ read-only ตาม ADR-001 มันยังไม่ decode terrain/spawn binaries เป็น world cells

## ใช้งานใน Unity

1. ออกจาก Play Mode
2. เปิด `Window > Unturned > World Upgrade > Raw Map Importer`
3. กด `Generate Configured Raw Map Manifests`

หรือเลือก `Window > Unturned > World Upgrade > Generate Raw Map Manifests` โดยตรง

Source catalog อยู่ที่ `Builds/WorldUpgrade/WorldSourceCatalog.json` และ output อยู่ที่ `Builds/WorldUpgrade/RawMapManifests/`

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

## ขั้นถัดไป

1. decoder ของ `Spawns/Items.dat` และ `Spawns/Jars.dat`
2. decoder ของ zombie/animal/vehicle/player spawn data
3. landscape tile manifest พร้อม bounds/coordinates
4. object/hierarchy conversion เป็น stable zone/cell/entity IDs
5. world layout manifest และ cell streamer prototype
