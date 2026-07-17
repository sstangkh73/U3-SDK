# รายงานผลระยะ 1: Binary Decoders และ Validation

**วันที่ทดสอบ:** 18 กรกฎาคม 2026  
**Branch:** `codex/world-travel-mvp`  
**ผล gate:** ผ่านการตรวจในเครื่อง  
**ฐานจากระยะก่อน:** Phase 0 baseline commit `41bb305`  

## 1. วัตถุประสงค์

ระยะ 1 เปลี่ยน Raw Map Importer จากเครื่องมือที่รู้เพียงชื่อและขนาดไฟล์ ให้สามารถอ่านโครงสร้างข้อมูลที่เกมใช้จริง ตรวจไฟล์ truncated/เสีย ตรวจ reference ที่อยู่นอก table และสร้าง decoded summary ที่ใช้เป็น input ของ stable world schema ในระยะ 2

หลักสำคัญคือ decoder ต้องอ่าน source map แบบ read-only, ต้องไม่โหลด legacy gameplay overrides เป็น policy และต้องหยุดด้วย error เมื่อ byte stream ไม่ครบ แทนการคืนค่า zero แบบ compatibility reader ที่ runtime เดิมใช้

## 2. สิ่งที่ implement

### 2.1 Strict binary reader

เพิ่ม `StrictBinaryReader` สำหรับข้อมูล little-endian ของ Unturned รองรับ:

- byte และ boolean ที่ยอมรับเฉพาะ 0/1
- UInt16, Int32, UInt32, UInt64 และ finite Single
- Vector3
- UTF-8 string ที่มี byte-length prefix
- GUID ที่มี UInt16 length prefix และต้องยาว 16 bytes
- offset-aware truncation errors

เมื่อข้อมูลไม่พอ reader จะรายงาน path, byte offset, จำนวน byte ที่ต้องใช้ และจำนวนที่เหลือ

### 2.2 Spawn decoders

อ่าน format ตาม source reader เดิมของเกม:

- `Spawns/Items.dat`: item tables, tiers และ item entries
- `Spawns/Jars.dat`: item spawn points ในกริด 64 × 64 regions
- `Spawns/Zombies.dat`: zombie tables, unique IDs, health/damage/loot metadata และ clothing slots
- `Spawns/Animals.dat`: จุดเกิดซอมบี้ในกริด 64 × 64 regions
- `Spawns/Fauna.dat`: animal tables และ points
- `Spawns/Vehicles.dat`: vehicle tables, points และ angle byte
- `Spawns/Players.dat`: player points, angle และ alternate flag

Validator ตรวจ table index ของ spawn point, zombie unique ID, trailing bytes, string encoding, finite positions และ truncated payload

### 2.3 Landscape decoder

อ่านและตรวจ:

- heightmap ขนาด 257 × 257 samples, big-endian UInt16, 132,098 bytes ต่อ tile
- splatmap ขนาด 256 × 256 × 8 layers, 524,288 bytes ต่อ tile
- holes mask ขนาด 256 × 256 bits + version byte, 8,193 bytes ต่อ tile
- tile coordinates จาก filename
- world bounds จาก tile coordinates และ min/max height
- source-data mismatch ระหว่าง heightmap, splatmap และ holes
- pixel ที่ผลรวมน้ำหนัก material เป็นศูนย์

### 2.4 Objects decoder

อ่าน `Level/Objects.dat` ถึง version 12 รวม:

- region counts
- position, Euler rotation และ scale
- legacy ID และ GUID
- placement origin
- instance ID
- material palette override/index
- per-object culling override

Validator ตรวจ duplicate instance IDs, empty asset references, finite transforms, GUID length และ trailing bytes

### 2.5 Roads และ hierarchy

- อ่าน `Environment/Roads.dat` material metadata ถึง version 2
- อ่าน `Environment/Paths.dat` ถึง version 6 รวม loop, road asset GUID, tangents, mode, offset และ ignore-terrain flag
- ตรวจ material index และคำนวณ road bounds
- parse `Level.hierarchy` ด้วย KeyValueTableReader เดิมของโครงการ
- บันทึกจำนวน hierarchy items และ type inventory โดยไม่ instantiate hierarchy objects

### 2.6 Content hashes

เพิ่ม SHA-256 ของ byte content สำหรับ:

- spawn files ทุกไฟล์
- heightmap, splatmap และ holes ทุก tile
- `Objects.dat`
- `Roads.dat` และ `Paths.dat`
- `Level.hierarchy`

Hash ชุดนี้แยกจาก inventory fingerprint ระยะ 0 ซึ่งใช้เพียง relative path และ file size

### 2.7 Generator และ Unity UI

เพิ่ม output:

```text
Builds/WorldUpgrade/DecodedMapSummaries/<zone>.decoded-map-summary.json
```

เพิ่มเมนูและปุ่ม:

```text
Window > Unturned > World Upgrade > Generate Decoded Map Summaries
Decode and Validate Configured Maps
```

Command-line entrypoint คือ:

```text
SDG.Unturned.WorldUpgrade.Editor.RawMapDecodedSummaryGenerator.GenerateFromCommandLine
```

ถ้ามี summary ใดมี error process จะออกด้วย exit code 1 หลังเขียน evidence file แล้ว

## 3. Automated tests

เพิ่ม Unity EditMode tests 9 รายการ:

1. truncated integer ต้อง throw พร้อม offset
2. item table และ region spawn point synthetic data decode ถูกต้อง
3. truncated region spawn file ต้องกลายเป็น `BinaryFormat` error
4. landscape dimensions, bounds, splat weights และ holes decode ถูกต้อง
5. heightmap ขนาดผิดต้องเป็น validation error
6. splatmap ที่ไม่มี heightmap คู่กันต้องเป็น warning
7. Objects.dat version 12 synthetic data decode ถูกต้อง
8. Roads.dat v2 และ Paths.dat v6 synthetic data decode ถูกต้อง
9. Level.hierarchy type inventory parse ถูกต้อง

ผล Unity Test Framework:

| รายการ | ผล |
|---|---:|
| Test cases | 9 |
| Passed | 9 |
| Failed | 0 |
| Skipped | 0 |
| Duration | 0.155 วินาทีโดยประมาณ |

Test result XML และ Unity log อยู่ใต้ `Logs/` ในเครื่องและไม่ commit

## 4. Integration validation กับแมพจริง

รัน decoder กับ source map 7 แห่งจาก catalog และตรวจ fingerprint linkage กับ raw inventory manifest

| Zone | Item points | Zombie points | Animal points | Vehicle points | Player points | Height tiles | Objects | Hierarchy items | Road joints | Errors | Warnings |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| California 2 | 13,491 | 5,918 | 522 | 283 | 60 | 52 | 81,392 | 3,689 | 4,769 | 0 | 4 |
| Limestone | 3,638 | 1,242 | 157 | 111 | 19 | 22 | 26,714 | 684 | 1,858 | 0 | 1 |
| PEI | 2,470 | 1,456 | 60 | 95 | 22 | 16 | 4,329 | 73 | 98 | 0 | 0 |
| Washington | 2,798 | 1,137 | 59 | 103 | 24 | 16 | 5,271 | 76 | 123 | 0 | 0 |
| Yukon | 1,207 | 524 | 35 | 44 | 18 | 16 | 1,913 | 13 | 88 | 0 | 0 |
| Russia | 6,401 | 2,458 | 85 | 169 | 36 | 64 | 15,282 | 138 | 705 | 0 | 0 |
| Germany | 4,343 | 1,955 | 120 | 90 | 14 | 36 | 13,499 | 197 | 234 | 0 | 0 |
| **รวม** | **34,348** | **14,690** | **1,038** | **895** | **193** | **222** | **148,400** | **4,870** | **7,875** | **0** | **5** |

ทุกไฟล์ที่รองรับถูกอ่านจนจบโดย trailing bytes เท่ากับศูนย์ และ summary ทั้ง 7 มี `IsValid=true`

## 5. Source-data warnings

### 5.1 Black splat pixels

- California 2: `Landscape/Splatmaps/Tile_-1_3_Source.splatmap` จำนวน 1 pixel
- Limestone: `Landscape/Splatmaps/Tile_-1_-2_Source.splatmap` จำนวน 1 pixel

Pixel เหล่านี้มีน้ำหนัก material รวมเป็นศูนย์ ไม่แก้ source Workshop โดยอัตโนมัติ ระยะ 3 ต้องกำหนด fallback material เพื่อป้องกัน terrain ดำ

### 5.2 Splat/hole data ที่ไม่มี heightmap คู่กัน

California 2 มี 3 tile coordinates:

- `2,-6`
- `2,-4`
- `3,-4`

มี splatmap หรือ holes data แต่ไม่มี source heightmap ชื่อเดียวกัน อาจเป็น orphan data หรือ tile ที่ตั้งใจใช้ default height ระยะ 2 ต้องเก็บสถานะนี้ใน schema และระยะ 3 ต้องตรวจเทียบกับ runtime เดิมก่อนตัดสินใจว่าจะสร้าง default terrain หรือไม่โหลด tile

## 6. ปัญหาระหว่าง implementation และการแก้ไข

### 6.1 `InvalidDataException` เป็น sealed ใน Unity profile

Custom `RawMapBinaryFormatException` เดิมสืบทอดจาก `InvalidDataException` แต่ Unity 2022.3 profile ของโครงการประกาศชนิดนี้เป็น sealed ทำให้ compile error `CS0509`

**การแก้:** เปลี่ยนฐานเป็น `IOException` และรักษา path/offset fields ไว้ ผล compile รอบถัดไปผ่าน

### 6.2 ชื่อ `Action` ชนกับ namespace ของเกม

`Action` แบบไม่ fully qualify ถูก resolve เป็นชื่อใน namespace ของเกม ทำให้ lambda ไม่ถูกมองเป็น delegate และเกิด `CS0149`/`CS1660`

**การแก้:** ใช้ `System.Action` แบบเต็มใน validation wrapper ผล compile รอบถัดไปผ่าน

### 6.3 Source-data mismatches

พบ warning 5 รายการตามหัวข้อก่อนหน้า เนื่องจาก importer มีนโยบาย read-only จึงไม่แก้ไฟล์ต้นฉบับ แต่เก็บ warning พร้อม path และจำนวนใน generated summary เพื่อให้ runtime ระยะถัดไปตัดสินใจอย่างโปร่งใส

## 7. ข้อจำกัดที่ยังเหลือ

- Decoded summary เก็บ counts, bounds, table summaries และ hashes แต่ยังไม่ export record รายตัวพร้อม stable IDs
- Legacy per-region object files (`Objects_x_y.dat`) ยังไม่ decode ถ้าไม่มี aggregated `Objects.dat`
- Legacy terrain PNG/data ถูก inventory แต่ยังไม่แปลงเป็น normalized landscape samples
- Navigation mesh files, environment volumes และ arbitrary hierarchy item fields ยังไม่ decode เป็น runtime records
- Decoder รองรับ format ที่ยืนยันจาก source ปัจจุบัน แต่ยังต้องมี fixture เพิ่มสำหรับ version เก่าที่ไม่มีในแมพ 7 แห่ง
- Warning ของ orphan landscape data ยังไม่มี runtime policy
- Absolute source paths ยังผูกกับเครื่องปัจจุบัน

ข้อจำกัดเหล่านี้ไม่ขัดกับ gate ระยะ 1 เพราะเป้าหมายของระยะนี้คือ structural decode/validation และข้อมูลที่ต้องใช้สร้าง stable schema ระยะ 2 ไม่ใช่การเปิดโลกใน runtime

## 8. เกณฑ์ผ่าน

| เกณฑ์ | ผล |
|---|---|
| Strict reader ตรวจ truncated/invalid primitives | ผ่าน |
| Spawn formats หลัก decode ได้ | ผ่าน |
| Landscape tiles decode และ validate ได้ | ผ่าน |
| Objects v12, Roads v2/Paths v6 และ hierarchy decode ได้ | ผ่าน |
| SHA-256 byte-content hashes ถูกสร้าง | ผ่าน |
| Unity compile ผ่าน | ผ่าน |
| EditMode tests ผ่าน | ผ่าน 9/9 |
| Integration decode แมพจริง | ผ่าน 7/7 |
| ไม่มี validation errors | ผ่าน — 0 errors |
| Warning และข้อจำกัดถูกบันทึก | ผ่าน |
| Code/report อยู่บน GitHub | รอเติม commit metadata หลัง push |

## 9. ข้อสรุป

Phase 1 ผ่าน gate ในเครื่องแล้ว โครงการมี decoder ที่หยุดเมื่อข้อมูลเสียและมีหลักฐานจาก source map จริงครบ 7 แห่ง ข้อมูล spawn, landscape, objects, roads และ hierarchy พร้อมใช้เป็น input ของ Phase 2 ซึ่งจะสร้าง deterministic records, stable zone/cell/entity IDs และ migration-aware world schema

