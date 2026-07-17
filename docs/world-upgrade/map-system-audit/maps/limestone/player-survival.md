# Limestone — Player Survival, Difficulty และ Gameplay

Limestone กำหนดทั้ง override ทั่วไปและ override แยก Easy/Normal/Hard จึงต้องเก็บลำดับการ merge: base server mode → map common overrides → map difficulty overrides

## Survival overrides

| ค่า | Limestone | ความหมาย |
|---|---:|---|
| `Health_Regen_Ticks` | 45 | ช่วงฟื้น health เมื่อผ่านเงื่อนไข regen |
| `Food_Use_Ticks` | 1200 | อาหารลด 1 หน่วยตามช่วงนี้ |
| `Food_Damage_Ticks` | 5 | เมื่ออาหารเป็นศูนย์ damage ทุกช่วง 5 |
| `Water_Use_Ticks` | 1400 | น้ำลด 1 หน่วยตามช่วงนี้ |
| `Water_Damage_Ticks` | 5 | เมื่อน้ำเป็นศูนย์ damage ทุกช่วง 5 |
| `Virus_Use_Ticks` | 900 | ช่วงลด virus เมื่ออยู่ต่ำกว่าเกณฑ์ |
| `Virus_Damage_Ticks` | 5 | เมื่อ virus เป็นศูนย์ damage ทุกช่วง 5 |
| `Bleed_Damage_Ticks` | 21 | ช่วง damage จากเลือดไหล |
| `Can_Stop_Bleeding` | false | เลือดไม่หยุดเองจาก config นี้ |
| `Detect_Radius_Multiplier` | 0.9 | ระยะตรวจจับถูกลด 10% |
| `Ray_Aggressor_Distance` | 3 | ระยะ aggressor สั้นกว่า California |
| `Skillset_Reduces_Skill_Cost` | false | skillset ไม่ลดค่า skill |

เมื่อเทียบกลไกเดียวกัน Limestone ลดอาหารและน้ำช้ากว่า California อย่างมาก แต่เมื่อหมดจะเสีย health ด้วยช่วง 5 ที่ระบุชัด

## ค่าเริ่มต้นและความยาก

| ค่า | Easy | Normal | Hard |
|---|---:|---:|---:|
| Health default | 100 | 75 | 75 |
| Food default | 100 | 50 | 30 |
| Water default | 100 | 50 | 30 |
| Virus default | 100 | 90 | 80 |
| Health regen min food/water | 80/80 | 85/85 | 85/85 |
| Virus infect threshold | 31 | 51 | 71 |
| Quality full chance | 1 | 0 | 0 |
| Gun/mag/crate full chance | 0.25/0.25/0.25 | 0/0/0 | 0/0/0 |
| Zombie damage multiplier | 0.75 | 1 | 1 |
| Zombie backstab multiplier | 3 | 2 | 1 |
| Zombie can stun / slow | true/true | false/false | false/false |
| Group HUD / hitmarkers | true/true | false/false | false/false |
| Friendly fire | false | true | true |
| Home timer | 30 | 45 | 60 |

XP multiplier เป็น 0, lose skills PvP/PvE เป็น 1, lose experience เป็น 1 และ lose skill levels เป็น 0 ทุกโหมดจาก common override

## Movement/combat presentation

| ค่า | Limestone |
|---|---:|
| Ballistics | true |
| Damage flinch | false |
| Explosion camera shake | false |
| Air-strafing acceleration/deceleration | 0.08 / 0.08 |
| Third-person recoil/spread | 1.5 / 1.0 |
| Viewmodel aiming jump/land | 4.0 |
| Viewmodel aiming misalignment | 0.5 |

## นโยบายโลกใหม่

- ความยากเป็น world/session setting แต่ Limestone สามารถมี `ZoneChallengeProfile` ได้
- อย่าเปลี่ยน difficulty profile ทันทีตามการก้าวข้ามเส้น zone เพราะจะทำให้ค่าเริ่มต้น, friendly fire และ HUD เปลี่ยนแบบไร้เหตุผล
- เก็บ common survival rates เป็น parity preset; แยก combat/camera accessibility ออกจาก zone เพราะควรเป็น player setting ในระบบใหม่
