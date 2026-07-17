# California 2 — Item Economy

## Data ownership

| ไฟล์/asset | หน้าที่ |
|---|---|
| `Spawns/Items.dat` | ตาราง loot ของแมพ: 1,727 bytes |
| `Spawns/Jars.dat` | จุดเกิด loot แบ่งตาม region: 183,576 bytes |
| California 2 Assets `Spawns/` | spawn assets ที่ resolve ตารางไปยัง item IDs |
| California 2 Assets `Items/` | นิยามไอเท็มจริง |
| `Config.json` | quality, bullets, respawn และ despawn |

## Runtime behavior

กลไกกลางใน `ItemManager` ตั้งเป้าจำนวนไอเท็มแต่ละ region จากจำนวนจุดเกิดคูณ `Items.Spawn_Chance` แล้วสุ่มจุดที่ไม่ชน safezone/ไอเท็มเดิม ระบบไม่ได้รับประกันว่าทุกจุดมีของพร้อมกัน

## Overrides

| ค่า | California 2 | ผล |
|---|---:|---|
| `Items.Spawn_Chance` | inherited | ความหนาแน่นขึ้นกับ difficulty/server ไม่ได้ล็อกโดยแมพ |
| `Quality_Full_Chance` | 0.25 | โอกาสเกิดไอเท็มคุณภาพเต็ม 25% |
| `Despawn_Dropped_Time` | 1080 s | ของที่ผู้เล่นทำตกหายหลัง 18 นาที |
| `Despawn_Natural_Time` | 1080 s | ของเกิดตามธรรมชาติหายหลัง 18 นาที |
| `Respawn_Time` | 240 s | region พยายามเติมของทุก 4 นาทีเมื่อยังต่ำกว่าเป้า |
| `Crate_Bullets_Full_Chance` | 0.2 | โอกาสกระสุนกล่องเต็ม |
| `Crate_Bullets_Multiplier` | 0.6 | ตัวคูณกระสุนกล่อง |
| `Magazine_Bullets_Full_Chance` | 0.2 | โอกาสแม็กเต็ม |
| `Magazine_Bullets_Multiplier` | 1.0 | ตัวคูณกระสุนแม็ก |

## Item content footprint

ประเภทเด่นใน dependency pack: Barricade 426, Food 193, Gun 93, Magazine 62, Water 44, Medical 24, Melee 35, Storage 50, Structure 107 และ Supply 132 definitions ดู inventory เต็มแบบจัดกลุ่มใน [content-assets.md](content-assets.md)

## ความเสี่ยงตอนรวมโลก

- ID/GUID จาก asset packs ต้อง resolve ใน registry กลางก่อนโหลด spawn table
- `Spawn_Chance` ที่ inherited ห้าม freeze เป็นค่า California โดยไม่บันทึก difficulty ที่ใช้
- region เดิมเป็น array ของ active level; โลกใหม่ต้องเปลี่ยนเป็น cell key และรักษา `lastRespawn`
- unload cell ต้อง serialize ของที่ผู้เล่นทำตกแยกจาก natural spawn มิฉะนั้นเวลา despawn จะรีเซ็ต
- safezone และ event hook `onServerSpawningItemDrop` ต้องยังทำงานหลังย้าย
