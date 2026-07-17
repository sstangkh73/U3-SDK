# Limestone — Map Profile

## Identity

| ค่า | ข้อมูล |
|---|---|
| Workshop map ID | `3565225438` |
| Version | `1.2.2.3` |
| Level type | `SURVIVAL` |
| Level size | `MEDIUM` = 2048 units |
| Asset GUID | `9185aa4c68cd4a059117b9ebe411551c` |
| Map root | `steamapps/workshop/content/304930/3565225438/Limestone` |
| Gravity override | -12 |

ขนาดและชนิดถูกอ่านจาก `Level.dat` ตาม format ใน `Level.cs`

## System modules

- [ผู้เล่น การกิน น้ำ เชื้อ ความยาก และ gameplay](player-survival.md)
- [ไอเท็ม loot และวงจรเกิด/หาย](item-economy.md)
- [ซอมบี้ สัตว์ NPC และ population](population.md)
- [terrain, environment, รถ, event, buildables และ performance](world-runtime.md)
- [asset inventory](content-assets.md)

## โครงสร้างโลกที่พบ

| ข้อมูล | จำนวน/ขนาด |
|---|---:|
| Heightmap files | 22 |
| Splatmap files | 22 |
| Hole files | 20 |
| Navigation chunks | 34 |
| `Level/Objects.dat` | 2,252,173 bytes |
| `Level.hierarchy` | 2,096,000 bytes |
| `Foliage.blob` | 310,747,681 bytes |
| `Spawns/Jars.dat` — item points | 55,487 bytes |
| `Spawns/Animals.dat` — zombie points | 24,339 bytes |
| `Spawns/Fauna.dat` — animal data | 2,085 bytes |
| `Spawns/Vehicles.dat` | 1,815 bytes |
| `Spawns/Players.dat` | 268 bytes |

`TerrainWasAutoConverted.txt` มีอยู่ในแมพ แสดงว่าข้อมูล terrain บางส่วนผ่านกระบวนการแปลงอัตโนมัติ ต้องให้ landscape รุ่นใหม่เป็น source of truth และเก็บ legacy artifact ไว้เพื่อเทียบเท่านั้น

## Legacy flags

| Flag | ค่า |
|---|---:|
| `Use_Legacy_Ground` | false |
| `Use_Legacy_Water` | false |
| `Use_Legacy_Fog_Height` | false |
| `Use_Legacy_Oxygen_Height` | false |
| `Use_Legacy_Clip_Borders` | false |
| `Use_Underground_Whitelist` | true |
| `Prevent_Building_Near_Spawnpoint_Radius` | 64 |

## สถานะการย้าย

- `Preserve`: terrain/เมือง, object placement, roads, spawn tables/points, difficulty identity
- `Normalize`: global survival algorithm, save ownership, item/zombie budgets
- `Blend`: lighting, fog, oxygen, ambience และ weather
- `Replace`: active-level singleton, region-global arrays และ event scheduler
