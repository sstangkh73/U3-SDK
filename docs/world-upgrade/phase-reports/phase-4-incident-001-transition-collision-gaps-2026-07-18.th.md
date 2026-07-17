# Incident Report Phase 4-001: Transition Collision Gaps

วันที่: 18 กรกฎาคม 2026
สถานะ: แก้แล้วสำหรับ automated prototype
ผลกระทบ: Phase 4 physical collision gate ไม่ผ่าน; ไม่มี source map หรือ player save ถูกแก้

## 1. Detection

integration รอบแรก:

- logical preload misses: 0
- logical ground-coverage misses: 0
- physical raycast misses: 8/33

หลังแก้ centerline รอบที่สอง:

- physical raycast misses: 2/33
- ตำแหน่งที่พลาด: Limestone `x=5184` และ `x=5248`, `z=512`

## 2. Root causes

### 2.1 Centerline ทับ terrain tile seam

overlap bounds midpoint คือ `z=0` ซึ่งตรงขอบระหว่าง terrain cells การ raycast บน exact TerrainCollider edge ไม่ใช่เส้นทางที่ปลอดภัยสำหรับ transition

### 2.2 Destination landing มี terrain holes

หลังย้าย centerline ไป `z=512` จุดที่เหลือไม่ได้อยู่บน seam แต่เป็น holes ภายใน Limestone boundary tile สะพานรุ่นแรกจบตรง zone boundary `x=5120` จึงปล่อยให้ landing ช่วงแรกพึ่ง terrain collider ที่มี holes

### 2.3 Slope collider surface offset

bridge รุ่นแรกวาง thickness ด้วย global-down หลังหมุน ทำให้ top surface ไม่ผ่าน sampled endpoints อย่างแม่นยำ

## 3. Corrective actions

- snap corridor centerline ไป terrain-cell center ถ้ายังอยู่ใน shared Z interval
- sample source/destination terrain heights ที่ landing positions จริง
- offset bridge thickness ตาม rotated local-up
- เพิ่ม collider length overlap 2 เมตรที่ปลายเชิงตัวเลข
- ขยาย transition span เข้า terrain ทั้งสองฝั่ง 256 เมตร
- เก็บ source/destination boundary X แยกจาก transition start/end เพื่อไม่ซ่อน raw gap
- log exact miss coordinate/collider เมื่อ validation ล้ม

## 4. Final result

- raw zone gap: 1,024 เมตร
- transition span: 1,536 เมตร
- centerline: `z=512`
- landing overlap: 256 เมตรต่อฝั่ง
- physical collision samples: 41
- physical misses: 0
- logical misses: 0
- preload misses: 0
- validation errors: 0

## 5. Prevention and limits

- transition placement ต้องพิจารณา terrain holes ไม่ใช่ bounds อย่างเดียว
- route candidate ควร sample physical collider ก่อนผ่าน gate
- final art route ต้องทดสอบ player/vehicle suspension, slope, width และ nav เพิ่ม
- ผลนี้รับรองเฉพาะ generated prototype bridge ไม่ใช่ทุกเส้นทางเชื่อมในอนาคต
