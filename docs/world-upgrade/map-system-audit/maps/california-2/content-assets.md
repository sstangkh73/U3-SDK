# California 2 — Content และ Asset Inventory

California 2 แยกคอนเทนต์ออกไปที่ Workshop file `3711646503` ถ้าโหลดเฉพาะโฟลเดอร์แมพจะขาด item, object, NPC, vehicle, effect และ spawn assets จำนวนมาก

## Roots ใน dependency pack

`Animals`, `Assets`, `Effects`, `Items`, `Music`, `NPCs`, `Objects`, `Resources`, `Spawns`, `Vehicles` และ masterbundles

## Semantic item definitions

จำนวนต่อไปนี้นับจากบรรทัด `Type` ใน item definitions ไม่รวม localization files:

| กลุ่ม | Type และจำนวน |
|---|---|
| Survival consumables | Food 193, Water 44, Medical 24, Refill 2, Filter 2 |
| Weapons | Gun 93, Melee 35, Throwable 17, Sentry 12, Trap 14 |
| Weapon parts/ammo | Magazine 62, Sight 22, Barrel 21, Tactical 10, Grip 9, Optic 4 |
| Clothing | Shirt 141, Pants 105, Hat 99, Vest 50, Backpack 30, Mask 29, Glasses 17 |
| Building/storage | Barricade 426, Structure 107, Storage 50, Farm 42, Generator 2, Oil Pump 1, Grower 1 |
| Resources/tools | Supply 132, Tool 5, Fisher 8, Fuel 4, Charge 4, Map 4, Compass 1 |
| Vehicle tools | Vehicle Paint Tool 31, Tank 3, Tire 3, Vehicle Lockpick Tool 3, Vehicle Repair Tool 1 |
| Special | Library 10, Detonator 1, Cloud 1, Beacon 1, Arrest Start/End 1/1 |

## NPC และ vehicle definitions

| Type | จำนวน |
|---|---:|
| NPC | 144 |
| Dialogue | 261 |
| Quest | 156 |
| Vendor | 22 |
| Vehicle | 48 |

## Migration rules

- GUID เป็น identity หลัก; legacy numeric ID เป็น compatibility alias
- importer ต้องบันทึก dependency graph ต่อ world cell เพื่อ reference counting
- asset ที่หลาย zone ใช้ร่วมกันต้องโหลดครั้งเดียวและห้าม unload เมื่อ cell หนึ่งออก
- localization `.dat` ไม่นับเป็น gameplay definition แต่ต้องย้ายพร้อม asset owner
- ก่อน merge California กับแมพอื่น ต้องสร้าง collision report ของ GUID, legacy ID, spawn table ID และ NPC quest IDs
