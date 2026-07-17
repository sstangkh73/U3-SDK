# Deterministic World Schema

เครื่องมือระยะ 2 แปลง decoded source records เป็น schema ใหม่ที่มี stable world/zone/cell/entity IDs โดยไม่แก้และไม่คัดลอก source map จาก Steam

## Input

- `Builds/WorldUpgrade/WorldLayout.json`: world key, cell size และ explicit zone offsets
- `Builds/WorldUpgrade/WorldSourceCatalog.json`: ตำแหน่ง source map ภายนอก
- `Builds/WorldUpgrade/RawMapManifests/*.raw-map-manifest.json`: source inventory fingerprints
- raw map files ที่ Phase 1 decoder รองรับ

layout ปัจจุบันใช้ California 2 ที่ offset `(0, 0, 0)` และ Limestone ที่ `(7168, 0, 0)` ทำให้ world-space cell bounds มีช่องว่าง 1 cell ระหว่าง zone และไม่ overlap

## Generate

จาก Unity Editor:

```text
Window > Unturned > World Upgrade > Generate Deterministic World Schema
```

จาก command line:

```text
SDG.Unturned.WorldUpgrade.Editor.WorldSchemaGenerator.GenerateFromCommandLine
```

## Output

```text
Builds/WorldUpgrade/WorldSchemas/u3-connected-world/
  world-manifest.json
  generation-evidence.json
  source-update-diff.json
  zones/
    california-2.zone-definition.json
    limestone.zone-definition.json
  cells/
    <zone>/<grid-x>_<grid-z>.world-cell.json
```

manifest, evidence, diff และ zone indices ถูกเก็บใน Git ส่วน cell bundles ราย entity ถูก ignore เพราะมีขนาดประมาณ 131 MiB และสร้างซ้ำได้จาก source map ภายนอก

## Identity rules

- World ID: SHA-256-derived ID จาก canonical `world key`
- Zone ID: world namespace + source `zone key`
- Cell ID: zone namespace + integer grid `(x,z)`
- Object entity ID: zone namespace + object instance ID; ถ้า format เก่าไม่มี instance ID ใช้ region/index source key
- Spawn entity ID: zone namespace + source file + region/index หรือ point index
- Landscape entity ID: zone namespace + landscape tile coordinates

พิกัดติดลบใช้ mathematical floor ดังนั้น `-0.01` อยู่ cell `-1` ไม่ใช่ cell `0`

## Landscape authority

`Level.hierarchy` เป็น authoritative manifest ของ active landscape tiles ส่วนไฟล์ height/splat/hole ที่อยู่บน disk แต่ไม่ได้ถูกอ้างใน hierarchy ถูกเก็บใน decoded diagnostic summary ด้วย `UnreferencedLandscapeSourceTile` แต่ไม่สร้าง world entity วิธีนี้ป้องกัน streamer สร้าง terrain จากไฟล์ค้างที่ runtime เดิมไม่ใช้

## Migration policy

GUID เป็น asset identity หลัก ส่วน legacy ID เป็น compatibility alias เท่านั้น:

- `Unambiguous`: legacy ID มี target เดียวใน zone/kind และ resolve ได้
- `AmbiguousGuidPrimary`: legacy ID ชนหลาย GUID; validator บันทึก warning และปิด legacy-only resolution
- `GuidOnly`: record ไม่มี legacy ID; resolve ด้วย GUID เท่านั้น

นโยบายนี้รักษาข้อมูล Workshop ที่ใช้ legacy IDs ซ้ำโดยไม่เลือก asset ผิดอย่างเงียบ ๆ

## Validation and update diff

generator ตรวจ:

- duplicate stable IDs ทุก namespace
- entity owner cell เทียบกับ local position
- world-space zone bounds overlap
- ambiguous migration aliases
- landscape records ที่ไม่มี heightmap
- repeated build fingerprints ใน process เดียวกัน

ก่อนเขียน output รอบใหม่ generator โหลด schema เดิมและจำแนก entity เป็น `Added`, `Removed`, `Moved`, `Modified` และ `Unchanged` ใน `source-update-diff.json`

ข้อจำกัดปัจจุบัน: hierarchy ถูก decode เฉพาะ type inventory และ authoritative landscape coordinates; hierarchy transforms อื่น, road ownership, environment volumes และ navigation data ยังไม่อยู่ใน schema Phase 3 เพิ่ม single-zone runtime parity แล้ว แต่ proximity/two-zone streaming ยังเป็นงาน Phase 4
