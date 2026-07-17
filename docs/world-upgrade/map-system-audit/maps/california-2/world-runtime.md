# California 2 — World Runtime

## Geometry, objects และ navigation

| ส่วน | California 2 |
|---|---:|
| Landscape | 52 heightmaps, 55 splatmaps, 43 holes |
| Navigation | 52 chunks |
| Paths | 235,501 bytes |
| Roads metadata/bundle | 767 / 139,168 bytes |
| Objects | 6,845,125 bytes |
| Buildables baked into level | 8,661 bytes |
| Hierarchy | 6,082,665 bytes |
| Foliage | 286,152,041 bytes |
| Ambience bundle | 2,616,759 bytes |

แมพปิด legacy ground/water/clip borders และเปิด static volumes กับ clutter option จึงควร import landscape, water volumes และ hierarchy รุ่นใหม่ ไม่ควรพึ่ง legacy terrain เป็น source of truth

## Vehicles และ trains

| ค่า | California 2 |
|---|---:|
| Vehicle spawn file | 4,371 bytes |
| Vehicle semantic definitions ใน asset pack | 48 |
| Max instances Large / Insane | 40 / 50 |
| Battery min/max/chance | 1 / 1 / 1 |
| Armor multiplier | 1 |
| Decay time | 432,000 s = 5 วัน |
| Decay damage per second | 200 |

รถไฟที่ประกาศใน config:

- Vehicle `4124`, road index `215` — Oakland ไป Border
- Vehicle `4126`, road index `1006` — Cathedral ไป Quarry

road index เป็น local index ของ California ห้ามใช้เป็น global road ID ในโลกใหม่

## Events และ weather

| ค่า | California 2 |
|---|---:|
| Vanilla airdrops | false |
| Airdrop speed | 95 |
| Airdrop force | 9.5 |
| Weather frequency multiplier | 1.5 |

แมพปิด vanilla airdrop แม้ยังใส่ speed/force ไว้ จึงต้องตรวจว่ามี custom event/quest/asset ใช้ค่าหรือไม่ก่อนตัดออก

## Buildables และ resource reset

| ค่า | California 2 |
|---|---:|
| Barricade decay | 604,800 s = 7 วัน |
| Structure decay | 604,800 s = 7 วัน |
| Low/high tier armor | 1 / 1 |
| Rubble reset multiplier | 1 |
| Resource reset multiplier | 1 |

## Player spawn/loadout

- `Spawns/Players.dat`: 842 bytes
- Spawn loadout table `4267`, amount 1

## Performance profile

| ค่า | California 2 |
|---|---:|
| Batching version | 2 |
| Static volumes | true |
| Clutter option | true |
| Max batching texture | 128 |

โลกใหม่ต้องเปลี่ยน static volume และ batching ให้มี cell ownership; hierarchy เดิมถูกสร้างโดยสมมติว่า California เป็น active level เดียว
