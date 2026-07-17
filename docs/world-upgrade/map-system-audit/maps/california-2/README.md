# California 2 — Map Profile

## Identity

| ค่า | ข้อมูล |
|---|---|
| Workshop map ID | `3707778928` |
| Version | `2.1.0.1` |
| Level type | `SURVIVAL` |
| Level size | `INSANE` = 8192 units |
| Asset GUID | `009505be2a2d48b7b0a8f71adba8b858` |
| Required workshop file | `3711646503` — California 2 Assets |
| Map root | `steamapps/workshop/content/304930/3707778928/California2` |

ขนาดและชนิดถูกอ่านจาก `Level.dat` ตาม format ใน `Level.cs` ไม่ได้เดาจากชื่อหรือภาพแผนที่

## System modules

- [ผู้เล่น การกิน น้ำ เชื้อ และ gameplay](player-survival.md)
- [ไอเท็ม loot และวงจรเกิด/หาย](item-economy.md)
- [ซอมบี้ สัตว์ NPC และ population](population.md)
- [terrain, environment, รถ, event, buildables และ performance](world-runtime.md)
- [asset และ dependency inventory](content-assets.md)

## โครงสร้างโลกที่พบ

| ข้อมูล | จำนวน/ขนาด |
|---|---:|
| Heightmap files | 52 |
| Splatmap files | 55 |
| Hole files | 43 |
| Navigation chunks | 52 |
| `Level/Objects.dat` | 6,845,125 bytes |
| `Level.hierarchy` | 6,082,665 bytes |
| `Foliage.blob` | 286,152,041 bytes |
| `Spawns/Jars.dat` — item points | 183,576 bytes |
| `Spawns/Animals.dat` — zombie points | 85,127 bytes |
| `Spawns/Fauna.dat` — animal data | 7,124 bytes |
| `Spawns/Vehicles.dat` | 4,371 bytes |
| `Spawns/Players.dat` | 842 bytes |

ขนาดไฟล์ใช้ยืนยัน scale และว่าระบบมีข้อมูลอยู่จริง แต่ไม่ใช่จำนวน spawn point จำนวน point ที่แม่นยำจะมาจาก importer ที่อ่าน binary format โดยตรงในขั้นถัดไป

## Legacy flags

| Flag | ค่า | ผลต่อการย้าย |
|---|---:|---|
| `Use_Legacy_Water` | false | ใช้ water system รุ่นใหม่ของ level |
| `Use_Legacy_Ground` | false | ใช้ landscape tiles ไม่ควรย้ายเฉพาะ legacy terrain |
| `Use_Legacy_Clip_Borders` | false | ห้ามพึ่ง border clipping แบบเก่าเป็นขอบโลกใหม่ |
| `Use_Underground_Whitelist` | true | ต้องนำ whitelist/underground behavior เข้า zone data |
| `Allow_Holiday_Redirects` | true | content redirect ตามเทศกาลยังเปิด |

## สถานะการย้าย

- `Preserve`: terrain/เมือง, object placement, roads, spawn tables/points, NPC/quest identity
- `Normalize`: survival base rules, save ownership, item/zombie budgets
- `Blend`: weather, ambience, lighting, water/fog ที่รอยต่อ
- `Replace`: active-level singleton, vanilla airdrop handling, region-global arrays
