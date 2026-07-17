# Yukon — World Data Inventory

## Geometry และ runtime data

| ข้อมูล | จำนวน/ขนาด |
|---|---:|
| Heightmaps / splatmaps / holes | 16 / 16 / 0 |
| Navigation chunks | 18 |
| Objects.dat | 168,889 bytes |
| Level.hierarchy | 1,272,847 bytes |
| Foliage.blob | 7,542,805 bytes |
| Buildables.dat | 8,193 bytes |
| Environment Paths.dat | 3,941 bytes |
| Ambience bundle | 1,963,608 bytes |

## Spawn data

| ระบบ | ไฟล์ | ขนาด |
|---|---|---:|
| Item tables | `Spawns/Items.dat` | 2,215 bytes |
| Item points | `Spawns/Jars.dat` | 23,884 bytes |
| Zombie tables | `Spawns/Zombies.dat` | 932 bytes |
| Zombie points | `Spawns/Animals.dat` | 15,005 bytes |
| Animal tables/points | `Spawns/Fauna.dat` | 542 bytes |
| Vehicle tables/points | `Spawns/Vehicles.dat` | 907 bytes |
| Player points | `Spawns/Players.dat` | 254 bytes |

## Level flags และ train

- modern ground: `Use_Legacy_Ground=false`
- `Use_Legacy_Water` ไม่ได้กำหนด จึงรับค่า default ของ level config; ห้ามสมมติว่าเหมือน PEI/Washington
- terrain snow sparkle, aurora และ underground whitelist เปิด
- train vehicle `187` ผูกกับ local road index `0`
- batching v2, clutter option และ static volumes เปิด

## Migration note

Yukon ควรเป็น test zone ของ environment blending และ survival interaction: หิมะมีผลต่อ food interval ใน `PlayerLife`, aurora เป็น visual identity และ legacy-water state ต่างจาก official maps อื่นที่ระบุ false ชัดเจน
