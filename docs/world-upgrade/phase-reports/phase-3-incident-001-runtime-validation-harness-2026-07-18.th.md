# Incident Report Phase 3-001: Runtime Validation Harness เริ่มแผนที่ผิด path

วันที่: 18 กรกฎาคม 2026
สถานะ: แก้แล้ว
ผลกระทบ: ทำให้ integration validation ไม่สร้าง evidence; ไม่มี production data หรือ source map ถูกแก้

## 1. อาการ

การสร้าง Phase 3 command-line validation harness พบปัญหาต่อเนื่องสามจุด:

1. compile ล้มด้วย `CS1626: Cannot yield a value in the body of a try block with a catch clause`
2. runner เข้า Play Mode แต่เปิด `Temp/__Backupscenes/0.backup` แทน startup flow และ timeout หลัง 5 นาที
3. หลังเปิด `GameStartup.unity` ได้แล้ว runner ใช้ single-player mode และชื่อ `California 2`; `Level.load` ได้ข้อมูลระดับเป็น null และเกิด `NullReferenceException` จากนั้น player savedata ก็เป็น null ใน batch session

## 2. Root cause

- C# iterator ไม่อนุญาต `yield` ภายใน `try` ที่มี `catch`
- command-line `EditorApplication.isPlaying = true` ใช้ scene ที่ editor จำไว้ หาก runnerไม่เปิด startup scene ชัดเจน
- map discovery name จริงคือ `California2` ไม่มีช่องว่าง
- `AutoLoadMode=0` เรียก `Provider.singleplayer`, ซึ่งต้องใช้ Steam/player session state ที่ไม่มีใน headless Unity Editor batch run

## 3. การแก้

- เปลี่ยน smoke routine เป็น synchronous validation และ cleanup ด้วย `DestroyImmediate` ภายใต้ `UNITY_EDITOR`
- runner เปิด `Assets/GameStartup.unity` แบบ single scene ก่อนเข้า Play Mode
- ตั้ง `AutoLoadLevel=California2`
- ตั้ง `AutoLoadMode=1` เพื่อเรียก `Level.edit(level)` ซึ่งโหลด Landscape และ LevelObjects ที่ parity validator ต้องใช้โดยไม่สร้าง player session
- เก็บและคืนค่า EditorPrefs เดิมทั้ง success และ timeout path
- bootstrap ยัง opt-in ด้วย `-WorldUpgradePhase3Validation` เท่านั้น

## 4. Verification

- compile หลังแก้ผ่าน
- California2 ถูก discover และ Game scene โหลดสำเร็จ
- `Level.onPostLevelLoaded` เรียก validator
- final runtime run เขียน evidence และ exit โดย `valid=True`
- ไม่มี `NullReferenceException` ใน final validation marker set

## 5. Preventive controls

- command-line runner ระบุ startup scene และ exact discovered map name
- ใช้ Editor mode สำหรับ data parity tests; single-player mode สงวนไว้สำหรับ player/session integration tests ใน phase ที่เหมาะสม
- มี timeout watcher 5 นาทีพร้อมคืน EditorPrefs
- incident นี้ไม่ถูกตีความว่า player gameplay path ผ่าน เพราะ final test จงใจไม่สร้าง player
