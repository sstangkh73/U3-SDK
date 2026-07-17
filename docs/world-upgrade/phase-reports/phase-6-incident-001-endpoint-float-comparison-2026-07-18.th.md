# Incident Phase 6-001: Endpoint Float Comparison False Negative

วันที่: 18 กรกฎาคม 2026  
สถานะ: **ปิดแล้ว — validator ผ่านและ full regression 1,358/1,358**

## อาการ

หลังเพิ่ม gate ให้ตรวจว่าค่า environment ที่ blend ณ `t=0` และ `t=1` ตรงกับ source/destination ทุก field ตัว validator รายงาน `EnvironmentBlendDiscontinuity` แม้ค่าที่ log แบบ round-trip ของ RGB state และ expected profile เหมือนกันทุกหลัก

## ผลกระทบ

- evidence รอบ diagnostic มี `IsValid=false` และไม่ถูกใช้เป็นหลักฐาน final
- ไม่มี commit/push ระหว่างที่ gate ล้ม
- runtime data ไม่เสียหาย; ปัญหาอยู่ใน validator equality expression

## สาเหตุ

expected RGB ถูกคำนวณใน expression `byte / 255f` แล้วนำไปเทียบกับ float ที่ถูก materialize ใน list โดยตรง JIT/float evaluation path สามารถเก็บ intermediate precision ต่างจากค่าที่ store เป็น `float` ทำให้ direct `!=` ให้ false negative แม้เมื่อ format แบบ round-trip แล้วค่าที่ materialize ตรงกัน

## การแก้

1. ทำให้ `Lerp` คืน source/destination โดยตรงเมื่อ blend ถึง 0/1
2. materialize expected R/G/B เป็นตัวแปร `float` ก่อนเปรียบเทียบ
3. คง bitwise equality gate ไว้ ไม่เปลี่ยนเป็น tolerance เพื่อซ่อนปัญหา
4. รัน Phase 6 validator และ full EditMode suite ใหม่

## เกณฑ์ปิด incident

- `BlendEndpointsExact=true`
- Phase 6 validation errors = 0
- full regression failures = 0

ค่าหลักฐาน final บันทึกใน `phase-6-environment-expansion-evidence.json` และรายงาน Phase 6
