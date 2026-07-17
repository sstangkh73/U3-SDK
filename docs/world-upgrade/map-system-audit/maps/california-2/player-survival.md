# California 2 — Player Survival และ Gameplay

แหล่งข้อมูลหลักคือ `California2/Config.json` และกลไกใน `PlayerLife.cs`

## Survival overrides

| ค่า | California 2 | ความหมายที่ source ยืนยัน |
|---|---:|---|
| `Food_Use_Ticks` | 700 | เมื่อครบช่วงนี้อาหารลด 1 หน่วย; สกิล Survival ทำให้ช้าลง และหิมะทำให้เร็วขึ้น |
| `Water_Use_Ticks` | 700 | เมื่อครบช่วงนี้น้ำลด 1 หน่วย; สกิล Survival ทำให้ช้าลง |
| `Food_Damage_Ticks` | inherited | เมื่ออาหารเป็น 0 ใช้ค่าจาก difficulty/server |
| `Water_Damage_Ticks` | inherited | เมื่อน้ำเป็น 0 ใช้ค่าจาก difficulty/server |
| `Virus_Infect` | 5 | ต่ำกว่า 5 จึงเริ่มลด virus ตามช่วงใช้ |
| `Virus_Use_Ticks` | 256 | ช่วงเวลาลด virus 1 หน่วยเมื่ออยู่ต่ำกว่าเกณฑ์ |
| `Virus_Damage_Ticks` | `0.1` ใน JSON | มี compatibility quirk ด้านล่าง |
| `Bleed_Damage_Ticks` | 21 | ช่วง damage จากเลือดไหล |
| `Can_Stop_Bleeding` | false | เลือดไม่หยุดเองจาก config นี้ |
| `Detect_Radius_Multiplier` | 1 | ระยะตรวจจับฐาน |
| `Ray_Aggressor_Distance` | 12 | ระยะ ray aggressor override |

## Compatibility quirk: `Virus_Damage_Ticks`

ฟิลด์ `PlayersConfigData.Virus_Damage_Ticks` เป็น `uint` แต่ California ใส่ `0.1` ตัวโหลดใน `Provider.applyLevelConfigOverride` ใช้ `Convert.ToUInt32` จึงมีแนวโน้มแปลงเป็น `0` จากนั้น `PlayerLife` ตรวจ `simulation - lastInfect > 0` และทำ damage 1 หน่วย

สถานะ: **Confirmed by code path, pending runtime verification** ต้องเก็บ `0.1` เป็น legacy source value แต่ห้ามย้ายเข้า schema ใหม่ที่เป็น integer โดยไม่กำหนดความตั้งใจใหม่ เพราะอาจเป็นบั๊กหรือเป็นเทคนิคให้ผู้เล่นตายเร็วเมื่อเชื้อเป็นศูนย์

## XP, skill และความตาย

| ค่า | California 2 |
|---|---:|
| Experience multiplier | 0 |
| Lose skills PvP/PvE | 1 / 1 |
| Lose skill levels PvP/PvE | 0 / 0 |
| Lose experience PvP/PvE | 0.9 / 0.9 |
| Spawn with max skills | false |
| Per-character saves | false |

## Gameplay

| ค่า | California 2 |
|---|---:|
| Chart | true |
| Ballistics | true |
| Group map | true |
| Home timer | 20 |
| Exit timer | 15 |
| 2D scope overlay | false |

California ไม่กำหนด difficulty-specific overrides ใน `Config.json` ดังนั้นค่าอื่น เช่น health/food เริ่มต้น, item spawn chance, damage ตอนหิว และความยากซอมบี้ จะถูกสืบทอดจาก difficulty/server config

## นโยบายโลกใหม่

- เก็บ 700/700 ใน `California2LegacySurvivalProfile` สำหรับโหมด parity
- ค่า starvation/dehydration/virus damage ใช้ global validated profile ในโหมดโลกใหม่
- zone modifier ต้องเปลี่ยนที่ boundary ที่ประกาศไว้ ไม่ควรเปลี่ยน food timer กลาง tick โดยไม่มี state migration
- แยก “ค่าจากแมพ” ออกจาก “ค่าที่ server override” เพื่อ debug ได้ว่า runtime ใช้อะไรจริง
