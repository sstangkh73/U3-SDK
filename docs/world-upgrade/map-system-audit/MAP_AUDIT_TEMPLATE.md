# `<Map Name>` — Map System Audit Template

ใช้ template นี้กับแมพทุกแมพที่เพิ่มภายหลัง ห้ามลบหัวข้อเพียงเพราะไม่พบ override ให้เขียน `inherited`, `not present` หรือ `pending decode` เพื่อแยก “ไม่มี” ออกจาก “ยังไม่ได้ตรวจ”

## Identity

| Field | Value | Evidence/status |
|---|---|---|
| Folder/workshop ID | | |
| Version | | |
| Level.dat format | | |
| Size/type | | |
| Asset GUID | | |
| Required workshop files | | |
| Legacy flags | | |

## World geometry/environment

- landscape tile counts: height/splat/hole
- objects, hierarchy, foliage, buildables sizes
- roads/paths/trains
- water, lighting, fog, oxygen, snow, ambience และ weather
- navigation chunk count และ flag ownership
- batching/static volume/clutter settings

## Player survival/gameplay

- food/water use และ damage intervals
- virus/bleeding/health regen
- XP/skills/death loss/save policy
- movement, combat, HUD และ difficulty overrides
- ระบุทุกค่าที่ inherited และลำดับการ merge

## Item economy

- item table และ point files
- spawn chance, respawn/despawn, quality, bullets
- safezone/event interactions
- item/spawn asset dependencies
- effective-config snapshot ที่ใช้ทดสอบ

## Population

- zombie tables/points/nav limits/specials/loot/respawn
- animal tables/points/caps/respawn
- NPC/dialogue/quest/vendor definitions และ placements
- point/table counts จาก binary importer

## Vehicles/player spawn/buildables/events

- vehicle tables/points/caps/respawn/decay
- train vehicle/road associations
- player points/loadouts
- buildable decay/armor/placement/no-build zones
- airdrop/weather/custom event behavior

## Content inventory

- semantic definitions แยกจาก localization/auxiliary files
- GUID และ legacy ID collision report
- asset usage/dependency graph
- shared asset ownership และ unload lifetime

## Migration decisions

กำหนดทุก component เป็น `Preserve`, `Normalize`, `Blend` หรือ `Replace` พร้อมเหตุผลและ test ที่ต้องผ่าน

## Evidence and gaps

- source file/line anchors
- installed content paths
- `Confirmed`, `Derived`, `Inherited`, `Pending runtime verification`
- binary fields ที่ยังไม่ decode
- runtime/performance tests ที่ยังไม่ทำ
