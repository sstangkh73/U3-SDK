# Limestone — Item Economy

## Data ownership

| ไฟล์/asset | หน้าที่ |
|---|---|
| `Spawns/Items.dat` | ตาราง loot: 1,189 bytes |
| `Spawns/Jars.dat` | จุดเกิด loot แบ่งตาม region: 55,487 bytes |
| `I/` และ `Spawn_Tables/` | item และ spawn assets ที่รวมมากับ workshop map |
| `Config.json` | bullets, respawn/despawn และ difficulty quality |

Limestone ต่างจาก California ตรงที่ asset packs หลักอยู่ใต้ workshop item ของแมพเอง ไม่ได้ประกาศ `RequiredWorkshopFileIds` แยกใน config ที่ตรวจพบ

## Overrides

| ค่า | Limestone | ผล |
|---|---:|---|
| `Items.Spawn_Chance` | inherited | ความหนาแน่นรับจาก difficulty/server |
| `Gun_Bullets_Multiplier` | 1 | ตัวคูณกระสุนปืน |
| `Magazine_Bullets_Multiplier` | 1 | ตัวคูณกระสุนแม็ก |
| `Despawn_Dropped_Time` | 1080 s | ของผู้เล่นทำตกหายหลัง 18 นาที |
| `Despawn_Natural_Time` | 1080 s | ของเกิดธรรมชาติหายหลัง 18 นาที |
| `Respawn_Time` | 100 s | พยายามเติมของเร็วกว่า California |
| Easy quality full chance | 1 | Easy ได้ไอเท็มคุณภาพเต็มทั้งหมดตาม override |
| Normal/Hard quality full chance | 0 | ไม่บังคับคุณภาพเต็ม |

## Item content footprint

ประเภทเด่น: Barricade 214, Food 67, Gun 93, Magazine 83, Water 23, Medical 22, Melee 44, Storage 38, Structure 78 และ Supply 77 definitions

## ความเสี่ยงตอนรวมโลก

- Limestone respawn 100 วินาที ขณะที่ California 240 วินาที ถ้าใช้ economy global ตรงๆ จะเปลี่ยน scarcity ของหนึ่งในสองแมพ
- quality/bullet state ขึ้นกับ difficulty-specific overrides ต้อง import เป็น policy layer ไม่ฝังค่าไว้ใน spawn record
- `Spawn_Chance` ยัง inherited; snapshot ต้องบันทึก effective server/difficulty value ตอนเทียบ parity
- cell unload/reload ต้องรักษาเวลา respawn/despawn และ origin ของ item
