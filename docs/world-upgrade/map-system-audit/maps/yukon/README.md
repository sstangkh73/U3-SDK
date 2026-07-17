# Yukon — Map System Audit

| ค่า | ข้อมูล |
|---|---|
| Version | `3.26.1.0` |
| Level type | `SURVIVAL` |
| Level size | `MEDIUM` = 2048 units |
| Asset GUID | `537278c5f6eb45198994ff2915d3107b` |
| Category | Official |
| Map root | `steamapps/common/Unturned/Maps/Yukon` |

## System matrix

| ระบบ | Yukon data | Gameplay config ownership |
|---|---|---|
| Player survival | snow/temperature identity, aurora, environment volumes | food/water/virus rates inherited; snow effect ทำงานผ่านกลไกกลาง |
| Items | `Items.dat` + `Jars.dat` | spawn/respawn/despawn/quality inherited |
| Zombies | `Zombies.dat` + `Animals.dat` + 18 nav chunks | density/damage/respawn inherited |
| Animals | `Fauna.dat` | cap/damage/respawn inherited |
| Vehicles | `Vehicles.dat` + train vehicle 187/road 0 | cap/respawn/decay inherited |
| Player spawn | `Players.dat` | algorithm inherited |
| NPC/quests | object/level asset references | official shared asset catalog |
| Buildables | `Buildables.dat` | decay/armor/persistence inherited |
| Events/environment | aurora visible, terrain snow sparkle | timers inherited |
| Rendering | landscape, hierarchy, foliage, ambience; batching v2 | global streamer ต้องรับ ownership ต่อ cell |

ไม่มี map gameplay overrides ดู [Official shared systems](../official-main-maps/SHARED_SYSTEMS.md)

- [World data inventory](world-data.md)
