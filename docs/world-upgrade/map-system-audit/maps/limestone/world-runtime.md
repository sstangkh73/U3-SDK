# Limestone — World Runtime

## Geometry, objects และ navigation

| ส่วน | Limestone |
|---|---:|
| Landscape | 22 heightmaps, 22 splatmaps, 20 holes |
| Navigation | 34 chunks |
| Paths | 89,853 bytes |
| Roads metadata/bundle | 342 / 389,040 bytes |
| Objects | 2,252,173 bytes |
| Buildables baked into level | 18,619 bytes |
| Hierarchy | 2,096,000 bytes |
| Foliage | 310,747,681 bytes |
| Ambience bundle | 1,871,728 bytes |
| Gravity | -12 |

แม้เป็น Medium แต่ `Foliage.blob` ใหญ่กว่า California จึงห้ามตั้ง streaming budget จาก level size อย่างเดียว

## Vehicles และ trains

| ค่า | Limestone |
|---|---:|
| Vehicle spawn file | 1,815 bytes |
| Vehicle semantic definitions | 19 |
| Max instances Medium | 40 |
| Respawn time | 450 s |
| Decay time | 432,000 s = 5 วัน |
| Decay damage per second | 200 |

รถไฟ 6 รายการ:

| Vehicle ID | Road index | Placement min/max |
|---:|---:|---:|
| 21507 | 523 | 0 / 0 |
| 21506 | 484 | 0.2 / 0.2 |
| 21508 | 486 | 0.2 / 0.2 |
| 21500 | 4 | 0.2 / 0.2 |
| 21522 | 536 | 0.2 / 0.2 |
| 21501 | 10 | 0.2 / 0.2 |

## Events

| ค่า | Limestone |
|---|---:|
| Airdrop frequency min/max | 2 / 4 |
| Airdrop speed | 50 |
| Airdrop force | -55 |
| Use airdrops | inherited |

ต่างจาก California ซึ่งปิด vanilla airdrops โดยตรง Limestone ไม่ได้ปิดระบบ แต่เปลี่ยน frequency/flight behavior

## Buildables และ resources

| ค่า | Limestone |
|---|---:|
| Barricade/structure decay | 432,000 s = 5 วัน |
| Armor low/high tier | 1 / 1 |
| Gun low/high damage multipliers | 1 / 1 |
| Item placement on vehicle | false |
| Trap placement on vehicle | false |
| No-build radius around spawn | 64 |
| Rubble/resource reset | 1 / 1 |

## Player spawn/loadout

- `Spawns/Players.dat`: 268 bytes
- Spawn loadout tables: `21541`, `22300`, `22301`, amount 1 แต่ละรายการ

## Performance profile

| ค่า | Limestone |
|---|---:|
| Batching version | 2 |
| Max batching texture | 512 |

โลกใหม่ต้อง profile foliage และ object hierarchy จริง แมพ Medium นี้อาจแพงกว่าที่ขนาดภูมิประเทศบอก
