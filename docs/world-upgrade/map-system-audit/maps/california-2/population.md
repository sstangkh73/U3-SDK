# California 2 — Zombie, Animal, NPC และ Population

## Zombies

| ข้อมูล | California 2 |
|---|---:|
| Zombie table file | `Spawns/Zombies.dat` — 5,635 bytes |
| Zombie point file | `Spawns/Animals.dat` — 85,127 bytes |
| Spawn chance | 0.28 |
| Slow movement | false |
| Armor multiplier | 0.75 |
| Damage multiplier | 0.9 |
| Minimum mega drops | 7 |
| Maximum mega drops | inherited |
| Day/night respawn | inherited |

จำนวนสูงสุดจริงไม่ใช่ 28% ของแต่ละ nav region โดยตรง โค้ดคำนวณ `ceil(total level zombie points × 0.28)` เป็น server cap แล้วใช้ค่าต่ำสุดระหว่าง cap นี้กับ `LevelNavigation.flagData[bound].maxZombies` ของ region

`Min_Mega_Drops = 7` แต่ไม่ได้ override `Max_Mega_Drops` จึงต้องตรวจ runtime config ที่ merge แล้ว ถ้า maximum ต่ำกว่า minimum behavior ของ `Random.Range(min, max + 1)` ต้องทดสอบก่อนรักษา parity

## Animals

| ข้อมูล | California 2 |
|---|---:|
| Animal table/point file | `Spawns/Fauna.dat` — 7,124 bytes |
| Max instances for Insane | 30 |
| Respawn time | 300 s |
| Animal files ใน dependency pack | 52 `.dat` files รวม localization; semantic asset count ต้องอ่าน GUID/type เพิ่ม |

## NPC, quest และ vendor

California 2 Assets มี semantic definitions ที่ตรวจจาก `Type`:

| Type | จำนวน |
|---|---:|
| NPC | 144 |
| Dialogue | 261 |
| Quest | 156 |
| Vendor | 22 |

จำนวน definition ไม่เท่ากับจำนวน NPC ที่วางอยู่ในโลก เพราะ placement อยู่ใน objects/hierarchy และบาง definition อาจเป็น dependency หรือไม่ได้ใช้งาน

## นโยบายโลกใหม่

- import spawn table และ spawn point เป็นคนละ entity ตามความหมายไฟล์จริง
- สร้าง stable `PopulationZoneId` และ `SpawnPointId` ไม่ใช้ byte table index เป็น global identity
- เก็บ clothing/loot/difficulty reference ของ zombie table ครบ
- เปลี่ยน total-level cap เป็น active-cell budget มิฉะนั้นการเปิดหลายแมพพร้อมกันจะเพิ่ม population แบบควบคุมไม่ได้
- NPC/quest/vendor ใช้ GUID registry กลาง พร้อม migration table สำหรับ legacy ushort IDs
