-- Seed: เพิ่มสถานะ ticket "ยกเลิก"
INSERT INTO tbm_ticket_status (id, name_th, name_en) VALUES (5, 'ยกเลิก', 'Cancelled')
ON CONFLICT (id) DO NOTHING;
