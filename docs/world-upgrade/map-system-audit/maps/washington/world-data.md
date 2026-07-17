# Washington — World Data Inventory

## Geometry และ runtime data

| ข้อมูล | จำนวน/ขนาด |
|---|---:|
| Heightmaps / splatmaps / holes | 16 / 16 / 0 |
| Navigation chunks | 21 |
| Objects.dat | 450,961 bytes |
| Level.hierarchy | 1,302,409 bytes |
| Foliage.blob | 98,458,579 bytes |
| Buildables.dat | 8,193 bytes |
| Environment Paths.dat | 5,851 bytes |
| Ambience bundle | 2,497,360 bytes |

## Spawn data

| ระบบ | ไฟล์ | ขนาด |
|---|---|---:|
| Item tables | `Spawns/Items.dat` | 3,273 bytes |
| Item points | `Spawns/Jars.dat` | 44,567 bytes |
| Zombie tables | `Spawns/Zombies.dat` | 985 bytes |
| Zombie points | `Spawns/Animals.dat` | 22,974 bytes |
| Animal tables/points | `Spawns/Fauna.dat` | 801 bytes |
| Vehicle tables/points | `Spawns/Vehicles.dat` | 2,133 bytes |
| Player points | `Spawns/Players.dat` | 338 bytes |

## Level flags

- modern ground/water, terrain snow sparkle, holiday redirects และ underground whitelist
- batching v2, clutter option และ static volumes เปิด
- clip border ไม่ได้ override จึงรับ default ของ level config; importer ต้องอ่าน effective flag ไม่เดาจากแมพอื่น

## Migration note

Washington ใช้ schema เดียวกับ PEI ได้ แต่มี foliage และ vehicle data ใหญ่กว่า การแยก cell ต้องอิง tile/object density ไม่ใช่ตั้ง budget จากคำว่า Medium เท่านั้น
