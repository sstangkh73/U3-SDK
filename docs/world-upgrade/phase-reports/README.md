# World Upgrade Phase Reports

เอกสารในโฟลเดอร์นี้เป็นรายงาน gate ของแผนพัฒนาระยะ 0–7 แต่ละระยะจะเปลี่ยนเป็น “ผ่าน” เมื่อ implementation, validation และข้อจำกัดที่ยังเหลือถูกบันทึกครบแล้วเท่านั้น

| ระยะ | รายงาน | สถานะ |
|---:|---|---|
| 0 | [Baseline และการปกป้องงาน](phase-0-baseline-2026-07-18.th.md) | ผ่านและเผยแพร่แล้ว |
| 1 | [Binary decoders และ validation](phase-1-binary-decoders-2026-07-18.th.md) | ผ่านและเผยแพร่แล้ว — `b9c5c19` |
| 2 | [Deterministic world schema และ stable identity](phase-2-world-schema-2026-07-18.th.md) | ผ่านและเผยแพร่แล้ว — `03cdfee` |
| 3 | [Single-zone runtime parity](phase-3-single-zone-runtime-parity-2026-07-18.th.md) | ผ่านและเผยแพร่แล้ว — `cb745fa` |
| 4 | [Two-zone streaming prototype](phase-4-two-zone-streaming-prototype-2026-07-18.th.md) | ผ่าน automated prototype gate และเผยแพร่แล้ว — `190e15f` |
| 5 | [Population, economy และ persistence foundation](phase-5-population-persistence-foundation-2026-07-18.th.md) | ผ่าน automated data-foundation gate และเผยแพร่แล้ว — `5efe724` |
| 6 | Environment, presentation และการขยายแมพ | ยังไม่เริ่ม |
| 7 | Multiplayer และ production hardening | ยังไม่เริ่ม |

## กติกา gate

- ต้องมี source/config/docs ที่ทำซ้ำได้
- ต้อง compile และผ่าน validation ที่เกี่ยวข้อง
- ต้องบันทึกหลักฐาน ผลลัพธ์ ข้อผิดพลาด และข้อจำกัด
- ห้ามใช้ผลของระยะก่อนหน้าเป็นหลักฐานแทนสิ่งที่ระยะปัจจุบันยังไม่ได้ทดสอบ
- ถ้าเกิดปัญหาระหว่างระยะ ให้เขียน issue/incident section และแก้หรือประกาศ blocker ก่อนผ่าน gate

## Incident reports

- [Phase 2-001: Legacy alias collisions](phase-2-incident-001-legacy-alias-collisions-2026-07-18.th.md)
- [Phase 3-001: Runtime validation harness](phase-3-incident-001-runtime-validation-harness-2026-07-18.th.md)
- [Phase 3-002: Hierarchy landscape authority](phase-3-incident-002-hierarchy-landscape-authority-2026-07-18.th.md)
- [Phase 4-001: Transition collision gaps](phase-4-incident-001-transition-collision-gaps-2026-07-18.th.md)
