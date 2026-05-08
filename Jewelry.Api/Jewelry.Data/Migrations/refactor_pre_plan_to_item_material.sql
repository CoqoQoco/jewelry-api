-- =============================================
-- Migration: Refactor Pre-Plan (Header → Item → Material)
-- Date: 2026-05-08
-- Description: Drop and recreate pre-plan tables with 3-level hierarchy:
--              TbtProductionPrePlan → TbtProductionPrePlanItem → TbtProductionPrePlanMaterial
-- =============================================

DROP TABLE IF EXISTS "TbtProductionPrePlanMaterial";
DROP TABLE IF EXISTS "TbtProductionPrePlanItem";
DROP TABLE IF EXISTS "TbtProductionPrePlan";

-- =============================================
-- 1. TbtProductionPrePlan (Header)
-- =============================================
CREATE TABLE "TbtProductionPrePlan" (
    "Id"               SERIAL PRIMARY KEY,
    "OrderNo"          VARCHAR(50),
    "ProductionRound"  INT,
    "JobType"          VARCHAR(50),
    "JobLocation"      VARCHAR(50),
    "GoldType"         VARCHAR(20),
    "OrderDate"        TIMESTAMPTZ NOT NULL,
    "DeliveryDate"     TIMESTAMPTZ NOT NULL,
    "Remark"           TEXT,
    "Status"           VARCHAR(20) NOT NULL DEFAULT 'Draft',
    -- Status: Draft, Submitted, Approved, Rejected, Consumed
    "RejectReason"     TEXT,
    "CreateBy"         VARCHAR(50),
    "CreateDate"       TIMESTAMPTZ,
    "UpdateBy"         VARCHAR(50),
    "UpdateDate"       TIMESTAMPTZ,
    "SubmitBy"         VARCHAR(50),
    "SubmitDate"       TIMESTAMPTZ,
    "ApproveBy"        VARCHAR(50),
    "ApproveDate"      TIMESTAMPTZ
);

-- =============================================
-- 2. TbtProductionPrePlanItem
-- =============================================
CREATE TABLE "TbtProductionPrePlanItem" (
    "Id"                     SERIAL PRIMARY KEY,
    "PrePlanId"              INT NOT NULL REFERENCES "TbtProductionPrePlan"("Id") ON DELETE CASCADE,
    "ItemNo"                 INT NOT NULL,
    "MoldCode"               VARCHAR(50) NOT NULL,
    "MoldDetail"             TEXT,
    "ProductType"            VARCHAR(100),
    "ProductQty"             INT,
    "ProductQtyUnit"         VARCHAR(20),
    "ProductDetail"          TEXT,
    "ProductImagePath"       VARCHAR(500),
    "LinkedProductionPlanId" INT NULL,
    "CreateBy"               VARCHAR(50),
    "CreateDate"             TIMESTAMPTZ,
    "UpdateBy"               VARCHAR(50),
    "UpdateDate"             TIMESTAMPTZ
);

CREATE INDEX "IX_PrePlanItem_PrePlanId" ON "TbtProductionPrePlanItem"("PrePlanId");

-- =============================================
-- 3. TbtProductionPrePlanMaterial
-- =============================================
CREATE TABLE "TbtProductionPrePlanMaterial" (
    "Id"                 SERIAL PRIMARY KEY,
    "PrePlanItemId"      INT NOT NULL REFERENCES "TbtProductionPrePlanItem"("Id") ON DELETE CASCADE,
    "Gold"               VARCHAR(50),
    "GoldSize"           VARCHAR(50),
    "GoldQty"            DECIMAL(18,3),
    "Gem"                VARCHAR(50),
    "GemShape"           VARCHAR(50),
    "GemQty"             DECIMAL(18,3),
    "GemUnit"            VARCHAR(20),
    "GemSize"            VARCHAR(50),
    "GemWeight"          DECIMAL(18,3),
    "GemWeightUnit"      VARCHAR(20),
    "DiamondQty"         DECIMAL(18,3),
    "DiamondUnit"        VARCHAR(20),
    "DiamondSize"        VARCHAR(50),
    "DiamondWeight"      DECIMAL(18,3),
    "DiamondWeightUnit"  VARCHAR(20),
    "DiamondQuality"     VARCHAR(50),
    "CreateBy"           VARCHAR(50),
    "CreateDate"         TIMESTAMPTZ
);

CREATE INDEX "IX_PrePlanMaterial_PrePlanItemId" ON "TbtProductionPrePlanMaterial"("PrePlanItemId");
