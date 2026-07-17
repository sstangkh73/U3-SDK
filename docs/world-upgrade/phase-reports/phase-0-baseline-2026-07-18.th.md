# รายงานผลระยะ 0: Baseline และการปกป้องงาน

**วันที่ทดสอบ:** 18 กรกฎาคม 2026  
**Branch:** `codex/world-travel-mvp`  
**ผล gate:** ผ่านและเผยแพร่แล้ว  
**ขอบเขต:** World Travel MVP, Raw Map Importer, generated manifests, system audit และเอกสารแผนงาน

**Baseline commit:** [`41bb305`](https://github.com/sstangkh73/U3-SDK/commit/41bb305)  
**Remote branch:** `sstangkh73/U3-SDK:codex/world-travel-mvp`  
**Draft PR:** [sstangkh73/U3-SDK#1](https://github.com/sstangkh73/U3-SDK/pull/1)  

## 1. วัตถุประสงค์

ระยะ 0 มีหน้าที่ทำให้งานต้นแบบที่กระจายอยู่ใน working tree กลายเป็น baseline ที่ตรวจซ้ำและเผยแพร่ได้ โดยต้องไม่รวมไฟล์ Unity cache, local editor settings, save/logs หรือ asset ต้นฉบับของแมพจาก Steam/Workshop

## 2. สิ่งที่ทำในระยะนี้

- ตรวจรายการ tracked และ untracked files ทั้งหมดก่อน stage
- แยก `.vscode/` ออกจาก source control ผ่าน `.gitignore`
- ยืนยันว่า `Library/`, `Logs/`, `UserSettings/`, save data และเกม logs ถูก ignore อยู่แล้ว
- ตรวจ pattern ของ credential/secret ใน source, configs, manifests และ docs โดยไม่พบข้อมูลลับ
- สร้าง fork `sstangkh73/U3-SDK` สำหรับเผยแพร่งาน โดยคง `SmartlyDressedGames/U3-SDK` เป็น upstream
- รวบรวม World Travel MVP, World Upgrade importer, manifest ทั้ง 7 แมพ และเอกสาร audit เป็น baseline เดียว
- ใช้ Unity version เดียวกับ `ProjectSettings/ProjectVersion.txt` คือ `2022.3.62f3`

## 3. ขอบเขตไฟล์

### รวมใน baseline

- การติดตั้งและ resume `WorldTravelManager` ใน lifecycle ของเกม
- source ของ `WorldTravel/`
- `Builds/Shared/WorldTravel.json`
- source ของ Editor/Runtime `WorldUpgrade/`
- `Builds/WorldUpgrade/WorldSourceCatalog.json`
- generated raw-map manifests 7 ไฟล์
- ADR, audit, architecture, full project report และ phase reports
- `.gitignore` ที่เพิ่ม `/.vscode`

### ไม่รวมใน baseline

- `.vscode/`
- `Library/`, `Logs/`, `UserSettings/`
- `Builds/Shared/Logs`, saves, worlds และ cloud data
- raw terrain, bundles หรือ copyrighted map assets จาก Steam/Workshop
- binaries และ project files ที่ Unity/IDE สร้างใหม่ได้

## 4. การทดสอบ

### 4.1 Unity batch compile และ importer execution

รัน Unity 2022.3.62f3 แบบ `-batchmode -nographics` พร้อม execute method:

```text
SDG.Unturned.WorldUpgrade.Editor.RawMapManifestGenerator.GenerateFromCommandLine
```

ผล:

- Tundra script build สำเร็จ
- ไม่พบ `error CSxxxx`, compilation failure หรือ unhandled exception ใน batch log
- สร้าง raw map manifests ครบ 7 ไฟล์
- process ปิดตัวหลัง execute method เสร็จ

Batch log ถูกเก็บใน `Logs/phase0-unity-batch.log` ซึ่งเป็น local evidence และไม่ commit เพราะ `Logs/` ถูก ignore

### 4.2 Manifest validation

| รายการ | ผล |
|---|---:|
| Catalog schema | 1 |
| Maps ใน catalog | 7 |
| Zone IDs ไม่ซ้ำ | 7/7 |
| Zone IDs ผ่านรูปแบบ `^[a-z0-9-]+$` | 7/7 |
| Source roots ที่มีอยู่จริง | 7/7 |
| Generated manifests | 7/7 |
| Files ที่ catalog รวม | 8,125 |
| ขนาดข้อมูลต้นทางที่ inventory | 2,459,187,789 bytes (ประมาณ 2.29 GiB) |
| Fingerprints รูปแบบ SHA-256 | 7/7 |
| `CopiesSourceContent` | false ทุกแมพ |
| `ImportsLegacyGameplayRules` | false ทุกแมพ |

### 4.3 Runtime evidence ที่รักษาไว้ใน baseline

`Builds/Shared/Logs/Client.log` ซึ่งเป็นไฟล์ local/ignored มีหลักฐาน `Completed world travel` 3 รายการจากการทดสอบก่อนหน้า:

- California 2 → Limestone สำเร็จ 2 ครั้ง
- Limestone → California 2 สำเร็จ 1 ครั้ง

หลักฐานนี้ยืนยัน flow ต้นแบบ ณ เวลาทดสอบ แต่ไม่ใช่ automated test และไม่ได้ถูก commit

## 5. ปัญหาที่พบและการแก้ไข

### GitHub CLI ไม่อยู่ใน PATH ของ process เดิม

หลังติดตั้ง GitHub CLI shell ของ Codex ยังไม่เห็น PATH ใหม่ แก้โดยเรียก executable ที่ `C:/Program Files/GitHub CLI/gh.exe` โดยตรง ยืนยันเวอร์ชัน 2.96.0 และบัญชี `sstangkh73` authenticated แล้ว

### Remote เดิมชี้ไป upstream ทางการ

`origin` เดิมคือ `SmartlyDressedGames/U3-SDK` จึงไม่ควรใช้เป็นปลายทาง push งานส่วนตัว แก้โดยสร้าง fork `sstangkh73/U3-SDK`, ใช้ fork เป็น `origin` และเก็บ repository ทางการเป็น `upstream`

### `.vscode` ยังไม่ถูก ignore

พบไฟล์ local launch/settings ของ VS Code เป็น untracked จึงเพิ่ม `/.vscode` ใน `.gitignore` และไม่นำเข้า baseline

## 6. ข้อจำกัดที่ยังเหลือ

- Runtime evidence ของ World Travel มาจาก manual test เดิมเพียง 3 เที่ยว
- Phase 0 ยังไม่มี Unity Test Framework suite สำหรับ source ที่เพิ่ม
- Manifest fingerprint ปัจจุบัน hash เฉพาะ relative path และ file size ไม่ได้ hash file content
- Source catalog ยังมี absolute path ที่ผูกกับเครื่องปัจจุบัน
- World Travel ยังเป็น full level hand-off และรองรับ singleplayer เท่านั้น
- Editor lifecycle exceptions ที่เคยพบใน interactive log ต้องติดตามเมื่อเริ่ม streaming/unload ถี่ขึ้น

## 7. เกณฑ์ผ่าน

| เกณฑ์ | ผล |
|---|---|
| ขอบเขต source ชัดและไม่มี local cache | ผ่าน |
| ไม่มี raw map assets ถูกเพิ่ม | ผ่าน |
| Unity compile ผ่าน | ผ่าน |
| Importer สร้าง manifest ครบ | ผ่าน |
| JSON/safety validation ผ่าน | ผ่าน |
| มีรายงานข้อจำกัดและหลักฐาน | ผ่าน |
| มี baseline บน GitHub | ผ่าน — commit `41bb305`, Draft PR #1 |

## 8. ข้อสรุป

Baseline ถูก commit และ push ไปยัง fork ของผู้ใช้แล้ว พร้อม Draft PR สำหรับติดตาม diff ระยะ 1 สามารถเริ่มจากการสร้าง binary decoders และ automated validators โดยอ้าง schema, source catalog และ manifest ที่ตรวจในระยะนี้
