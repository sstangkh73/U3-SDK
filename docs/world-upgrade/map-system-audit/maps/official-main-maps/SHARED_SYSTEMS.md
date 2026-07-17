# Official Main Maps — Shared System Ownership

## ทำไมระบบ gameplay จึงต่างจาก curated maps

PEI, Washington, Yukon, Russia และ Germany ไม่มี map mode overrides ใน `Config.json` ที่ติดตั้งอยู่ ดังนั้น effective behavior ต้องประกอบจาก:

```text
PlayConfigData default ของ difficulty
  → server config overrides
  → ไม่มี map common override
  → ไม่มี map difficulty override
```

จึงห้ามบันทึกค่าจากเซิร์ฟเวอร์ที่ทดสอบครั้งหนึ่งว่าเป็น “ค่าของ PEI” หรือ “ค่าของ Russia”

## Component ownership

| Component | สิ่งที่เป็นของแมพ | สิ่งที่ inherited/shared |
|---|---|---|
| Player survival | สภาพแวดล้อม เช่น snow/temperature/oxygen volumes | food/water/virus rates, damage, XP และ death penalties |
| Item economy | item tables, point placements, safezones และ asset references | spawn chance, respawn/despawn, quality และ bullet multipliers |
| Zombie ecology | zombie tables, point placements, nav flags และ difficulty asset references | spawn chance, damage/armor, specials, loot และ respawn timing |
| Animal ecology | fauna tables/points และ asset references | max instances, damage/armor และ respawn timing |
| Vehicles | tables/points, roads และ train associations | caps, respawn, decay, damage และ battery policy |
| Player spawn | player points/directions | spawn-selection algorithm และ session rules |
| NPC/quests | placements/references ใน objects และ level asset | official global asset catalog และ managers |
| Buildables | baked buildables และ no-build volumes | decay, armor, placement และ persistence rules |
| Events | environmental setup และ route-compatible data | airdrop/weather event timers เว้นแต่ level asset เปลี่ยน |
| Rendering | landscape, hierarchy, foliage, ambience, nav และ static volumes | loader, batching implementation และ quality settings |

## นโยบายโลกใหม่

- import local map data ทุกชิ้น แต่ไม่สร้าง `PEISurvivalConfig`, `RussiaItemConfig` ฯลฯ ถ้าไม่มี source override
- snapshot effective global config แยกตาม Easy/Normal/Hard เพื่อใช้ parity test
- environment effects เช่น snow temperature เป็น zone data ส่วน accessibility/camera/server rules เป็น global policy
- official assets ต้องผ่าน usage graph จาก spawn tables, object references และ level assets ก่อนระบุ ownership ต่อ zone
- เมื่อ balance โลกใหม่ ค่ากลางหนึ่งชุดใช้กับ official zones ได้ก่อน แล้วเพิ่ม zone modifier เฉพาะเมื่อมีเหตุผล gameplay ชัดเจน
