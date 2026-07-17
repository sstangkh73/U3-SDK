# Russia — Map System Audit

| ค่า | ข้อมูล |
|---|---|
| Version | `3.26.1.0` |
| Level type | `SURVIVAL` |
| Level size | `LARGE` = 4096 units |
| Asset GUID | `341a12e4ec1d424ca56876b4e9a722ac` |
| Stockpile item | `21022` |
| Category | Official |
| Map root | `steamapps/common/Unturned/Maps/Russia` |

## System matrix

| ระบบ | Russia data | Gameplay config ownership |
|---|---|---|
| Player survival | snow sparkle, underground/water/environment volumes | inherited จาก difficulty/server |
| Items | `Items.dat` + `Jars.dat` | spawn/respawn/despawn/quality inherited |
| Zombies | `Zombies.dat` + `Animals.dat` + 36 nav chunks | density/damage/respawn inherited |
| Animals | `Fauna.dat` | cap/damage/respawn inherited |
| Vehicles | `Vehicles.dat` + train vehicle 186/road 3 | cap/respawn/decay inherited |
| Player spawn | `Players.dat` | algorithm inherited |
| NPC/quests | object/level asset references | official shared asset catalog |
| Buildables | `Buildables.dat` | decay/armor/persistence inherited |
| Events | environment/level asset | event timing inherited |
| Rendering | 64 landscape tiles ต่อชนิด, hierarchy/foliage ขนาดใหญ่ | ต้องแบ่ง cell และ profile ก่อนรวม |

ไม่มี map gameplay overrides ดู [Official shared systems](../official-main-maps/SHARED_SYSTEMS.md)

- [World data inventory](world-data.md)
