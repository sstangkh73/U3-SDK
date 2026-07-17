# Evidence Index

ไฟล์นี้ชี้หลักฐานที่ใช้สร้าง audit เพื่อให้ตรวจซ้ำได้และไม่พึ่งความจำ

## Installed content

| รายการ | ตำแหน่ง |
|---|---|
| California 2 | `C:/Program Files (x86)/Steam/steamapps/workshop/content/304930/3707778928/California2` |
| California 2 Assets | `C:/Program Files (x86)/Steam/steamapps/workshop/content/304930/3711646503` |
| Limestone | `C:/Program Files (x86)/Steam/steamapps/workshop/content/304930/3565225438/Limestone` |
| Official maps | `C:/Program Files (x86)/Steam/steamapps/common/Unturned/Maps` |

ไฟล์แมพ/Workshop เป็น input แบบ read-only ในรอบ audit นี้ ไม่มีการแก้ไฟล์เกมที่ติดตั้ง

## Source code anchors

| พฤติกรรม | Source |
|---|---|
| Level size constants | `Assets/Runtime/Assembly-CSharp/Unturned/Level/Level.cs:46-50` |
| Level.dat size/type parsing และ version-1 default | `Assets/Runtime/Assembly-CSharp/Unturned/Level/Level.cs:913-929` |
| Active map singleton | `Assets/Runtime/Assembly-CSharp/Unturned/Level/Level.cs:182` |
| Game scene load | `Assets/Runtime/Assembly-CSharp/Unturned/Level/Level.cs:672`, `:697` |
| Unload unused assets | `Assets/Runtime/Assembly-CSharp/Unturned/Level/Level.cs:1940`, `:2284` |
| 64×64 regions / 128-unit region | `Assets/Runtime/Assembly-CSharp/Unturned/Regions/Regions.cs:33-36` |
| Food/water/virus loops | `Assets/Runtime/Assembly-CSharp/Unturned/Player/PlayerLife.cs:1953-2009` |
| Level override numeric conversion | `Assets/Runtime/Assembly-CSharp/Unturned/Provider/Provider.cs:4533-4554` |
| Common then difficulty override order | `Assets/Runtime/Assembly-CSharp/Unturned/Provider/Provider.cs:4569-4579` |
| Item despawn/respawn/target count | `Assets/Runtime/Assembly-CSharp/Unturned/Managers/ItemManager.cs:745-869` |
| Zombie total cap and nav limit | `Assets/Runtime/Assembly-CSharp/Unturned/Managers/ZombieManager.cs:1180-1183` |
| Zombie day/full-moon respawn | `Assets/Runtime/Assembly-CSharp/Unturned/Managers/ZombieManager.cs:1355-1370` |
| Item tables/points files | `Assets/Runtime/Assembly-CSharp/Unturned/Level/LevelItems.cs:183-272` |
| Zombie tables/points files | `Assets/Runtime/Assembly-CSharp/Unturned/Level/LevelZombies.cs:200-360` |
| Animal tables/points file | `Assets/Runtime/Assembly-CSharp/Unturned/Level/LevelAnimals.cs:148-150` |
| Vehicle tables/points file | `Assets/Runtime/Assembly-CSharp/Unturned/Level/LevelVehicles.cs:148-150` |
| Player spawn file | `Assets/Runtime/Assembly-CSharp/Unturned/Level/LevelPlayers.cs:144-146` |

## วิธีนับ asset

- semantic definition count: นับบรรทัด `Type <value>` ใน `.dat` ใต้ root ของระบบนั้น
- ไม่นับ total `.dat` เป็นจำนวน asset เพราะมี localization และ auxiliary data ปะปน
- จำนวน NPC/quest/vendor/vehicle ใน audit จึงเป็น definition count ไม่ใช่ placement count
- file size และ tile count มาจาก filesystem inventory

## ข้อจำกัดของ audit v1

- ยังไม่ได้ decode binary point counts, table weights และ object placements เป็น record รายตัว
- ยังไม่ได้เปิด runtime เพื่อจับ effective config หลัง server overrides
- ยังไม่ได้ยืนยันว่า definition ทุกไฟล์ถูกอ้างถึงโดย placement/spawn table จริง
- ยังไม่ได้วัด frame time, memory residency และ unload behavior
