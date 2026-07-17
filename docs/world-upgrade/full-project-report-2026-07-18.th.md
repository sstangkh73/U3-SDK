# รายงานฉบับเต็มโครงการ Unturned World Upgrade

## การเดินทางข้ามแมพ การนำเข้าข้อมูลดิบ และแผนพัฒนาโลกขนาดใหญ่

**วันที่จัดทำ:** 18 กรกฎาคม 2026  
**โครงการ:** U3-SDK / Unturned World Upgrade  
**ตำแหน่งโครงการ:** `C:/Unturned/work/U3-SDK`  
**สาขาที่กำลังพัฒนา:** `codex/world-travel-mvp`  
**สถานะเอกสาร:** รายงานสถานะตามหลักฐานใน source code, generated manifests และ runtime log ที่มีอยู่ในเครื่อง  

---

## บทสรุปผู้บริหาร

โครงการนี้มีเป้าหมายระยะยาวในการนำเอกลักษณ์และข้อมูลดิบของแมพ Unturned หลายแมพมาใช้ร่วมกันภายใต้ระบบโลกใหม่ โดยต้องการให้ผู้เล่นสามารถเดินทางระหว่างพื้นที่ต่าง ๆ ได้อย่างต่อเนื่อง และในขั้นสุดท้ายสามารถเดินหรือขับรถข้ามเขตโดยไม่ต้องออกจากเกมหรือเปลี่ยน save แยกเป็นคนละโลก

อย่างไรก็ตาม สถานะปัจจุบันต้องแบ่งเป็น **สองระบบที่ต่างกันอย่างชัดเจน** ดังนี้

1. **World Travel MVP** — มีโค้ดต้นแบบที่ใช้งานได้ใน singleplayer สำหรับเดินทางระหว่าง California 2 และ Limestone โดยบันทึกสถานะผู้เล่น ออกจากแมพเดิม โหลดแมพเป้าหมาย และย้ายผู้เล่นไปยังตำแหน่งที่กำหนด พร้อมหน้าจอ transition ปิดบังช่วงเปลี่ยน level ระบบนี้ผ่านการทดสอบจาก runtime log อย่างน้อย 3 เที่ยว แต่ยัง **ไม่ใช่โลก seamless** เพราะเกมยัง unload และ load แมพตามระบบเดิม
2. **World Upgrade / Seamless World** — มีการสำรวจระบบแมพ วางสถาปัตยกรรม สร้าง source catalog และ importer ระยะแรกที่อ่าน inventory ของไฟล์แมพแบบ read-only พร้อมสร้าง manifest ครบ 7 แมพแล้ว แต่ยังไม่ได้ decode terrain, objects, spawn points และ navigation ให้เป็น `WorldCell` และยังไม่มีระบบ streaming โลกจริง

ดังนั้น ข้อสรุปที่ตรงกับหลักฐานที่สุดคือ โครงการมี **ต้นแบบการเดินทางข้ามแมพที่พิสูจน์ flow ได้แล้ว** และมี **ฐานข้อมูล/เครื่องมือเตรียมข้อมูลสำหรับโลกใหม่** แต่ยังอยู่ช่วงต้นของการสร้าง runtime โลกเดียว งานส่วนที่ยากที่สุด ได้แก่ cell streaming, stable persistence, navigation ข้ามเขต, asset lifetime, floating origin, multiplayer และ performance ยังไม่ได้ implement

ก่อนเริ่มระยะ 0 งานทั้งหมดใน branch ปัจจุบันยังเป็น working tree ที่ **ไม่ได้ commit** จึงกำหนดให้การตรวจ scope, compile, สร้าง baseline commit และเผยแพร่ไปยัง fork เป็น gate แรกก่อนเริ่มแก้โครงสร้างขนาดใหญ่ รายละเอียดผลอยู่ใน `phase-reports/phase-0-baseline-2026-07-18.th.md`

---

## 1. ที่มาและปัญหาของโครงการ

Unturned รุ่นปัจจุบันออกแบบโดยมี active level หลักเพียงหนึ่งแมพในแต่ละครั้ง ระบบสำคัญจำนวนมากอ้างอิง `Level.info`, region grid, spawn tables, lighting, asset lifecycle และ save namespace ของแมพที่กำลังเปิดอยู่โดยตรง วิธีนำโฟลเดอร์ของหลายแมพมาวางต่อกันจึงไม่เพียงพอ เพราะจะเกิดปัญหาทั้งด้านพิกัด การถือครอง entity การโหลด asset ระบบเกิดของและซอมบี้ navigation รวมถึงการบันทึกสถานะ

แมพแต่ละแมพไม่ได้มี source code ของระบบ survival แยกจากกันทั้งหมด แต่เกิดจากองค์ประกอบสามชั้นร่วมกัน ได้แก่

1. กลไกกลางของเกม เช่น `PlayerLife`, `ItemManager`, `ZombieManager` และ `Level`
2. ค่าปรับแต่งของแมพและระดับความยากจาก `Config.json`
3. ข้อมูลและคอนเทนต์ของแมพ เช่น terrain, objects, roads, environment, spawn tables, spawn points, NPC และ asset dependencies

ถ้านำค่า legacy ของทุกแมพมารวมโดยตรง โลกเดียวจะมีกฎหลายชุดที่ชนกัน เช่น อัตราการลดอาหาร อัตราเกิดไอเท็ม ความหนาแน่นซอมบี้ และเวลา respawn โครงการจึงตัดสินใจเก็บ **ข้อมูลที่ผู้สร้างแมพวางไว้** แต่เขียน **runtime gameplay policy ใหม่แบบรวมศูนย์** แทนการนำกฎเดิมมาใช้โดยอัตโนมัติ

---

## 2. วิสัยทัศน์และเป้าหมาย

### 2.1 วิสัยทัศน์ระยะยาว

สร้างโลก Unturned ขนาดใหญ่ที่ประกอบด้วยหลาย zone โดยรักษารูปร่าง ภูมิประเทศ เมือง จุดสำคัญ บรรยากาศ spawn identity และคอนเทนต์เฉพาะแมพ แต่ใช้ระบบ runtime กลางที่ควบคุมการโหลดโลก population, economy, survival, events และ persistence อย่างสม่ำเสมอ

### 2.2 เป้าหมายระยะสั้น

- พิสูจน์ว่าผู้เล่นสามารถเดินทางจากแมพหนึ่งไปอีกแมพหนึ่งและคงสถานะสำคัญได้
- สร้าง transition ที่ทำให้การเปลี่ยนแมพเข้าใจง่ายและลดความรู้สึกว่ากลับไปหน้าเมนู
- สำรวจโครงสร้าง source code และไฟล์แมพจริงก่อนออกแบบ schema ใหม่
- สร้างรายการแหล่งข้อมูลของแมพที่ติดตั้ง และสร้าง derived manifest โดยไม่คัดลอก asset ต้นฉบับเข้า repository

### 2.3 เป้าหมายระยะกลาง

- Decode ข้อมูล binary ของ terrain, objects, spawn tables และ spawn points
- แปลงข้อมูลของหนึ่งแมพเป็น `ZoneDefinition` และ `WorldCell`
- เปิด California 2 ผ่าน runtime ใหม่เพียง zone เดียวให้มี behavior เทียบกับระบบเดิมได้
- เพิ่ม Limestone เป็น zone ที่สอง และทดสอบการโหลด/ถอด cell ระหว่างพื้นที่

### 2.4 เป้าหมายระยะยาว

- เดินหรือขับรถข้าม zone โดยไม่ reload scene และไม่ teleport
- ให้ terrain, collision, objects และ nav พร้อมก่อนผู้เล่นถึงขอบ cell
- เก็บ dropped items, vehicles, buildables และ dynamic entities โดยใช้ stable world/cell/entity IDs
- รองรับ environment blending, world origin shifting และ asset reference counting
- เพิ่มแมพอื่นทีละแมพหลัง California 2 และ Limestone ผ่านเกณฑ์ความถูกต้องและประสิทธิภาพ
- ออกแบบ multiplayer/server authority หลัง singleplayer architecture มีเสถียรภาพ

---

## 3. หลักการออกแบบและขอบเขตข้อมูล

### 3.1 ข้อมูลที่จะนำเข้า

- terrain, landscape, holes, water และ roads/paths
- objects, hierarchy, foliage และ baked buildables
- lighting, fog, weather, ambience, oxygen และ environment identity
- navigation data และ zone boundaries
- item, zombie, animal, vehicle และ player spawn tables/points
- NPC, dialogue, quests, vendors และ asset references
- level identity, dependency IDs, train associations และภาพแผนที่

### 3.2 ข้อมูล legacy ที่เก็บเพื่ออ้างอิง แต่ไม่ใช้เป็น runtime rule อัตโนมัติ

- food, water และ virus rates
- item spawn chance, respawn, despawn และ quality
- zombie density, damage, armor, specials, loot และ respawn
- animal/vehicle caps และ timing
- difficulty-specific gameplay overrides
- buildable decay/armor, airdrop timing และ persistence rules

ค่ากลุ่มนี้ยังมีประโยชน์ในการวิเคราะห์เอกลักษณ์และใช้เป็นข้อมูลอ้างอิงตอน balance แต่ modifier ของโลกใหม่ต้องประกาศใน schema ใหม่อย่างชัดเจนเท่านั้น

### 3.3 ขอบเขตด้านไฟล์และลิขสิทธิ์

- Importer อ่านโฟลเดอร์ Steam และ Workshop แบบ read-only
- Repository เก็บ source catalog, metadata และ derived manifests
- ไม่คัดลอก copyrighted map bundles หรือ asset ต้นฉบับเข้า repository
- การแจกจ่าย build หรือข้อมูลจาก Workshop ในอนาคตต้องตรวจสิทธิ์และเงื่อนไขของเจ้าของแต่ละแมพอีกครั้ง

---

## 4. สถาปัตยกรรมที่มีอยู่ในปัจจุบัน

### 4.1 World Travel MVP

ระบบปัจจุบันใช้ flow ต่อไปนี้

```text
ผู้เล่นเข้าใกล้จุดเชื่อม
        ↓
กดปุ่ม interact
        ↓
จับภาพหน้าจอและแสดง transition
        ↓
SaveManager.save()
        ↓
คัดลอกไฟล์สถานะผู้เล่นไป namespace ของแมพเป้าหมาย
        ↓
เขียน PendingTravel.json
        ↓
disconnect จากแมพต้นทาง
        ↓
หน้าเมนูเปิดแมพเป้าหมายอัตโนมัติ
        ↓
กำหนดตำแหน่งเกิดและมุมหันใหม่
        ↓
สร้างผู้เล่นสำเร็จ แล้วลบ PendingTravel.json
```

ไฟล์สถานะที่ต้นแบบคัดลอกมีดังนี้

- `Player.dat`
- `Anim.dat`
- `Clothing.dat`
- `Inventory.dat`
- `Life.dat`
- `Quests.dat`
- `Skills.dat`

การเชื่อมต่อกำหนดใน `Builds/Shared/WorldTravel.json` โดยปัจจุบันมีสองทิศทาง ได้แก่ California 2 → Limestone และ Limestone → California 2

ระบบมี loop guard ผ่าน `ResumeAttempts` เพื่อยกเลิกการเดินทางถ้ากลับมาหน้าเมนูซ้ำก่อนโหลดแมพเป้าหมายสำเร็จ และมีการสำรองไฟล์ปลายทางตามกลไก backup ของ savedata ก่อนเขียนสถานะชุดใหม่

### 4.2 Raw Map Importer

Importer ระยะแรกทำหน้าที่สำรวจไฟล์ ไม่ได้สร้างโลกที่เล่นได้ โดยอ่าน source root ของแต่ละแมพแล้วบันทึกข้อมูลต่อไปนี้

- level format, size, type และ world units
- map version, level asset GUID และ Workshop dependency IDs
- relative path, ขนาด และ category ของไฟล์
- category summaries
- inventory fingerprint แบบ SHA-256
- dependency root summary
- รายชื่อ key ของ legacy gameplay overrides เพื่อ audit
- flags ยืนยันว่าไม่คัดลอก source content และไม่ใช้ legacy gameplay rules

ข้อควรระวังคือ fingerprint ปัจจุบันคำนวณจาก **relative path และ file size** ไม่ได้ hash เนื้อหาเต็มของทุกไฟล์ จึงเหมาะกับการตรวจ inventory เปลี่ยนแบบเบื้องต้น แต่ไม่ใช่หลักฐานยืนยันความเหมือนของ byte content

### 4.3 สถาปัตยกรรมเป้าหมายของโลกเดียว

ระบบเป้าหมายเสนอหน่วยข้อมูลหลักดังนี้

```text
WorldManifest
  ├─ ZoneDefinition
  │    ├─ Zone profile
  │    └─ WorldCells[]
  └─ Transition zones

WorldCell
  ├─ terrain, holes และ water
  ├─ static objects และ HLOD
  ├─ nav chunk และ border links
  ├─ spawn records
  └─ persistent dynamic state
```

บริการหลักที่ต้องสร้างใหม่ประกอบด้วย `WorldOriginService`, `WorldCellStreamer`, `ZoneResolver`, `WorldPopulationService`, `WorldPersistenceService`, `EnvironmentBlender`, `WorldAssetRegistry` และ `TransitionSafetyService`

---

## 5. สิ่งที่ทำเสร็จแล้ว

### 5.1 การสำรวจระบบแมพ

จัดทำ system catalog และหลักฐานจาก source code สำหรับระบบหลัก ได้แก่ map identity, geometry, objects, environment, navigation, survival, item economy, zombie/animal population, vehicles, player spawn, NPC/quest, persistence, events, dependencies และ performance profile

สถานะ audit ปัจจุบันคือ

- California 2: audit รายละเอียด v1
- Limestone: audit รายละเอียด v1
- PEI, Washington, Yukon, Russia และ Germany: audit โครงสร้างและ system ownership v1
- California 2 Assets: inventory dependency แล้ว
- แมพ legacy/non-standard อื่น เช่น Alpha Valley, Destruction และ Monolith ยังรอจัดประเภทและ audit

### 5.2 World Travel MVP

สิ่งที่ implement แล้ว ได้แก่

- manifest ของจุดเชื่อมแบบ JSON
- ตรวจจับผู้เล่นใน trigger radius
- prompt ให้กด interact
- ป้องกันการเดินทางขณะผู้เล่นอยู่ในรถ
- บันทึกเกมก่อนย้ายแมพ
- คัดลอกสถานะผู้เล่นไป target-map namespace
- pending hand-off record สำหรับข้าม scene/menu
- เปิด singleplayer target map อัตโนมัติ
- override spawn point และ yaw ที่ปลายทาง
- transition overlay และภาพจาก source frame
- ยกเลิก pending state เมื่อโหลดผิดพลาดหรือเกิด loop
- editor-only F8 shortcut สำหรับทดสอบการเดินทาง

### 5.3 Raw Map Importer และ manifests

สร้าง manifest สำเร็จครบ 7 แมพ รวมข้อมูล source 8,125 ไฟล์ ขนาดรวมประมาณ 2.29 GiB โดยไม่คัดลอกไฟล์ต้นทางเข้า repository

| Zone | ประเภท | จำนวนไฟล์ใน map root | Dependency summary | สถานะ |
|---|---|---:|---:|---|
| California 2 | Workshop | 272 | 1 | สร้าง manifest แล้ว |
| Limestone | Workshop | 7,179 | 0 | สร้าง manifest แล้ว |
| PEI | Official | 106 | 0 | สร้าง manifest แล้ว |
| Washington | Official | 106 | 0 | สร้าง manifest แล้ว |
| Yukon | Official | 102 | 0 | สร้าง manifest แล้ว |
| Russia | Official | 216 | 0 | สร้าง manifest แล้ว |
| Germany | Official | 144 | 0 | สร้าง manifest แล้ว |

Limestone มี asset content จำนวนมากอยู่ภายใน map root จึงมีจำนวนไฟล์สูง ส่วน California 2 แยก dependency pack ออกจาก root ของแมพ

### 5.4 เอกสารการตัดสินใจ

มี ADR ยืนยันแนวทาง “ใช้ข้อมูลดิบของแมพกับ runtime ใหม่ทั้งหมด” เพื่อไม่ให้ค่า legacy จากหลายแมพกลายเป็นกฎที่ขัดกัน และมีเอกสารสถาปัตยกรรม กติกาหลักฐาน คู่มือ importer และรายงานรายแมพรองรับการตรวจย้อนกลับ

---

## 6. สิ่งที่ผ่านการทดสอบและหลักฐานที่มี

### 6.1 หลักฐาน World Travel

`Builds/Shared/Logs/Client.log` บันทึกการเดินทางสำเร็จอย่างน้อย 3 เที่ยวในวันที่ 14 กรกฎาคม 2026

| เส้นทาง | ผล |
|---|---|
| California 2 → Limestone | สำเร็จ |
| Limestone → California 2 | สำเร็จ |
| California 2 → Limestone | สำเร็จ |

Log แสดงครบทั้งขั้น disconnect, การกำหนดจุดเกิดในแมพเป้าหมาย และข้อความ `Completed world travel`

### 6.2 หลักฐาน Raw Map Importer

- Unity Editor log บันทึกการสร้าง manifest ครบ 7 ไฟล์
- generated manifests ทุกไฟล์มี `CopiesSourceContent = false`
- generated manifests ทุกไฟล์มี `ImportsLegacyGameplayRules = false`
- output directory ถูกบังคับให้อยู่ภายใน Unity project
- Zone ID จำกัดเป็น lowercase, digit และ hyphen เพื่อป้องกันการใช้ชื่อไฟล์ไม่ปลอดภัย

### 6.3 ข้อผิดพลาดที่พบใน log

Editor log มี `NullReferenceException` จำนวนมากขณะ object เดิมของ level ถูก disable โดย stack trace ชี้ไปที่ `LevelVolume.OnDisable`, `CullingVolume.OnDisable` และ `Spawnpoint.OnDisable` ปัญหานี้ไม่ได้ชี้เข้าคลาส World Travel หรือ Raw Map Importer โดยตรง แต่ยังไม่ควรถูกมองข้าม เพราะการ unload/reload และการสร้าง streaming runtime จะกระทบ lifecycle ของ object กลุ่มเดียวกันอย่างมาก

### 6.4 สิ่งที่ยังไม่ได้ทดสอบอย่างเป็นระบบ

- ยังไม่มี automated unit/integration test suite สำหรับ World Travel และ importer
- ยังไม่มี soak test เดินทางไปกลับหลายสิบหรือหลายร้อยรอบ
- ยังไม่มี crash/fault-injection test ระหว่าง copy savedata, disconnect และ target load
- ยังไม่มี test matrix ครบ inventory, clothing, health, quests, skills และ edge cases ของตัวละครหลายช่อง
- ยังไม่มี performance benchmark ของ frame time, memory, GC, load duration หรือ disk I/O
- ยังไม่มี multiplayer หรือ dedicated server test

---

## 7. สิ่งที่กำลังทำอยู่

คำว่า “กำลังทำ” ในรายงานนี้หมายถึงงานที่มีไฟล์อยู่ใน working branch และยังไม่ถือว่าเสร็จสมบูรณ์ ไม่ได้หมายความว่ามีโปรแกรมหรือ background job กำลังประมวลผลอยู่ตลอดเวลา

### 7.1 การทำให้ World Travel MVP แข็งแรงขึ้น

ต้นแบบเดินทางได้แล้ว แต่ยังอยู่ระหว่างพัฒนาในประเด็นต่อไปนี้

- ตรวจความครบถ้วนของข้อมูลผู้เล่นก่อนและหลังย้ายแมพ
- ออกแบบ transaction ของ savedata ให้กู้คืนได้เมื่อเกมปิดกลางทาง
- ปรับจุดเชื่อมจาก normalized coordinates แบบประมาณให้เป็นตำแหน่งที่ออกแบบในโลกจริง
- ปรับ transition จาก IMGUI prototype ให้เป็น UI/visual ที่เหมาะกับเกม
- เพิ่มข้อความผิดพลาดและ recovery path เมื่อแมพเป้าหมายหาย ถูกปิด หรือ save เขียนไม่สำเร็จ
- แยกให้ชัดว่าระบบนี้เป็น fast-travel/level hand-off ไม่ใช่ seamless streaming

### 7.2 การเปลี่ยนจาก inventory manifest ไปเป็น data importer จริง

Importer ปัจจุบันรู้ว่าไฟล์ใดมีอยู่และอยู่ใน category ใด แต่ยังไม่เข้าใจ record ภายในไฟล์ binary งานที่อยู่ในลำดับถัดไปคือ decoder สำหรับ spawn tables, spawn points, landscape tiles, objects และ navigation เพื่อแปลงข้อมูลเป็น schema ใหม่

### 7.3 การรวมสองสายงานเข้าหากัน

World Travel MVP ใช้พิสูจน์ player-state continuity และ UX ระหว่างแมพ ส่วน World Upgrade จะเป็น runtime ระยะยาว งานปัจจุบันต้องรักษา MVP ไว้เป็น baseline โดยไม่สร้าง dependency ที่ทำให้ต้องใช้ full reload ตลอดไป เมื่อ cell streamer พร้อม ระบบเชื่อมต่อควรเปลี่ยนจาก “disconnect แล้วเปิดแมพใหม่” เป็น “preload cell ปลายทางแล้วข้าม boundary”

### 7.4 สถานะ Git

ก่อนผ่าน gate ระยะ 0 ไฟล์ World Travel, World Upgrade, manifests และเอกสารเป็น uncommitted changes บน branch `codex/world-travel-mvp` รวมทั้งมีการแก้ `MenuStartup.cs` และ `Provider.cs` เพื่อ install/resume ระบบเดินทาง ระยะ 0 จึงถูกเพิ่มเพื่อสร้าง commit ที่ใช้เป็นจุดย้อนกลับและอ้างอิงสถานะทดสอบก่อนเริ่มระยะ 1

---

## 8. สิ่งที่จะทำต่อไป

### ระยะ 0 — ปกป้องงานและสร้าง baseline

**งาน**

- ตรวจแยกไฟล์ source ที่ตั้งใจเก็บจากไฟล์ Unity-generated และ editor-local
- ตรวจ `.gitignore` สำหรับ `Library`, `Logs`, `.vscode` และ output ที่ไม่ควรเผยแพร่
- commit งานปัจจุบันเป็น baseline ที่อ้างอิงผลทดสอบได้
- เก็บ test log และรายการ known issues คู่กับ commit

**เกณฑ์ผ่าน**

- clone/checkout แล้วได้ source, config และ docs ที่จำเป็นครบ
- ไม่มี copyrighted map asset ถูก commit
- สามารถย้อนกลับมายัง baseline ที่เดินทางข้ามแมพได้

### ระยะ 1 — Binary decoders และ validation

**งาน**

- decoder ของ `Spawns/Items.dat` และ `Spawns/Jars.dat`
- decoder ของ zombie, animal, vehicle และ player spawn data
- decoder/manifest ของ landscape, terrain tiles, roads และ object hierarchy
- validation ของ version, bounds, truncated files, missing dependencies และ duplicate IDs
- content hash สำหรับไฟล์สำคัญ แยกจาก inventory fingerprint ปัจจุบัน

**เกณฑ์ผ่าน**

- parse California 2 และ Limestone ได้โดยไม่แก้ source file
- จำนวน record และตำแหน่งตรวจเทียบกับ editor/game เดิมได้
- decoder ล้มอย่างปลอดภัยและรายงานไฟล์/offset เมื่อข้อมูลเสีย

### ระยะ 2 — World schema และ stable identity

**งาน**

- กำหนด `WorldManifest`, `ZoneDefinition`, `WorldCell` และ schema versioning
- กำหนด stable world/zone/cell/entity IDs
- migration table สำหรับ asset GUID/legacy ID
- world layout ของ California 2 และ Limestone
- กำหนด owner cell ของ static/dynamic entities

**เกณฑ์ผ่าน**

- generate schema ซ้ำจาก source เดิมแล้ว IDs ไม่เปลี่ยน
- ตรวจ duplicate/collision ได้ก่อน runtime
- update source manifest ได้โดยรู้ว่า entity ใดเพิ่ม ลบ หรือย้าย

### ระยะ 3 — Single-zone runtime parity

**งาน**

- เปิด California 2 ด้วย runtime ใหม่เพียง zone เดียว
- โหลด terrain, collision และ static objects ผ่าน cell model
- สร้าง World Asset Registry และ reference counting
- เปรียบเทียบตำแหน่ง รูปร่าง collision และ visual identity กับระบบเดิม

**เกณฑ์ผ่าน**

- ผู้เล่นเดินในพื้นที่ทดสอบได้โดยไม่มีพื้นหรือ collision หาย
- object/terrain bounds ตรงกับข้อมูลเดิมภายใน tolerance ที่กำหนด
- โหลดและ unload cell ซ้ำโดย memory ไม่เพิ่มต่อเนื่อง

### ระยะ 4 — Two-zone streaming prototype

**งาน**

- เพิ่ม Limestone เป็น zone ที่สอง
- สร้าง `WorldCellStreamer`, `ZoneResolver` และ `TransitionSafetyService`
- preload/unload terrain, objects และ nav ตามระยะผู้เล่น
- ทำทางเชื่อมทดสอบก่อนสร้างรอยต่อศิลป์จริง
- เพิ่ม world origin shifting หรือ local-coordinate strategy

**เกณฑ์ผ่าน**

- เดิน/ขับข้าม boundary โดย scene ไม่ reload และไม่ teleport
- ไม่มีเฟรมที่ collision หายตรงรอยต่อภายใต้ความเร็วสูงสุดที่กำหนด
- ข้ามไปกลับหลายรอบแล้ว memory กลับสู่ช่วงคงที่

### ระยะ 5 — Population, economy และ persistence

**งาน**

- World Population Service สำหรับ items, zombies, animals และ vehicles
- global policy ใหม่สำหรับ survival, economy และ population
- cell-owned persistence สำหรับ dropped items, vehicles และ buildables
- save transaction, journal และ recovery
- nav border links สำหรับ AI ข้าม cell/zone

**เกณฑ์ผ่าน**

- entity ไม่หายและไม่ duplicate หลัง unload/reload
- save/reload แล้ว entity กลับ owner cell และตำแหน่งเดิม
- population ไม่เพิ่มตามจำนวนครั้งที่ผู้เล่นข้ามเขต

### ระยะ 6 — Environment, presentation และการขยายแมพ

**งาน**

- blend lighting, fog, weather, ambience, oxygen และ water profile
- HLOD, occlusion และ per-cell performance budgets
- เพิ่ม PEI, Washington, Yukon, Russia และ Germany ทีละแมพ
- ออกแบบรอยต่อทางภูมิศาสตร์ ทางเรือ อุโมงค์ หรือพื้นที่คั่น
- ปรับสมดุล gameplay ใหม่หลัง runtime policy พร้อม

**เกณฑ์ผ่าน**

- environment ไม่กระพริบหรือเปลี่ยนกะทันหันโดยไม่มี design intent
- แต่ละแมพผ่าน import, validation, parity และ performance gate ก่อนรวม

### ระยะ 7 — Multiplayer และการผลิตจริง

**งาน**

- server-authoritative streaming และ zone ownership
- replication scope ตาม cell
- reconnect, migration และ anti-duplication rules
- dedicated server load test
- packaging, update compatibility และ license review

**เกณฑ์ผ่าน**

- ผู้เล่นหลายคนอยู่คนละ cell/zone ได้โดย state ไม่ขัดกัน
- reconnect แล้วได้ state ที่ server ยืนยัน
- ไม่มี client-controlled travel/save path ที่ใช้ duplicate item ได้

---

## 9. ข้อจำกัดของระบบปัจจุบัน

### 9.1 ข้อจำกัดของ World Travel MVP

1. **ยังไม่ seamless** — ระบบ disconnect, unload แมพเดิม, ผ่านหน้าเมนู และ load แมพใหม่ เพียงใช้ transition overlay ปิดบัง flow เดิม
2. **รองรับเฉพาะ singleplayer survival** — โค้ดตรวจ `Provider.maxPlayers == 1` และไม่ติดตั้งบน dedicated server
3. **รองรับเพียง California 2 กับ Limestone** — แมพอื่นยังไม่มี connection และตำแหน่งปลายทาง
4. **รถข้ามแมพไม่ได้** — ผู้เล่นต้องลงจากรถก่อนเดินทาง สถานะรถไม่ได้ถูกย้าย
5. **ย้ายเฉพาะสถานะผู้เล่นบางไฟล์** — dropped items, buildables, vehicles, zombies และ world state ไม่ได้เคลื่อนตามผู้เล่น
6. **มีความเสี่ยงทับ target-map player state เดิม** — ระบบสำรองไฟล์ก่อนเขียน แต่ยังไม่มี transaction/journal ที่พิสูจน์การกู้คืนทุก failure point
7. **จุดเชื่อมยังเป็นค่าประมาณ** — ใช้ normalized map coordinate และ trigger radius ไม่ใช่ authored doorway/volume ที่ผ่าน level design
8. **ขึ้นกับแมพที่ติดตั้งในเครื่อง** — ถ้า target map หายหรือถูก disable การเดินทางจะถูกยกเลิก
9. **transition เป็น prototype** — ใช้ screenshot และ IMGUI ไม่ได้ทำให้การโหลดแบบ synchronous หายไป และอาจกระตุกตามเครื่องหรือขนาดแมพ
10. **ผลทดสอบยังมีจำนวนน้อย** — runtime log ยืนยัน 3 เที่ยว ไม่เพียงพอสำหรับสรุปความเสถียรระยะยาว

### 9.2 ข้อจำกัดของ Raw Map Importer

1. เป็น inventory generator ไม่ใช่ world importer ที่เล่นได้
2. ยังไม่ decode binary records ของ terrain, objects, spawn points และ navigation
3. อ่านเพียงชื่อ key ของ legacy gameplay overrides ไม่ได้แปลงค่าเป็น policy ใหม่
4. source catalog ใช้ absolute path ที่ผูกกับเครื่องและตำแหน่งติดตั้ง Steam ปัจจุบัน
5. fingerprint ใช้ path + file size จึงตรวจไม่พบกรณีที่เนื้อหาเปลี่ยนแต่ขนาดเท่าเดิม
6. manifest ของ Limestone มีขนาดใหญ่เพราะเก็บ file record หลายพันรายการ อาจต้องแยก index หรือใช้ format ที่มีประสิทธิภาพกว่าในอนาคต
7. ยังไม่มี automated regression test ข้าม version ของแมพและ U3-SDK
8. ไม่ยืนยันว่า asset definition ทุกไฟล์ถูกอ้างถึงโดย placement หรือ spawn table จริง

### 9.3 ข้อจำกัดของสถาปัตยกรรมโลกเดียว

1. ระบบเดิมมี active `Level.info` เดียวและ manager หลายตัวเป็น singleton
2. region grid และพิกัดโลกเดิมไม่ออกแบบสำหรับหลายแมพที่วางห่างกันมาก
3. asset GUID/legacy ID จากหลายแพ็กอาจชนกัน
4. navigation mesh ต้องถูกแบ่งและเชื่อมข้าม cell โดยไม่ทำให้ AI ติดขอบ
5. physics, terrain collider และ object activation ต้องพร้อมก่อนผู้เล่นถึง cell
6. persistence เดิมผูกกับ map namespace ไม่ใช่ stable world entity ownership
7. environment และ gameplay-affecting zones ต้องแยกจาก visual blending
8. memory budget อาจสูงมากถ้าโหลด asset หรือ terrain ของหลาย zone พร้อมกัน
9. floating origin อาจกระทบ physics, networking, particles และตำแหน่งของ entity ที่บันทึกไว้
10. multiplayer เพิ่มปัญหา authority, replication, reconnect และ item duplication ซึ่งยังไม่ได้ออกแบบครบ

### 9.4 ข้อจำกัดด้านกระบวนการพัฒนา

- ความเสี่ยงจาก working tree ที่ไม่มี baseline ถูกจัดการในระยะ 0 และต้องรักษาวินัย commit/test gate ต่อในทุกระยะ
- ไม่มี CI หรือ automated test สำหรับส่วนที่เพิ่มใหม่
- ไม่มี performance baseline ที่ทำซ้ำได้
- Unity-generated/local editor files ปะปนอยู่ใน working directory และต้องตรวจ scope ก่อน commit
- Editor log มี exception จาก lifecycle ของระบบเดิม ซึ่งอาจกลายเป็น blocker เมื่อเริ่ม unload cell แบบถี่

---

## 10. ความเสี่ยงและแนวทางลดความเสี่ยง

| ความเสี่ยง | ผลกระทบ | ระดับ | แนวทางลดความเสี่ยง |
|---|---|---|---|
| งานยังไม่ commit | สูญหายหรือย้อนกลับยาก | สูง | สร้าง baseline commit หลังตรวจ scope |
| save เสียระหว่าง travel | สูญ inventory/สถานะผู้เล่น | สูง | transaction, staging, checksum, journal และ crash tests |
| entity duplicate หลัง streaming | ทำลาย economy และ save | สูง | stable IDs, owner cell และ idempotent load |
| memory โตหลังข้าม zone | crash หรือกระตุก | สูง | reference counting, budgets และ soak test |
| collision/nav ไม่พร้อมที่รอยต่อ | ผู้เล่นตกโลกหรือ AI ค้าง | สูง | predictive preload และ transition safety gate |
| asset ID/GUID ชนกัน | โหลด asset ผิดหรือ save อ้างผิด | สูง | global registry และ collision validator |
| Workshop update เปลี่ยนไฟล์ | importer/schema ไม่ตรง | กลาง–สูง | content version, hashes และ compatibility report |
| absolute path ใช้ไม่ได้บนเครื่องอื่น | generate manifest ไม่ได้ | กลาง | path discovery และ user-configurable roots |
| นำ asset ไปแจกโดยไม่มีสิทธิ์ | ปัญหาลิขสิทธิ์/การเผยแพร่ | สูง | เก็บ importer read-only และตรวจสิทธิ์ก่อน packaging |
| multiplayer duplication/exploit | economy และ server state เสีย | สูง | server authority และ transactional transfer |

---

## 11. เกณฑ์ก่อนเรียกระบบว่า “โลกเดียวแบบ seamless”

ระบบจะยังไม่ถูกเรียกว่า seamless จนกว่าจะผ่านเงื่อนไขอย่างน้อยดังนี้

- เดินและขับข้าม boundary โดย scene ไม่ reload และตัวละครไม่ teleport
- ไม่มีเฟรมที่พื้นหรือ collision หายตรงรอยต่อในความเร็วสูงสุดที่รองรับ
- AI ข้าม nav border link ได้ หรือหยุดตามกฎที่อธิบายได้
- dropped items, vehicles และ buildables ไม่หายหรือ duplicate หลัง cell unload/reload
- save/reload แล้ว entity กลับ owner cell และตำแหน่งเดิม
- memory อยู่ในช่วงคงที่หลังเดินทางข้ามไปกลับหลายรอบ
- environment transition ไม่มี flicker หรือ gameplay modifier เปลี่ยนแบบไม่กำหนด
- failure ระหว่าง save/load สามารถ rollback หรือ resume ได้โดยไม่ทำลายข้อมูล

World Travel MVP ปัจจุบันยังไม่ผ่านนิยามนี้ เพราะยังใช้ full level reload

---

## 12. ลำดับความสำคัญที่แนะนำทันที

1. **เก็บงานปัจจุบันเป็น baseline commit** โดยคัดเฉพาะ source/config/docs/manifests ที่ตั้งใจเก็บ
2. **เขียน smoke-test checklist ของ World Travel** และทดสอบ state ทุกไฟล์ทั้งสองทิศทาง
3. **เพิ่ม crash-recovery design** ก่อนขยายระบบ copy savedata
4. **เริ่ม binary decoder จาก Items.dat และ Jars.dat** เพราะมี source code reader เดิมให้เทียบ behavior ได้
5. **กำหนด stable ID/schema ก่อนสร้าง streamer** เพื่อไม่ต้องย้าย persistence ซ้ำภายหลัง
6. **ทำ single-zone parity ก่อน two-zone seamless** เพื่อแยก bug importer ออกจาก bug streaming
7. **ตั้ง performance baseline ตั้งแต่ California 2 zone เดียว** แล้วใช้เป็น gate ทุกระยะ

---

## 13. ไฟล์หลักและหลักฐานในโครงการ

### World Travel

- `Assets/Runtime/Assembly-CSharp/Unturned/WorldTravel/WorldTravelManager.cs`
- `Assets/Runtime/Assembly-CSharp/Unturned/WorldTravel/WorldTravelSavedata.cs`
- `Assets/Runtime/Assembly-CSharp/Unturned/WorldTravel/WorldTravelData.cs`
- `Assets/Runtime/Assembly-CSharp/Unturned/WorldTravel/WorldTravelTransitionPresenter.cs`
- `Builds/Shared/WorldTravel.json`
- `Builds/Shared/Logs/Client.log`

### World Upgrade

- `docs/world-upgrade/decisions/ADR-001-raw-map-data-new-runtime.md`
- `docs/world-upgrade/map-system-audit/README.md`
- `docs/world-upgrade/map-system-audit/SYSTEM_CATALOG.md`
- `docs/world-upgrade/map-system-audit/WORLD_RUNTIME_ARCHITECTURE.md`
- `docs/world-upgrade/map-system-audit/EVIDENCE.md`
- `docs/world-upgrade/raw-map-importer/README.md`

### Importer และ manifests

- `Assets/Editor/Assembly-CSharp-Editor/WorldUpgrade/RawMapImporterWindow.cs`
- `Assets/Editor/Assembly-CSharp-Editor/WorldUpgrade/RawMapManifestGenerator.cs`
- `Assets/Editor/Assembly-CSharp-Editor/WorldUpgrade/RawMapInventoryBuilder.cs`
- `Assets/Runtime/Assembly-CSharp/Unturned/WorldUpgrade/RawMapManifestData.cs`
- `Builds/WorldUpgrade/WorldSourceCatalog.json`
- `Builds/WorldUpgrade/RawMapManifests/`

---

## 14. ข้อสรุป

โครงการมีความคืบหน้าที่จับต้องได้สองส่วน ได้แก่ ต้นแบบการเดินทางข้าม California 2 และ Limestone ที่รักษาสถานะผู้เล่นบางส่วนได้ และเครื่องมือ inventory importer ที่สร้าง manifest จากแมพ 7 แห่งโดยรักษาขอบเขต read-only และไม่รับกฎ gameplay เดิมเข้าระบบใหม่

แต่ระบบปัจจุบันยังไม่ใช่โลกเดียวแบบ seamless และยังไม่ควรสื่อว่า terrain, objects, navigation, population หรือ persistence ของหลายแมพทำงานใน runtime เดียวแล้ว งานเหล่านั้นยังเป็นแผนและสถาปัตยกรรมที่ต้อง implement และทดสอบอีกหลายระยะ

แนวทางที่ปลอดภัยที่สุดคือรักษา World Travel MVP เป็น baseline สำหรับการพิสูจน์ player-state continuity ขณะเดียวกันพัฒนา Raw Map Importer → stable schema → single-zone parity → two-zone streaming ตามลำดับ การเร่งวางหลายแมพต่อกันก่อนมี stable IDs, owner cells และ persistence transaction จะเพิ่มความเสี่ยงด้าน save corruption, entity duplication และการต้องรื้อระบบภายหลังอย่างมาก

สถานะที่ควรรายงานอย่างตรงไปตรงมาคือ: **ต้นแบบการเดินทางทำงานแล้ว, ฐาน importer พร้อมแล้ว, โลก seamless ยังไม่ถูกสร้าง และข้อจำกัดหลักได้รับการระบุเพื่อใช้วางแผนระยะถัดไป**
