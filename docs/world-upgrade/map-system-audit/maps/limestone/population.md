# Limestone — Zombie, Animal, NPC และ Population

## Zombies

| ข้อมูล | Limestone |
|---|---:|
| Zombie table file | `Spawns/Zombies.dat` — 4,289 bytes |
| Zombie point file | `Spawns/Animals.dat` — 24,339 bytes |
| Spawn chance | 0.325 |
| Armor multiplier | 1 |
| Loot chance | 1 |
| Acid / Flanker / Burner chance | 0 / 0 / 0 |
| Can target objects | true |
| Respawn day / full moon | 660 / 330 s |
| Mega drops min / max | 1 / 1 |

Easy ทำ zombie damage 0.75, stun ได้, เดินช้า และ backstab ×3; Normal/Hard damage/armor 1, stun ไม่ได้และเดินไม่ช้า โดย backstab ลดเป็น ×2/×1 ตามลำดับ

จำนวนสูงสุดจริงยังถูกจำกัดด้วย navigation flags เช่นเดียวกับ California และต้องนับจุดเกิดจริงผ่าน binary importer ในขั้นถัดไป

## Animals

| ข้อมูล | Limestone |
|---|---:|
| Animal table/point file | `Spawns/Fauna.dat` — 2,085 bytes |
| Max instances Medium | 40 |
| Damage multiplier | 1 |
| Armor multiplier | 1 |
| Respawn time | inherited |

## NPC, quest และ vendor

| Type | จำนวน |
|---|---:|
| NPC | 2 |
| Dialogue | 25 |
| Quest | 13 |
| Vendor | 13 |

จำนวนนี้เป็น semantic definitions ไม่ใช่จำนวน placement ในโลก

## นโยบายโลกใหม่

- รักษา identity ของ Limestone ว่าไม่มี acid/flanker/burner จาก common override จนกว่าจะออกแบบ ecosystem ใหม่
- population budget ต้องคำนวณตาม active cells ไม่ใช่ใช้ total level points ของหลายแมพรวมกัน
- full-moon respawn 330 s เป็น event modifier ของ population profile ไม่ใช่ global constant
- NPC/quest/vendor ต้องผ่าน GUID/legacy-ID collision report ก่อนโหลดพร้อม California
