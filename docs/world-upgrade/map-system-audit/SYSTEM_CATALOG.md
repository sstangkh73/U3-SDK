# บัญชีระบบของแมพ

ไฟล์นี้กำหนดว่าคำว่า “ทุกระบบของแมพ” ครอบคลุมอะไร แหล่งข้อมูลอยู่ที่ไหน และควรย้ายเข้าสู่โลกใหม่แบบใด

## ชิ้นส่วนมาตรฐาน

| Component | หน้าที่ | ข้อมูลดั้งเดิม | กลไกกลางหลัก | นโยบายโลกใหม่ |
|---|---|---|---|---|
| `MapIdentity` | ชื่อ รุ่น ผู้สร้าง dependency ขนาดและชนิดแมพ | `Level.dat`, `Config.json` | `Level`, `LevelInfo` | แปลงเป็น `WorldZoneDefinition` |
| `WorldGeometry` | terrain, holes, roads, water และขอบเขต | `Landscape/`, `Terrain/`, `Roads.*` | `LevelGround`, `LevelRoads` | แปลงเป็น streamable world cells |
| `WorldObjects` | อาคาร ของตกแต่ง resource nodes และ buildables ที่ฝังมา | `Level/Objects.dat`, `Level/Buildables.dat`, `Level.hierarchy` | `LevelObjects`, `ObjectManager` | แยก static/dynamic persistence |
| `Environment` | แสง อากาศ fog oxygen ambience | `Environment/`, `Config.json` | `LightingManager`, `LevelLighting` | เป็น zone profile และ blend ที่รอยต่อ |
| `Navigation` | พื้นที่เดินของซอมบี้/AI และจำนวนสูงสุดต่อเขต | `Environment/Navigation_*.dat` | `LevelNavigation` | stream nav chunks พร้อม border links |
| `PlayerSurvival` | อาหาร น้ำ เลือด เชื้อ regen XP และการตาย | `Config.json` | `PlayerLife`, `PlayConfigData` | global algorithm + zone modifier ที่ระบุชัด |
| `ItemEconomy` | ตารางไอเท็ม จุดดรอป จำนวนเป้าหมาย respawn/despawn | `Spawns/Items.dat`, `Spawns/Jars.dat`, item/spawn assets, `Config.json` | `LevelItems`, `ItemManager` | cell-owned spawn registry และ global economy policy |
| `ZombieEcology` | ตารางซอมบี้ เสื้อผ้า loot จุดเกิด จำนวนและการเกิดซ้ำ | `Spawns/Zombies.dat`, `Spawns/Animals.dat`, nav data, `Config.json` | `LevelZombies`, `ZombieManager` | zone population profile + cell state |
| `AnimalEcology` | ตารางสัตว์ จุดเกิด จำนวนและ respawn | `Spawns/Fauna.dat`, animal assets, `Config.json` | `LevelAnimals`, `AnimalManager` | zone population profile + cell state |
| `VehicleTraffic` | ตารางรถ จุดเกิด respawn decay และรถไฟ | `Spawns/Vehicles.dat`, `Roads.*`, `Config.json`, vehicle assets | `LevelVehicles`, `VehicleManager` | global unique instances + zone spawn policy |
| `PlayerSpawn` | จุดเกิด ทิศ และ loadout | `Spawns/Players.dat`, `Spawn_Loadouts` | `LevelPlayers`, spawn pipeline | gateway/settlement spawn policy |
| `NPCQuestEconomy` | NPC dialogue quest vendor และ reward | NPC assets และ objects ในแมพ | asset/NPC managers | global ID registry และ migration table |
| `BuildablePersistence` | decay, armor, placement และ save ownership | `Config.json`, `Level/Buildables.dat` | `BarricadeManager`, `StructureManager` | persistence keyed by world cell |
| `Events` | airdrop, weather, full moon และ event เฉพาะแมพ | `Config.json`, level/assets | event managers | event scheduler ระดับโลก + zone rules |
| `ContentDependencies` | item/object/effect/music/resource assets | map bundles, workshop dependencies | `Assets` | global GUID registry และ unload references |
| `PerformanceProfile` | batching, static volumes, clutter และ texture budget | `Config.json`, hierarchy/foliage | level loading/rendering | per-cell budgets, HLOD และ visibility |

## ความหมายของไฟล์ spawn ที่ชื่อชวนสับสน

| ไฟล์ | ความหมายจริงจาก source code |
|---|---|
| `Spawns/Items.dat` | นิยามตารางไอเท็ม: สี ชื่อ table ID tiers chance และ item IDs |
| `Spawns/Jars.dat` | จุดเกิดไอเท็ม แบ่งเป็นกริด region; แต่ละจุดมีชนิดตารางและพิกัด |
| `Spawns/Zombies.dat` | นิยามตารางซอมบี้: mega, health, damage, loot, XP, difficulty และเสื้อผ้า |
| `Spawns/Animals.dat` | **จุดเกิดซอมบี้** แบ่งตาม region ไม่ใช่สัตว์ |
| `Spawns/Fauna.dat` | ตารางสัตว์และจุดเกิดสัตว์ |
| `Spawns/Vehicles.dat` | ตารางรถและจุดเกิดรถ พร้อมมุมหัน |
| `Spawns/Players.dat` | จุดเกิดผู้เล่น พิกัด มุม และ alternate flag |

หลักฐานการอ่านไฟล์อยู่ใน `LevelItems.cs`, `LevelZombies.cs`, `LevelAnimals.cs`, `LevelVehicles.cs` และ `LevelPlayers.cs` ภายใต้ `Assets/Runtime/Assembly-CSharp/Unturned/Level/`

## กลไกที่ยืนยันจาก source code

### การลดอาหารและน้ำ

`PlayerLife.cs` ลดอาหาร 1 หน่วยเมื่อเวลาจำลองผ่าน `Food_Use_Ticks` และลดน้ำ 1 หน่วยเมื่อผ่าน `Water_Use_Ticks` สกิล Survival เพิ่มช่วงเวลา ส่วนพื้นที่หิมะเร่งการลดอาหารโดยมีสกิล Warmblooded ช่วยบรรเทา เมื่อค่าเป็นศูนย์จะได้รับความเสียหายตาม `Food_Damage_Ticks` หรือ `Water_Damage_Ticks`

### การเกิดไอเท็ม

`ItemManager.cs` คำนวณจำนวนเป้าหมายของแต่ละ region เป็น:

```text
target item count = spawn point count in region × Items.Spawn_Chance
```

ระบบเลือกจุดสุ่มที่ไม่อยู่ใน safezone และไม่ทับไอเท็มเดิม แล้วเติมกลับเมื่อผ่าน `Items.Respawn_Time` ไอเท็มที่ผู้เล่นทำตกและไอเท็มที่เกิดตามธรรมชาติใช้เวลา despawn คนละค่าได้

### การเกิดซอมบี้

`ZombieManager.cs` คำนวณจำนวนรวมจากจำนวนจุดเกิดทั้ง level คูณ `Zombies.Spawn_Chance` แล้วจำกัดซ้ำด้วยเพดานของ navigation region การเกิดซ้ำใช้ `Respawn_Day_Time`; เมื่อ full moon ใช้ `Respawn_Night_Time` และ beacon ใช้ค่าของ beacon

### ข้อจำกัดระบบแมพเดิม

- มี `Level.info` ตัวเดียวเป็น active map identity
- โหลด scene `Game` แบบปกติ ไม่ใช่ additive world zones
- หลาย manager จอง array ขนาด `64 × 64` regions
- `Regions` ผูกพิกัดโลกไว้กับช่วงประมาณ `-4096..4096` และ region ขนาด 128
- มีการ `Resources.UnloadUnusedAssets()` ตาม lifecycle ของ level

เพราะฉะนั้นการนำแมพมา “วางต่อกัน” โดยคัดลอกโฟลเดอร์อย่างเดียวจะชนทั้งพิกัด global, singleton state, region ownership, lighting, spawn tables และ asset lifetime

## ระดับการย้ายระบบ

แต่ละค่าในเอกสารรายแมพต้องถูกจัดเป็นหนึ่งในสี่กลุ่มก่อน implement:

1. `Preserve` — รักษาพฤติกรรมเฉพาะเขต
2. `Normalize` — ใช้กฎกลางของโลกใหม่
3. `Blend` — เปลี่ยนค่าตามระยะรอยต่อ เช่น fog/weather/audio
4. `Replace` — เลิกใช้พฤติกรรมเก่าและใช้ระบบใหม่ เช่น global event scheduler
