# PEI — Map System Audit

| ค่า | ข้อมูล |
|---|---|
| Version | `3.26.1.0` |
| Level type | `SURVIVAL` |
| Level size | `MEDIUM` = 2048 units |
| Level.dat format | version 1; type ไม่ถูกเก็บและ loader default เป็น Survival |
| Asset GUID | `d258342682aa44f89b08de0b47797c4e` |
| Category | Official |
| Map root | `steamapps/common/Unturned/Maps/PEI` |

## System matrix

| ระบบ | PEI data | Gameplay config ownership |
|---|---|---|
| Player survival | snow sparkle, underground/water/environment volumes | inherited จาก difficulty/server |
| Items | `Items.dat` + `Jars.dat` | spawn/respawn/despawn/quality inherited |
| Zombies | `Zombies.dat` + `Animals.dat` + 19 nav chunks | density/damage/respawn inherited |
| Animals | `Fauna.dat` | cap/damage/respawn inherited |
| Vehicles | `Vehicles.dat`; ไม่มี train association ใน config | cap/respawn/decay inherited |
| Player spawn | `Players.dat` | algorithm inherited |
| NPC/quests | object/level asset references | official shared asset catalog |
| Buildables | `Buildables.dat` | decay/armor/persistence inherited |
| Events | environment/level asset | event timing inherited |
| Rendering | landscape, hierarchy, foliage, ambience; batching v2 | global streamer ต้องรับ ownership ต่อ cell |

ไม่มี `Mode_Config_Overrides` และ difficulty overrides ใน `Config.json` ดูหลักการร่วมที่ [Official shared systems](../official-main-maps/SHARED_SYSTEMS.md)

- [World data inventory](world-data.md)
