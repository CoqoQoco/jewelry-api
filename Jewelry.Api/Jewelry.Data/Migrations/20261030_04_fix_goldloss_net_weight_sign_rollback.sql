-- =============================================
-- Rollback: 20261030_04_fix_goldloss_net_weight_sign.sql
-- Ticket: TK202610300008
-- Description: คืนค่า net_weight_loss เดิมจากตารางสำรอง แล้วลบตารางสำรอง
-- =============================================

BEGIN;

UPDATE tbt_worker_gold_loss_slip s
SET net_weight_loss = b.net_weight_loss
FROM tbt_worker_gold_loss_slip_netw_bak b
WHERE b.id = s.id;

DROP TABLE tbt_worker_gold_loss_slip_netw_bak;

COMMIT;
