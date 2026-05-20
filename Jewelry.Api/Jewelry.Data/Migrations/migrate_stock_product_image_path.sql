-- Migration: Migrate Stock Product Image NamePath to filename-only + .jpg
-- Date: 2026-05-18
-- Description: เปลี่ยน NamePath จาก "Stock/{X}.PNG" เป็น "{X}.jpg"
--              เพื่อให้ frontend ImagePreview ต่อ Stock/Product/ prefix ได้ถูกต้อง

UPDATE tbt_stock_product_image
SET name_path = REPLACE(
    REPLACE(REPLACE(name_path, 'Stock/', ''), '.PNG', '.jpg'),
    '.png', '.jpg'
)
WHERE name_path LIKE 'Stock/%';
