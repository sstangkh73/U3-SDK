# Washington — Map System Audit

| ค่า | ข้อมูล |
|---|---|
| Version | `3.26.1.0` |
| Level type | `SURVIVAL` |
| Level size | `MEDIUM` = 2048 units |
| Asset GUID | `9334e9cd34334029800e655f57a08f54` |
| Category | Official |
| Map root | `steamapps/common/Unturned/Maps/Washington` |

## System matrix

| ระบบ | Washington data | Gameplay config ownership |
|---|---|---|
| Player survival | snow sparkle, underground/water/environment volumes | inherited จาก difficulty/server |
| Items | `Items.dat` + `Jars.dat` | spawn/respawn/despawn/quality inherited |
| Zombies | `Zombies.dat` + `Animals.dat` + 21 nav chunks | density/damage/respawn inherited |
| Animals | `Fauna.dat` | cap/damage/respawn inherited |
| Vehicles | `Vehicles.dat`; ไม่มี train association ใน config | cap/respawn/decay inherited |
| Player spawn | `Players.dat` | algorithm inherited |
| NPC/quests | object/level asset references | official shared asset catalog |
| Buildables | `Buildables.dat` | decay/armor/persistence inherited |
| Events | environment/level asset | event timing inherited |
| Rendering | landscape, hierarchy, foliage, ambience; batching v2 | global streamer ต้องรับ ownership ต่อ cell |

ไม่มี map gameplay overrides ดู [Official shared systems](../official-main-maps/SHARED_SYSTEMS.md)

- [World data inventory](world-data.md)
