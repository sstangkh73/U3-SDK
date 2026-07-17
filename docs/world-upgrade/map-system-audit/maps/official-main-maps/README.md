# Official Main Maps

ชุดนี้ครอบคลุมแมพหลักที่มากับเกมและติดตั้งอยู่: PEI, Washington, Yukon, Russia และ Germany

## จุดร่วมที่สำคัญ

`Config.json` ของทั้งห้าแมพ **ไม่มี** `Mode_Config_Overrides` หรือ difficulty-specific overrides จึงไม่มีกฎกิน น้ำ item respawn zombie density หรือ vehicle cap เฉพาะแมพจาก config ใน snapshot นี้ พฤติกรรมเหล่านั้นรับจาก server/difficulty config กลาง ส่วนความต่างของแมพอยู่ที่ terrain, environment, nav, spawn tables/points, objects, roads, train associations และ level assets

อ่านรายละเอียด ownership และผลต่อโลกใหม่ใน [Shared systems](SHARED_SYSTEMS.md)

## Maps

- [PEI](../pei/README.md)
- [Washington](../washington/README.md)
- [Yukon](../yukon/README.md)
- [Russia](../russia/README.md)
- [Germany](../germany/README.md)

## Audit depth

แมพ official ทั้งห้าอยู่ที่ระดับ **structure + ownership audit v1**: ยืนยัน config, size/type, tile counts และไฟล์ระบบแล้ว แต่ยังไม่ได้สร้าง usage graph ของ official global asset catalog หรือ decode binary records รายจุด จึงไม่อ้างจำนวน NPC/item definitions ต่อแมพแบบที่ทำกับ California และ Limestone
