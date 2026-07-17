# Limestone — Content และ Asset Inventory

คอนเทนต์ส่วนใหญ่รวมอยู่ภายใน workshop item ของ Limestone ใต้ roots เช่น `ADS`, `Animals`, `Assets`, `Effects`, `I`, `NPC`, `Objects`, `Resources`, `Spawn_Tables`, `Vehicles`

## Semantic item definitions

| กลุ่ม | Type และจำนวน |
|---|---|
| Survival consumables | Food 67, Water 23, Medical 22, Refill 4, Filter 2 |
| Weapons | Gun 93, Melee 44, Throwable 10, Sentry 6, Trap 9 |
| Weapon parts/ammo | Magazine 83, Sight 34, Barrel 9, Tactical 20, Grip 53, Optic 2 |
| Clothing | Shirt 81, Pants 164, Hat 122, Vest 106, Backpack 26, Mask 7, Glasses 15 |
| Building/storage | Barricade 214, Structure 78, Storage 38, Farm 7, Generator 2, Oil Pump 1, Grower 1 |
| Resources/tools | Supply 77, Tool 6, Fisher 1, Fuel 2, Charge 2, Map 3, Compass 1 |
| Vehicle tools | Vehicle Paint Tool 19, Tank 4, Vehicle Repair Tool 1 |
| Special | Cloud 11, Detonator 1, Beacon 1, Arrest Start/End 1/1 |

## NPC และ vehicle definitions

| Type | จำนวน |
|---|---:|
| NPC | 2 |
| Dialogue | 25 |
| Quest | 13 |
| Vendor | 13 |
| Vehicle | 19 |

## Migration rules

- สร้าง catalog จาก GUID/type ไม่ใช้ชื่อโฟลเดอร์ `I` หรือ numeric ID เป็น identity หลัก
- แยก definition files ออกจาก localization ก่อนสร้างจำนวนและ hash
- ทำ collision report กับ California assets ก่อนเปิดพร้อมกัน
- asset dependency ต่อ cell ต้องรองรับ shared references และ deterministic unload
