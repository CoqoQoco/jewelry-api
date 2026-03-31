---
name: md-instruction
description: คู่มือการเขียนและปรับปรุงไฟล์ .md ในโปรเจกต์นี้ — ใช้เมื่อต้องสร้างหรือแก้ไข CLAUDE.md, SKILL.md, หรือ documentation ใดๆ
---

# MD Writing Instruction

## กฎหลัก

1. **CLAUDE.md ห้ามเกิน 200 บรรทัด** — ย้ายรายละเอียดไปที่ skill แทน
2. **ทุก SKILL.md ต้องมี frontmatter** `name` และ `description` ที่ชัดเจน
3. **ห้าม copy เนื้อหาซ้ำ** ระหว่างไฟล์ — ใช้ `@filename` อ้างอิงแทน
4. **Plan ก่อนเสมอ รอ confirm ถึงจะ implement**

## โครงสร้าง CLAUDE.md ที่ดี (WHAT / WHY / HOW)

| Section | เนื้อหา |
|---|---|
| **WHAT** | Tech stack, Layer structure, Directory map |
| **WHY** | Purpose ของแต่ละ layer, Design decisions |
| **HOW** | Build/test commands, Workflows, Gotchas |

## กฎการเขียน

- ใช้ **ภาษาไทย** สำหรับคำอธิบาย, **English** เฉพาะ technical term
- ใช้ table เมื่อมีข้อมูล 3+ แถวที่มีโครงสร้างเหมือนกัน
- ระบุ language ใน code block เสมอ: ` ```csharp `, ` ```bash `, ` ```json `
- ใช้ ✅ / ❌ คู่กันเสมอเมื่อแสดง Good/Bad pattern

## เมื่อต้องเพิ่ม content ใน CLAUDE.md

1. ถามตัวเองว่า "เป็น gotcha/rule สำคัญ หรือเป็น how-to detail?"
   - **Gotcha / rule** → เขียนใน CLAUDE.md (สั้น 1-3 บรรทัด)
   - **How-to detail** → สร้าง skill แล้ว @reference จาก CLAUDE.md
2. ถ้า CLAUDE.md เกิน 200 บรรทัดหลังแก้ → extract section ที่ยาวที่สุดออกเป็น skill ใหม่
