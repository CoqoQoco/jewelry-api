-- =============================================
-- Migration: UPSERT stock master from staging tables
-- Date: 2026-05-26
-- Description: Import batch 25-5-2026 — รวม mdb Stock 3 batch (9K/14K/18K)
--              INSERT เฉพาะ noproduct ที่ยังไม่มีใน stock master
--              (staging ทั้ง 3 ตารางมี column ORDER ต่างกัน → ต้อง explicit list)
-- Run order: หลัง import 3 staging tables (stock_import_9k/14k/18k) จาก DBeaver
-- Re-run safety: WHERE NOT EXISTS guard — idempotent
-- =============================================

BEGIN;

-- ========== 9K ==========
INSERT INTO stock (
  recgen, stockid, typejob, username, typedesc, dateadd, noproduct, partnoold, whno, partno,
  issueno, typep, jobno, codeproduct, no_code, productname, descthai, dateproduct, status_p,
  quantity, unit, pricecost, pricesale, priceus, remark, ringsize,
  typeg, qtyg, wg, unit1, priceg,
  typed, qtyd, wd, unit2, priced, origin_d,
  typer, qtyr, wr, sizer, unit3, pricer,
  "TypeS", qtys, ws, sizes, unit4, prices,
  typee, qtye, we, sizee, unit5, pricee,
  typem, qtym, wm, sizem, unit6, pricem,
  typed1, qtyd1, wd1, unitd1, priced1, origin_d1,
  typesil, wsil, unitsil
)
SELECT DISTINCT ON (s.noproduct)
  s.recgen, s.stockid, s.typejob, s.username, s.typedesc, s.dateadd, s.noproduct, s.partnoold, s.whno, s.partno,
  s.issueno, s.typep, s.jobno, s.codeproduct, s.no_code, s.productname, s.descthai, s.dateproduct, s.status_p,
  s.quantity, s.unit, s.pricecost, s.pricesale, s.priceus, s.remark, s.ringsize,
  s.typeg, s.qtyg, s.wg, s.unit1, s.priceg,
  s.typed, s.qtyd, s.wd, s.unit2, s.priced, s.origin_d,
  s.typer, s.qtyr, s.wr, s.sizer, s.unit3, s.pricer,
  s."TypeS", s.qtys, s.ws, s.sizes, s.unit4, s.prices,
  s.typee, s.qtye, s.we, s.sizee, s.unit5, s.pricee,
  s.typem, s.qtym, s.wm, s.sizem, s.unit6, s.pricem,
  s.typed1, s.qtyd1, s.wd1, s.unitd1, s.priced1, s.origin_d1,
  s.typesil, s.wsil, s.unitsil
FROM stock_import_9k s
WHERE s.noproduct IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM stock m WHERE m.noproduct = s.noproduct);

-- ========== 14K (note: staging มีคอลัมน์ origin_d11 ไม่ตรงกับ master; typedesc ไม่มี → NULL) ==========
INSERT INTO stock (
  recgen, stockid, typejob, username, typedesc, dateadd, noproduct, partnoold, whno, partno,
  issueno, typep, jobno, codeproduct, no_code, productname, descthai, dateproduct, status_p,
  quantity, unit, pricecost, pricesale, priceus, remark, ringsize,
  typeg, qtyg, wg, unit1, priceg,
  typed, qtyd, wd, unit2, priced, origin_d,
  typer, qtyr, wr, sizer, unit3, pricer,
  "TypeS", qtys, ws, sizes, unit4, prices,
  typee, qtye, we, sizee, unit5, pricee,
  typem, qtym, wm, sizem, unit6, pricem,
  typed1, qtyd1, wd1, unitd1, priced1, origin_d1,
  typesil, wsil, unitsil
)
SELECT DISTINCT ON (s.noproduct)
  s.recgen, s.stockid, s.typejob, s.username, NULL::varchar AS typedesc, s.dateadd, s.noproduct, s.partnoold, s.whno, s.partno,
  s.issueno, s.typep, s.jobno, s.codeproduct, s.no_code, s.productname, s.descthai, s.dateproduct, s.status_p,
  s.quantity, s.unit, s.pricecost, s.pricesale, s.priceus, s.remark, s.ringsize,
  s.typeg, s.qtyg, s.wg, s.unit1, s.priceg,
  s.typed, s.qtyd, s.wd, s.unit2, s.priced, s.origin_d,
  s.typer, s.qtyr, s.wr, s.sizer, s.unit3, s.pricer,
  s."TypeS", s.qtys, s.ws, s.sizes, s.unit4, s.prices,
  s.typee, s.qtye, s.we, s.sizee, s.unit5, s.pricee,
  s.typem, s.qtym, s.wm, s.sizem, s.unit6, s.pricem,
  s.typed1, s.qtyd1, s.wd1, s.unitd1, s.priced1, s.origin_d1,
  s.typesil, s.wsil, s.unitsil
FROM stock_import_14k s
WHERE s.noproduct IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM stock m WHERE m.noproduct = s.noproduct);

-- ========== 18K ==========
INSERT INTO stock (
  recgen, stockid, typejob, username, typedesc, dateadd, noproduct, partnoold, whno, partno,
  issueno, typep, jobno, codeproduct, no_code, productname, descthai, dateproduct, status_p,
  quantity, unit, pricecost, pricesale, priceus, remark, ringsize,
  typeg, qtyg, wg, unit1, priceg,
  typed, qtyd, wd, unit2, priced, origin_d,
  typer, qtyr, wr, sizer, unit3, pricer,
  "TypeS", qtys, ws, sizes, unit4, prices,
  typee, qtye, we, sizee, unit5, pricee,
  typem, qtym, wm, sizem, unit6, pricem,
  typed1, qtyd1, wd1, unitd1, priced1, origin_d1,
  typesil, wsil, unitsil
)
SELECT DISTINCT ON (s.noproduct)
  s.recgen, s.stockid, s.typejob, s.username, s.typedesc, s.dateadd, s.noproduct, s.partnoold, s.whno, s.partno,
  s.issueno, s.typep, s.jobno, s.codeproduct, s.no_code, s.productname, s.descthai, s.dateproduct, s.status_p,
  s.quantity, s.unit, s.pricecost, s.pricesale, s.priceus, s.remark, s.ringsize,
  s.typeg, s.qtyg, s.wg, s.unit1, s.priceg,
  s.typed, s.qtyd, s.wd, s.unit2, s.priced, s.origin_d,
  s.typer, s.qtyr, s.wr, s.sizer, s.unit3, s.pricer,
  s."TypeS", s.qtys, s.ws, s.sizes, s.unit4, s.prices,
  s.typee, s.qtye, s.we, s.sizee, s.unit5, s.pricee,
  s.typem, s.qtym, s.wm, s.sizem, s.unit6, s.pricem,
  s.typed1, s.qtyd1, s.wd1, s.unitd1, s.priced1, s.origin_d1,
  s.typesil, s.wsil, s.unitsil
FROM stock_import_18k s
WHERE s.noproduct IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM stock m WHERE m.noproduct = s.noproduct);

-- Verify
SELECT
  (SELECT COUNT(*) FROM stock) AS stock_after,
  (SELECT COUNT(DISTINCT noproduct) FROM stock) AS distinct_noproduct;

COMMIT;

-- หลังเสร็จ ลบ staging tables (run แยกเมื่อ verify ok แล้ว):
-- DROP TABLE stock_import_9k, stock_import_14k, stock_import_18k;
