#### ช่วยเขียนเเละเเก้ไข code ตามรูปเเบบใน E:\coqo_duangkeaw\Code\jewelry-api\Jewelry.Api\CLAUDE.md เท่านั้นครับ
>> ปรับปรุงการ upload  image จาก local folder ไปยัง azure blob storage โดยใช้ Azure.Storage.Blobs library ใน .NET 8

  - ตรวจสอบกสนอัพโหลดรูปใน project ก่อนว่ามีกี่จุดโดยใช้ key  "image.CopyTo"
  - ตรวจสอบ folder ที่บันทึก เราจะใช้ folder เดิม ในการเก็บรูปที่อัพโหลดจาก local ก่อนที่จะส่งไปยัง Azure Blob Storage
  - สรุปผลการค้นหาก่อนว่ามี กี่จุดครับ