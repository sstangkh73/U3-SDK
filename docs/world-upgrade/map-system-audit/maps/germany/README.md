# Germany — Map System Audit

| ค่า | ข้อมูล |
|---|---|
| Version | `3.26.1.0` |
| Level type | `SURVIVAL` |
| Level size | `LARGE` = 4096 units |
| Asset GUID | `c8c0a176453b40018fbea2d4703af223` |
| Category | Official |
| Map root | `steamapps/common/Unturned/Maps/Germany` |

## System matrix

| ระบบ | Germany data | Gameplay config ownership |
|---|---|---|
| Player survival | modern fog/oxygen heights, underground/water/environment volumes | inherited จาก difficulty/server |
| Items | `Items.dat` + `Jars.dat` | spawn/respawn/despawn/quality inherited |
| Zombies | `Zombies.dat` + `Animals.dat` + 26 nav chunks | density/damage/respawn inherited |
| Animals | `Fauna.dat` | cap/damage/respawn inherited |
| Vehicles | `Vehicles.dat`; ไม่มี train association ใน config | cap/respawn/decay inherited |
| Player spawn | `Players.dat` | algorithm inherited |
| NPC/quests | object/level asset references | official shared asset catalog |
| Buildables | `Buildables.dat` | decay/armor/persistence inherited |
| Events | environment/level asset | event timing inherited |
| Rendering | 36 landscape tiles ต่อชนิด, hierarchy/foliage ขนาดใหญ่ | ต้องแบ่ง cell และ profile ก่อนรวม |

ไม่มี map gameplay overrides ดู [Official shared systems](../official-main-maps/SHARED_SYSTEMS.md)

- [World data inventory](world-data.md)
