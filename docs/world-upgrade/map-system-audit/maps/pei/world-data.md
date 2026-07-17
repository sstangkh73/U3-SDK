# PEI — World Data Inventory

## Geometry และ runtime data

| ข้อมูล | จำนวน/ขนาด |
|---|---:|
| Heightmaps / splatmaps / holes | 16 / 16 / 2 |
| Navigation chunks | 19 |
| Objects.dat | 371,833 bytes |
| Level.hierarchy | 1,303,123 bytes |
| Foliage.blob | 43,521,858 bytes |
| Buildables.dat | 8,193 bytes |
| Environment Paths.dat | 4,625 bytes |
| Ambience bundle | 2,339,728 bytes |

## Spawn data

| ระบบ | ไฟล์ | ขนาด |
|---|---|---:|
| Item tables | `Spawns/Items.dat` | 3,354 bytes |
| Item points | `Spawns/Jars.dat` | 40,303 bytes |
| Zombie tables | `Spawns/Zombies.dat` | 1,139 bytes |
| Zombie points | `Spawns/Animals.dat` | 27,121 bytes |
| Animal tables/points | `Spawns/Fauna.dat` | 814 bytes |
| Vehicle tables/points | `Spawns/Vehicles.dat` | 1,894 bytes |
| Player points | `Spawns/Players.dat` | 310 bytes |

## Level flags

- modern ground and water: `Use_Legacy_Ground=false`, `Use_Legacy_Water=false`
- terrain snow sparkle, holiday redirects, underground whitelist เปิด
- batching v2, clutter option และ static volumes เปิด

## Migration note

PEI เป็นตัวเลือกดีสำหรับ official importer test แรก: ขนาด Medium, data footprint ต่ำกว่าแมพใหญ่ และไม่มี map gameplay overrides แต่ต้องรองรับ `Level.dat` version 1 ที่ type default เป็น Survival
