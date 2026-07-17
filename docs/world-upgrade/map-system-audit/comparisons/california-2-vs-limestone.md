# California 2 เทียบ Limestone

ตารางนี้ใช้ตัดสินใจออกแบบระบบโลกใหม่ ไม่ได้ตัดสินว่าแมพไหน “ดีกว่า”

## ขนาดและโครงสร้าง

| ระบบ | California 2 | Limestone | ผลต่อโลกใหม่ |
|---|---:|---:|---|
| Size enum / units | Insane / 8192 | Medium / 2048 | California ต้องแบ่ง cell มากกว่า; ห้ามใช้หนึ่ง map = หนึ่ง cell |
| Heightmap files | 52 | 22 | importer ต้องอ่าน tile layout จริง |
| Navigation chunks | 52 | 34 | nav count ไม่แปรตาม size แบบตรงๆ |
| Objects.dat | 6.85 MB | 2.25 MB | California หนักกว่าใน objects |
| Foliage.blob | 286.15 MB | 310.75 MB | Limestone เล็กกว่าแต่ foliage หนักกว่า |
| Separate dependency pack | ใช่: `3711646503` | ไม่พบใน config | asset registry ต้องรองรับทั้งสองรูปแบบ |

## ผู้เล่นและ survival

| ค่า | California 2 | Limestone | ข้อเสนอ |
|---|---:|---:|---|
| Food use ticks | 700 | 1200 | เก็บ legacy profile; normalize ภายหลัง |
| Water use ticks | 700 | 1400 | เก็บ legacy profile; normalize ภายหลัง |
| Food/water damage ticks | inherited/inherited | 5/5 | ใช้ global survival policy ในโหมดใหม่ |
| Virus use ticks | 256 | 900 | เก็บ legacy profile |
| Virus damage ticks | 0.1 → เสี่ยงถูกแปลงเป็น 0 | 5 | replace ด้วยชนิดข้อมูลและหน่วยที่ชัด |
| Detect radius multiplier | 1 | 0.9 | อาจ preserve เป็น zone stealth identity |
| Ray aggressor distance | 12 | 3 | ต้องตรวจความหมาย gameplay ก่อน normalize |
| Difficulty-specific rules | ไม่มี | มี Easy/Normal/Hard จำนวนมาก | difficulty ควรเป็น session policy ไม่เปลี่ยนตาม zone |

## Loot economy

| ค่า | California 2 | Limestone | ข้อเสนอ |
|---|---:|---:|---|
| Item spawn chance | inherited | inherited | snapshot effective value ตอนทดสอบ parity |
| Item respawn | 240 s | 100 s | preserve ช่วงแรก; balance เป็น zone economy ภายหลัง |
| Dropped/natural despawn | 1080/1080 s | 1080/1080 s | normalize ได้ |
| Quality full | 0.25 ทุก difficulty จาก common override | Easy 1, Normal/Hard 0 | ย้ายเป็น difficulty/economy policy |
| Item point file size | 183,576 B | 55,487 B | California มีข้อมูลมากกว่า แต่ต้อง decode point count |

## Zombies และ animals

| ค่า | California 2 | Limestone | ข้อเสนอ |
|---|---:|---:|---|
| Zombie spawn chance | 0.28 | 0.325 | preserve zone density แล้วครอบด้วย active-cell cap |
| Zombie armor | 0.75 | 1 | preserve ช่วง parity |
| Zombie damage | 0.9 | Easy .75, Normal/Hard 1 | session difficulty + zone modifier |
| Special chances | inherited | acid/flanker/burner = 0 | preserve Limestone ecosystem identity |
| Day/full-moon respawn | inherited | 660/330 s | event-aware zone population profile |
| Mega drops | min 7, max inherited | 1/1 | California ต้อง runtime-test merge edge case |
| Animal max | 30 for Insane | 40 for Medium | replace ด้วย per-cell ecosystem budget |
| Animal respawn | 300 s | inherited | profile ต้องบันทึก source/inheritance |

## รถ, event และ buildables

| ค่า | California 2 | Limestone | ข้อเสนอ |
|---|---:|---:|---|
| Vehicle max | 50 Insane | 40 Medium | global vehicle registry + zone target |
| Vehicle respawn | inherited | 450 s | preserve source metadata |
| Vehicle decay | 5 วัน | 5 วัน | normalize ได้ |
| Trains | 2 | 6 | road index ต้อง remap เป็น global route ID |
| Vanilla airdrops | ปิด | inherited/เปิดตาม session | replace ด้วย world event scheduler |
| Airdrop speed/force | 95 / 9.5 | 50 / -55 | preserve event profile จนเข้าใจ coordinate/physics |
| Buildable decay | 7 วัน | 5 วัน | เลือก global persistence policy |
| Place item/trap on vehicle | inherited | false/false | session rule ไม่ควรสลับตาม boundary |

## Content footprint

| Semantic type | California 2 | Limestone |
|---|---:|---:|
| Food | 193 | 67 |
| Water | 44 | 23 |
| Gun | 93 | 93 |
| Magazine | 62 | 83 |
| Barricade | 426 | 214 |
| Structure | 107 | 78 |
| NPC | 144 | 2 |
| Dialogue | 261 | 25 |
| Quest | 156 | 13 |
| Vendor | 22 | 13 |
| Vehicle | 48 | 19 |

จำนวนนี้สะท้อน definition ที่พบ ไม่ใช่จำนวนของที่วางหรือใช้จริง

## Decision matrix v1

| Component | California 2 | Limestone | Decision |
|---|---|---|---|
| Terrain/objects/roads | preserve | preserve | import แล้ววางด้วย zone transform |
| Lighting/weather/ambience | blend | blend | transition band |
| Survival rates | compatibility profile | compatibility profile | algorithm กลาง + explicit profile |
| Difficulty | inherited | map-specific | normalize เป็น session/world rules |
| Loot points/tables | preserve | preserve | cell-owned economy |
| Item timing | preserve ชั่วคราว | preserve ชั่วคราว | balance หลัง parity |
| Zombie tables/points | preserve | preserve | cell population budget |
| NPC/quest/vendor | preserve | preserve | global GUID registry |
| Airdrops | custom/disabled | vanilla-derived | replace ด้วย global scheduler |
| Save/persistence | replace | replace | world/cell/entity IDs |

## สิ่งที่ยังต้องวัดก่อน implement

1. decode จำนวน spawn points/tables/tiers จริงจาก binary files
2. สร้าง effective-config snapshots สำหรับ Easy/Normal/Hard หลัง server overrides
3. ตรวจว่า California `Virus_Damage_Ticks 0.1` และ mega drop min/max ทำงานจริงอย่างไรใน runtime
4. hash/collision report ของ GUID, legacy IDs, spawn tables, quests และ vendors
5. วัด memory/CPU ของ terrain, foliage, objects และ nav ต่อ tile
6. ระบุ route/border candidates จากภาพและพิกัดจริงก่อนวาง layout โลก
