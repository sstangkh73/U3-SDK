# Incident Report Phase 2-001: Legacy Asset Alias Collisions

**วันที่:** 18 กรกฎาคม 2026  
**สถานะ:** แก้แล้วและ regression test ผ่าน  
**ผลกระทบ:** integration generation รอบแรกไม่ผ่าน gate; ไม่มี source map หรือ save data ถูกแก้ไข

## อาการ

การ generate deterministic schema รอบแรกสร้างข้อมูลได้ครบและได้ fingerprint ซ้ำตรงกัน แต่ validator รายงาน:

| รายการ | ผลรอบแรก |
|---|---:|
| Zones | 2 |
| Cells | 79 |
| Entities | 133,624 |
| Migrations | 4,559 |
| Deterministic repeat build | ผ่าน |
| Duplicate stable IDs | 0 |
| Owner cell mismatch | 0 |
| Zone overlap | 0 |
| Validation errors | 177 |

error ทั้ง 177 รายการเป็น `MigrationAliasCollision`: legacy object ID เดียวกันใน zone เดียวอ้างถึง GUID มากกว่าหนึ่งค่า

## สาเหตุ

`Objects.dat` รุ่นใหม่บันทึกทั้ง legacy `ushort id` และ asset GUID โดย loader เดิมใช้ GUID เป็นหลักและใช้ legacy ID เป็น fallback เมื่อ GUID หา asset ไม่พบ ใน Workshop content หลายชุด legacy IDs สามารถชนกันได้ แม้ GUID จะยังแยก asset ได้ถูกต้อง

validator รุ่นแรกสมมติว่า legacy ID ต้องเป็น one-to-one กับ GUID ภายใน zone จึงจัดทุก collision เป็น error สมมติฐานนี้เข้มเกิน format/runtime behavior จริง

## การแก้

เพิ่มสถานะ migration alias สามแบบ:

- `Unambiguous`: legacy ID resolve target เดียว
- `AmbiguousGuidPrimary`: legacy ID มีหลาย GUID target; ปิด `CanResolveLegacyId`
- `GuidOnly`: ไม่มี legacy ID

validator ยังตรวจและรายงาน collision ทุกกลุ่ม แต่เปลี่ยน collision ที่ถูก quarantine แล้วเป็น warning หาก code เผลอตั้ง ambiguous alias ให้ legacy-only resolution ได้ จะเป็น `UnsafeMigrationAliasCollision` error และหยุด generation

## ผลหลังแก้

| รายการ | ผล |
|---|---:|
| Validation errors | 0 |
| Quarantined alias groups | 177 warnings |
| Migration entries ที่อยู่ใน ambiguous groups | 747 |
| Unambiguous migration entries | 1,835 |
| GUID-only migration entries | 1,977 |
| Duplicate IDs | 0 |
| Owner mismatch | 0 |
| Zone overlap | 0 |
| Deterministic repeat build | ผ่าน |

source-update diff หลัง generate ซ้ำจาก source เดิมเป็น `Unchanged=133,624` และ `Added/Removed/Moved/Modified=0`

## การป้องกันซ้ำ

- automated test สร้าง legacy collision สังเคราะห์และยืนยันว่า alias ถูก quarantine
- validator ห้าม ambiguous alias มี `CanResolveLegacyId=true`
- generated manifest เก็บ `LegacyAliasStatus`, `CollisionTargetCount` และ source reference examples
- runtime ระยะถัดไปต้อง resolve GUID ก่อนเสมอและห้าม fallback ด้วย alias ที่ไม่ unambiguous

## ข้อจำกัด

การ quarantine ทำให้ record ที่เหลือเพียง ambiguous legacy ID แต่ไม่มี GUID ไม่สามารถ resolve อัตโนมัติได้ นี่เป็น fail-safe ที่ตั้งใจไว้ ต้องแก้ด้วย explicit migration override ที่ตรวจโดยมนุษย์ ไม่ควรเดา target
