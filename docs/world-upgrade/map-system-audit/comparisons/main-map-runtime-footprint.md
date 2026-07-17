# Runtime Footprint ของแมพหลัก

ตัวเลขนี้เป็น raw data footprint ไม่ใช่ RAM/VRAM ขณะเล่น แต่ช่วยจัดลำดับ importer และ streaming stress tests

| Map | Size | Height/Splat/Holes | Nav | Objects | Hierarchy | Foliage |
|---|---|---:|---:|---:|---:|---:|
| California 2 | Insane 8192 | 52/55/43 | 52 | 6.85 MB | 6.08 MB | 286.15 MB |
| Limestone | Medium 2048 | 22/22/20 | 34 | 2.25 MB | 2.10 MB | 310.75 MB |
| PEI | Medium 2048 | 16/16/2 | 19 | 0.37 MB | 1.30 MB | 43.52 MB |
| Washington | Medium 2048 | 16/16/0 | 21 | 0.45 MB | 1.30 MB | 98.46 MB |
| Yukon | Medium 2048 | 16/16/0 | 18 | 0.17 MB | 1.27 MB | 7.54 MB |
| Russia | Large 4096 | 64/64/0 | 36 | 1.29 MB | 5.18 MB | 526.91 MB |
| Germany | Large 4096 | 36/36/5 | 26 | 1.14 MB | 2.95 MB | 470.89 MB |

## ข้อสรุปสำหรับลำดับทดสอบ

1. **PEI** — official importer baseline เพราะโครงสร้างเล็กและไม่มี map gameplay overrides
2. **Yukon** — ทดสอบ snow/aurora/legacy-water interaction และ train
3. **California 2 + Limestone** — ทดสอบ curated overrides, dependency packs และ seamless transition ระหว่างระบบต่างยุค
4. **Germany** — ทดสอบ holes, modern fog/oxygen และ foliage หนัก
5. **Russia** — stress test สูงสุดของ official set จาก 64 landscape tiles และ foliage ประมาณ 527 MB

## ข้อควรระวัง

- Level size ไม่บอกค่าใช้จ่ายทั้งหมด: Limestone เป็น Medium แต่ foliage ใหญ่กว่า California
- file size ไม่เท่ากับ runtime residency; bundle compression และ Unity object expansion อาจทำให้ลำดับเปลี่ยน
- nav chunk count และ landscape tile count ไม่ได้มี mapping 1:1
- ต้องวัด peak memory ระหว่างช่วงที่ cell เก่าและใหม่ overlap กัน เพราะช่วง seamless transition ใช้ memory สูงกว่าการอยู่กลาง zone
