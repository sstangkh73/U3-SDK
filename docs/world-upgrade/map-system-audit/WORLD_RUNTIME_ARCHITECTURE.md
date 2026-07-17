# สถาปัตยกรรมเป้าหมายสำหรับโลกใหญ่

เอกสารนี้เป็นขอบเขตทางเทคนิคเบื้องต้น ไม่ใช่คำยืนยันว่า implement แล้ว

## เป้าหมาย

ผู้เล่นเดินหรือขับรถข้าม California, Limestone และแมพหลักอื่นได้ในโลกเดียว โดยไม่มีหน้า loading และไม่ต้องย้ายไปคนละ save/level

ระบบนี้ใช้ raw map-authored data แต่ไม่ใช้ legacy gameplay overrides เป็น runtime policy ตาม [ADR-001](../decisions/ADR-001-raw-map-data-new-runtime.md)

## หน่วยข้อมูลใหม่

```text
WorldManifest
  ├─ ZoneDefinition: California2
  │    ├─ SurvivalProfile
  │    ├─ PopulationProfile
  │    ├─ EconomyProfile
  │    └─ WorldCells[]
  ├─ ZoneDefinition: Limestone
  └─ TransitionZones[]

WorldCell
  ├─ terrain + holes + water
  ├─ static objects + HLOD
  ├─ nav chunk + border links
  ├─ item/zombie/animal/vehicle spawn records
  └─ persistent dynamic state
```

## Runtime ที่ต้องแทนระบบเดิม

| ระบบใหม่ | งาน |
|---|---|
| `WorldOriginService` | รองรับพิกัดไกลด้วย floating origin หรือ local coordinates |
| `WorldCellStreamer` | โหลด/ถอด terrain, objects, nav และ spawn data รอบผู้เล่นแบบ asynchronous |
| `ZoneResolver` | บอกว่าผู้เล่น/วัตถุอยู่ใน zone ใดและกำลัง blend ระหว่างอะไร |
| `WorldPopulationService` | ดูแลซอมบี้ สัตว์ รถ และ item targets แยกตาม cell |
| `WorldPersistenceService` | save สถานะด้วย stable world/cell/entity IDs ไม่ผูกกับ active `Level.info` |
| `EnvironmentBlender` | blend lighting, fog, weather, ambience, oxygen และ water profile |
| `WorldAssetRegistry` | resolve GUID/ID และ reference counting ข้าม asset pack |
| `TransitionSafetyService` | preload cell ฝั่งหน้า, กำหนด fallback และป้องกันตกโลก |

## หลักการรักษาความต่างของแมพ

- รูปร่าง ภูมิประเทศ จุดเมือง loot identity NPC/quest และ ecosystem เก็บเป็น zone data
- อาหาร/น้ำ/เลือด/เชื้อใช้ algorithm และค่ากลางใหม่ทั้งหมด; legacy rate เป็น reference-only และไม่เป็น zone modifier อัตโนมัติ
- จำนวนเกิดไม่ใช้ legacy `Spawn_Chance` ต้องคำนวณจาก policy ใหม่ตาม active cells และความหนาแน่นเป้าหมายของ zone
- รถและไอเท็มทุกชิ้นต้องมี owner cell เพื่อไม่ duplicate เมื่อ unload/reload
- อากาศ/แสง blend ได้ แต่ค่าที่กระทบ gameplay ต้องเปลี่ยนที่เส้นขอบที่นิยามชัด ไม่กระพริบตามเฟรม

## ลำดับ implement ที่แนะนำ

1. สร้าง importer แบบ read-only ให้แปลงหนึ่งแมพเป็น `ZoneDefinition` และ cell records
2. เปิด California เพียงแมพเดียวผ่าน runtime ใหม่ให้เทียบกับระบบเดิมได้
3. เพิ่ม Limestone เป็น zone ที่สอง แต่ยังวางห่างกันและใช้ทางเชื่อมทดสอบ
4. ทำ preload/unload terrain + objects + nav โดยไม่มีหน้า loading
5. ย้าย item/zombie/animal/vehicle population และ persistence
6. ทำ transition band จริง พร้อม environment blending และ world origin shifting
7. หลัง parity ผ่านจึงนำแมพหลักที่เหลือเข้าทีละแมพ

## เงื่อนไขผ่านก่อนเรียกว่า seamless

- เดิน/ขับข้าม boundary โดย scene ไม่ reload และตัวละครไม่ teleport
- ไม่มีเฟรมที่พื้นหรือ collision หายตรงรอยต่อในความเร็วรถสูงสุดที่รองรับ
- zombie/nav agent ข้าม border link ได้หรือหยุดอย่างมีเหตุผล
- ของที่ดรอป รถ และ buildables ไม่หายหรือ duplicate หลัง cell unload/reload
- save/reload แล้ว entity กลับ cell และตำแหน่งเดิม
- memory คงที่หลังเดินข้ามไปกลับหลายรอบ
