# ADR-001: ใช้ข้อมูลดิบของแมพกับ runtime ใหม่ทั้งหมด

- Status: Accepted
- Date: 2026-07-14

## Context

แมพแต่ละยุคมี geometry, spawn data, assets และ environment ที่ต้องรักษา แต่ gameplay overrides เดิมไม่สม่ำเสมอและบางค่ามี compatibility quirks เช่นชนิดข้อมูลไม่ตรงกับค่าใน JSON การพยายามรวม `Config.json` ของทุกแมพจะทำให้โลกเดียวมีหลายกฎที่ชนกันและแก้ยาก

## Decision

โลกใหม่จะนำเข้าเฉพาะ **map-authored content/data** และเขียนระบบ runtime ใหม่ทั้งหมด

### นำเข้า

- terrain/landscape, holes, water และ roads/paths
- objects, hierarchy, foliage และ baked buildables
- environment, lighting, fog, oxygen, ambience และ weather identity
- navigation data และ zone boundaries
- item, zombie, animal, vehicle และ player spawn tables/points
- NPC, dialogue, quest, vendor และ asset references
- level identity, dependency IDs, train associations และ map images

### ไม่นำมาเป็นกฎ runtime

- food/water/virus rates
- item spawn chance, respawn/despawn และ quality
- zombie density, damage, armor, specials, loot และ respawn
- animal/vehicle caps และ timing
- difficulty-specific gameplay overrides
- buildable decay/armor, airdrop timing และ persistence rules

ค่า legacy เหล่านี้ยังถูกบันทึกเป็น **reference-only metadata** เพื่อเปรียบเทียบและช่วย balance แต่ไม่มีสิทธิ์เปลี่ยน runtime ใหม่

## New runtime ownership

ระบบใหม่ต้องมี policy กลางที่ versioned และทดสอบได้สำหรับ:

- player survival และ difficulty
- item economy
- zombie/animal population
- vehicles และ traffic
- world events
- buildable/entity persistence
- cell streaming, navigation links และ environment blending

Zone สามารถมี modifier ใหม่ได้ภายหลัง แต่ modifier ต้องประกาศใน schema ใหม่ ไม่อ่านจาก legacy config โดยอัตโนมัติ

## Import policy

- importer เป็น read-only ต่อโฟลเดอร์ Steam install/Workshop
- repo เก็บ source catalog และ derived manifests แต่ไม่คัดลอก copyrighted map bundles/assets เข้ามา
- manifest บันทึก path, file size, category, dependency summary และ fingerprint
- binary records จะถูกแปลงเป็น schema ใหม่โดย importer รุ่นต่อไป ไม่แก้ไฟล์ต้นฉบับ
- ทุก entity ที่นำเข้าต้องได้ stable world/zone/cell ID ก่อนใช้ persistence

## Consequences

ข้อดีคือ balance และ behavior เป็นระบบเดียว แก้/ทดสอบได้ และไม่ต้องรักษาบั๊กจากหลายยุค ข้อแลกเปลี่ยนคือช่วงแรกยังเล่นเหมือนแมพเดิมไม่ได้จนกว่าระบบใหม่จะครบ และต้องสร้าง importer/validator สำหรับ binary formats ทีละชนิด

## First implementation slice

1. source catalog ของแมพที่ติดตั้ง
2. read-only inventory manifest generator
3. binary decoders สำหรับ level/spawn data
4. zone/cell conversion
5. runtime ใหม่ เริ่มจาก terrain/objects ก่อน population/gameplay

## Implementation status

Inventory manifest generator และ source catalog ถูก implement แล้วใน `Assets/Editor/Assembly-CSharp-Editor/WorldUpgrade/` พร้อม runtime manifest schema ใน `Assets/Runtime/Assembly-CSharp/Unturned/WorldUpgrade/` ผลลัพธ์ของแมพที่ติดตั้งอยู่ใน `Builds/WorldUpgrade/RawMapManifests/`
