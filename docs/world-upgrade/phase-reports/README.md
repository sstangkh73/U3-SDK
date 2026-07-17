# World Upgrade Phase Reports

เอกสารในโฟลเดอร์นี้เป็นรายงาน gate ของแผนพัฒนาระยะ 0–7 แต่ละระยะจะเปลี่ยนเป็น “ผ่าน” เมื่อ implementation, validation และข้อจำกัดที่ยังเหลือถูกบันทึกครบแล้วเท่านั้น

| ระยะ | รายงาน | สถานะ |
|---:|---|---|
| 0 | [Baseline และการปกป้องงาน](phase-0-baseline-2026-07-18.th.md) | ผ่านและเผยแพร่แล้ว |
| 1 | [Binary decoders และ validation](phase-1-binary-decoders-2026-07-18.th.md) | ผ่านและเผยแพร่แล้ว — `b9c5c19` |
| 2 | [Deterministic world schema และ stable identity](phase-2-world-schema-2026-07-18.th.md) | ผ่านการตรวจในเครื่อง; รอ metadata หลัง push |
| 3 | Single-zone runtime parity | ยังไม่เริ่ม |
| 4 | Two-zone streaming prototype | ยังไม่เริ่ม |
| 5 | Population, economy และ persistence | ยังไม่เริ่ม |
| 6 | Environment, presentation และการขยายแมพ | ยังไม่เริ่ม |
| 7 | Multiplayer และ production hardening | ยังไม่เริ่ม |

## กติกา gate

- ต้องมี source/config/docs ที่ทำซ้ำได้
- ต้อง compile และผ่าน validation ที่เกี่ยวข้อง
- ต้องบันทึกหลักฐาน ผลลัพธ์ ข้อผิดพลาด และข้อจำกัด
- ห้ามใช้ผลของระยะก่อนหน้าเป็นหลักฐานแทนสิ่งที่ระยะปัจจุบันยังไม่ได้ทดสอบ
- ถ้าเกิดปัญหาระหว่างระยะ ให้เขียน issue/incident section และแก้หรือประกาศ blocker ก่อนผ่าน gate
