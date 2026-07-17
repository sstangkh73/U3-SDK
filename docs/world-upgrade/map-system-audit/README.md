# Map System Audit สำหรับโครงการโลกใหญ่

เอกสารชุดนี้แยกระบบของแมพ Unturned ออกจากกันเป็นชิ้น เพื่อใช้เป็นฐานก่อนอัปเกรดเกมเป็นโลกเดียวขนาดใหญ่ โดยไม่สมมติว่าแมพที่สร้างคนละยุคใช้กฎเดียวกัน

การตัดสินใจล่าสุดของโครงการคือ [ใช้ข้อมูลดิบของแมพกับ runtime ใหม่ทั้งหมด](../decisions/ADR-001-raw-map-data-new-runtime.md) ค่า gameplay เดิมใน audit จึงเป็น reference สำหรับออกแบบและ balance เท่านั้น ไม่ถูกนำเข้าระบบใหม่อัตโนมัติ

เครื่องมือที่สร้าง inventory จากไฟล์ดิบอยู่ใน [Raw Map Importer](../raw-map-importer/README.md)

## ข้อสรุปสำคัญ

แมพไม่ได้มี source code ของระบบกิน ระบบซอมบี้ หรือระบบดรอปของแยกเป็นคนละชุดทั้งหมด แต่ประกอบจาก 3 ชั้น:

1. **กลไกกลางของเกม** เช่น `PlayerLife`, `ItemManager`, `ZombieManager`
2. **ค่าปรับแต่งของแมพ** จาก `Config.json` และค่าตามระดับความยาก
3. **ข้อมูล/คอนเทนต์ของแมพ** เช่น terrain, spawn tables, spawn points, navmesh, objects, NPCs และ asset bundles

ดังนั้นประโยคว่า “ระบบกินของ California” ใน audit นี้หมายถึง กลไกกลาง + override ของ California + ไอเท็มอาหารที่ California นำเข้ามาใช้ ไม่ได้หมายความว่ามีคลาสระบบกินเฉพาะ California

## สถานะ audit

| รายการ | ประเภท | สถานะ |
|---|---|---|
| California 2 | Workshop survival map | audit รายละเอียด v1 |
| Limestone | Workshop survival map | audit รายละเอียด v1 |
| California 2 Assets | dependency pack ไม่ใช่แมพ | inventory แล้ว |
| PEI | official survival map | audit โครงสร้างและ system ownership v1 |
| Washington | official survival map | audit โครงสร้างและ system ownership v1 |
| Russia | official survival map | audit โครงสร้างและ system ownership v1 |
| Germany | official survival map | audit โครงสร้างและ system ownership v1 |
| Yukon | official survival map | audit โครงสร้างและ system ownership v1 |
| Alpha Valley | official/legacy map | รอจัดประเภทและ audit |
| Destruction | non-standard map | รอจัดประเภทและ audit |
| Monolith | non-standard map | รอจัดประเภทและ audit |
| Paintball_Arena_0 | arena map | ไม่นำเข้ากฎ survival โดยตรง |
| Tutorial | tutorial level | ใช้เป็น reference ระบบสอน ไม่ใช่เขตโลกหลัก |

รายการข้างต้นมาจากโฟลเดอร์เกมและ Steam Workshop ที่ติดตั้งอยู่บนเครื่อง ณ วันที่ทำ audit นี้

## เอกสารหลัก

- [บัญชีระบบและแหล่งข้อมูล](SYSTEM_CATALOG.md)
- [Template สำหรับ audit แมพถัดไป](MAP_AUDIT_TEMPLATE.md)
- [สถาปัตยกรรมเป้าหมายสำหรับโลกใหญ่](WORLD_RUNTIME_ARCHITECTURE.md)
- [California 2](maps/california-2/README.md)
- [Limestone](maps/limestone/README.md)
- [Official main maps: PEI, Washington, Yukon, Russia, Germany](maps/official-main-maps/README.md)
- [เปรียบเทียบ California 2 กับ Limestone](comparisons/california-2-vs-limestone.md)
- [เปรียบเทียบ runtime footprint ของแมพหลัก](comparisons/main-map-runtime-footprint.md)

## กติกาการบันทึก

- **Confirmed**: อ่านจาก source code หรือไฟล์แมพจริงแล้ว
- **Derived**: คำนวณ/ตีความจากข้อมูลที่ยืนยันแล้ว
- **Pending runtime verification**: โค้ดชี้พฤติกรรมไว้ แต่ยังต้องเปิดเกมตรวจผลจริง
- **Inherited**: แมพไม่ได้ override ค่า จึงรับค่าจาก difficulty/server config
- เก็บ **legacy value** ไว้ครบ แม้สุดท้ายจะไม่ใช้ในโลกใหม่
- ห้ามรวมค่าของสองแมพด้วยการทับ `Config.json` เพราะ override เดียวกันอาจมีความหมายและข้อบกพร่องต่างกัน

## เป้าหมายของรอบนี้

รอบนี้เป็น inventory และ behavior audit ยังไม่แก้ระบบ gameplay และยังไม่แปลง binary spawn data เป็นฐานข้อมูลใหม่ การ implement จะเริ่มหลัง schema ของแมพหลักนิ่ง เพื่อไม่สร้าง runtime ใหม่บนความเข้าใจที่ไม่ครบ
