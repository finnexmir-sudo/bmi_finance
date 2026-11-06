-- ============================================================================
-- GOAML — ORA-01031 (insufficient privileges) diaqnostikası
-- ============================================================================
-- Bu skripti ORACLE-DA, FOXPRO istifadəçisi ilə, PL/SQL Developer-də işlət.
-- Hər sətri AYRI-AYRI icra et (hamısını birdən yox) — hansı sətir ORA-01031
-- (və ya ORA-00942 "table or view does not exist", GRANT olmayanda Oracle
-- bəzən bunu göstərir) versə, DƏQİQ O CƏDVƏLƏ icazə çatışmır.
-- ============================================================================

select count(*) from odb.doc_dna_all_tab;      -- 1
select count(*) from odb.doc_dna_all_risk;     -- 2
select count(*) from odb.balch_net;            -- 3
select count(*) from odb.balch_net_deb_kred;   -- 4
select count(*) from odb.arh_dd;               -- 5 — bu onsuz da işlək olmalıdır (müqayisə üçün)

-- ============================================================================
-- Hansı sətir(lər) xəta versə, DBA-dan bunları (yalnız o cədvəllər üçün) istə:
-- ============================================================================
-- GRANT SELECT ON ODB.DOC_DNA_ALL_TAB      TO FOXPRO;
-- GRANT SELECT ON ODB.DOC_DNA_ALL_RISK     TO FOXPRO;
-- GRANT SELECT ON ODB.BALCH_NET            TO FOXPRO;
-- GRANT SELECT ON ODB.BALCH_NET_DEB_KRED   TO FOXPRO;
--
-- Qeyd: bu GRANT-ları ODB sxeminin SAHİBİ (və ya DBA rolu olan bir istifadəçi)
-- icra etməlidir — FOXPRO özü özünə GRANT verə bilməz.
-- ============================================================================
