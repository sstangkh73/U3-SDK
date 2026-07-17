# Russia — World Data Inventory

## Geometry และ runtime data

| ข้อมูล | จำนวน/ขนาด |
|---|---:|
| Heightmaps / splatmaps / holes | 64 / 64 / 0 |
| Navigation chunks | 36 |
| Objects.dat | 1,291,885 bytes |
| Level.hierarchy | 5,180,949 bytes |
| Foliage.blob | 526,911,755 bytes |
| Buildables.dat | 8,349 bytes |
| Environment Paths.dat | 31,791 bytes |
| Ambience bundle | 3,998,308 bytes |

## Spawn data

| ระบบ | ไฟล์ | ขนาด |
|---|---|---:|
| Item tables | `Spawns/Items.dat` | 468 bytes |
| Item points | `Spawns/Jars.dat` | 91,406 bytes |
| Zombie tables | `Spawns/Zombies.dat` | 1,339 bytes |
| Zombie points | `Spawns/Animals.dat` | 40,147 bytes |
| Animal tables/points | `Spawns/Fauna.dat` | 1,120 bytes |
| Vehicle tables/points | `Spawns/Vehicles.dat` | 2,675 bytes |
| Player points | `Spawns/Players.dat` | 506 bytes |

ขนาด `Items.dat` เล็กแต่ `Jars.dat` ใหญ่ แปลได้เพียงว่าตาราง definition กระชับและ point data เยอะกว่า ไม่ควรตีความเป็น loot variety ต่ำจนกว่าจะ resolve spawn assets ที่อ้างต่อ

## Level flags และ train

- modern ground/water, terrain snow sparkle, holiday redirects และ underground whitelist
- train vehicle `186` ผูก local road index `3`
- batching v2, clutter option และ static volumes เปิด

## Migration note

Russia เป็น official stress-test หลัก: 64 heightmaps, foliage ประมาณ 527 MB และ spawn point data มาก ต้องผ่าน cell streaming, memory cycling และ persistence soak test ก่อนรวมกับ California
