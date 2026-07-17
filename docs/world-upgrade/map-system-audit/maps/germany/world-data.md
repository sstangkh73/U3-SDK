# Germany — World Data Inventory

## Geometry และ runtime data

| ข้อมูล | จำนวน/ขนาด |
|---|---:|
| Heightmaps / splatmaps / holes | 36 / 36 / 5 |
| Navigation chunks | 26 |
| Objects.dat | 1,142,113 bytes |
| Level.hierarchy | 2,952,469 bytes |
| Foliage.blob | 470,889,263 bytes |
| Buildables.dat | 8,193 bytes |
| Environment Paths.dat | 11,041 bytes |
| Ambience bundle | 2,616,759 bytes |

## Spawn data

| ระบบ | ไฟล์ | ขนาด |
|---|---|---:|
| Item tables | `Spawns/Items.dat` | 532 bytes |
| Item points | `Spawns/Jars.dat` | 64,652 bytes |
| Zombie tables | `Spawns/Zombies.dat` | 1,479 bytes |
| Zombie points | `Spawns/Animals.dat` | 33,608 bytes |
| Animal tables/points | `Spawns/Fauna.dat` | 1,575 bytes |
| Vehicle tables/points | `Spawns/Vehicles.dat` | 1,659 bytes |
| Player points | `Spawns/Players.dat` | 198 bytes |

## Level flags

- modern ground, water, fog height และ oxygen height
- legacy clip borders ปิดชัดเจน
- terrain snow sparkle และ underground whitelist เปิด
- batching v2, clutter option และ static volumes เปิด

## Migration note

Germany ต้องนำ holes, fog/oxygen volumes และ underground whitelist เข้า importer พร้อมกัน เพราะรอยต่อที่มองเห็นปกติอาจยังเปลี่ยน gameplay จาก oxygen/underground rules ได้
