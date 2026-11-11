/* ============================================================================
   GOAML — JSON_MMX_IN prosedurunun TAM mənbə kodu (11.11.2026, arxiv məqsədli)

   BU FAYL İCRA ÜÇÜN DEYİL — yalnız ARXİV/İSTİNAD üçündür. Oracle-ın ÖZ
   `ODB.JSON_MMX_IN` prosedurudur ("gələn" əməliyyat üçün goAML JSON
   hesabatını yaradan AYSEL prosedur ailəsindən biri — bax CLAUDE.md,
   "GOAML — Əməliyyatlar Tabı..." bölmələri). `all_source`-dan
   (`owner='ODB'`, `name='JSON_MMX_IN'`, `line` sırası ilə) 11.11.2026-da
   istifadəçi tərəfindən PL/SQL Developer-də çəkilib, BURAYA olduğu kimi
   köçürülüb.

   NİYƏ SAXLANILIR: Export to file (#15 tapşırığı) üçün bu, TAM blueprint-dir —
   prosedur `GOAML_CLIENTS` cədvəlindən ("Update New" kaskadının doldurduğu
   eyni cədvəl) oxuyur, `huquqi_shexs`/`fiziki_shexs`/`imza_huquqi_olan_
   shexsler`/`r_emitent_benefisiar`/`pv_regnom_all` cədvəllərindən KYC çəkir,
   `UTL_FILE` ilə birbaşa `.JSON` faylına yazır. FinNex-in öz Export
   mexanizmini qurarkən bu, HƏR sahənin HARADAN gəldiyini göstərən əsas
   mənbədir.

   ⚠️ `dubl_number` sahəsinin TAM izahı (05-11.11.2026 araşdırmasının nəticəsi):
   `sender_account_dubl_number` / `receiver_account_dubl_number` =
   `huquqi_shexs.REGISTRATION_NUMBER` (VÖEN-dən AYRI, hüquqi şəxsin İKİNCİ
   dövlət qeydiyyat nömrəsi), `C_REGISTRATION_NUMBER(P_VOEN)` kursoru ilə
   VÖEN üzrə tapılır. Göndərən tərəfdə yalnız mübadilə/kart əməliyyatı
   OLMAYANDA, alan tərəfdə `report_code<>'CTR' OR tip IN (2,4,8)` olanda
   doldurulur (bax sətir ~1189-1227 və ~1355-1387).

   ⚠️ Fayl adı formatı (sətir 436):
   v_bank||'_'||v_report_code||P_TIP_FI||V_KOL||'_'||V_SEQ||'_'||
   to_char(SYSDATE,'YYYYMMDD')||'_TEST.JSON'
   — test faylların adları ("BMI_BAKU_ETR_HI_1_15556_20261105_TEST.JSON",
   "BMI_BAKU_BLR_HF_IN_1_15565_20261106_TEST.JSON") bununla TAM üst-üstə düşür.
   ========================================================================== */

PROCEDURE JSON_MMX_IN
 (p_date_S DATE,  p_date_PO DATE, v_BANK varchar2, v_report_code  VARCHAR2, V_tip VARCHAR2, P_TIP_FI VARCHAR2)
   --( p_DATE_S DATE, p_DATE_PO DATE, p_nov varchar2, v_report_code  VARCHAR2, v_email varchar2, v_BANK varchar2 )
    AS
--SELECT * FROM S1TRNSC
 -- str1           CLOB;
  v_file         UTL_FILE.FILE_TYPE;
  v_name         VARCHAR2(255);

--
v_sendDate      varchar2(200); --"2024-06-13T11:29:58.335Z"
v_reportType    varchar2(200); --1
v_reportReason  varchar2(200); --1

  --  "transactions": [
 --      "Transaction": {
v_trn_status       varchar2(200);--"EXC",
v_trn_actual_date  varchar2(200);--"2024-06-13T11:29:58.335Z",
v_trn_bank_date    varchar2(200);--"2024-06-13T11:29:58.335Z",
v_trn_external_number  varchar2(200);--": "string value",
v_trn_internal_number  varchar2(200);--": "string value",
v_trn_type             varchar2(200);--": "CSH",
v_trn_payment_system   varchar2(200);--": "SWF",
v_trn_payment_sub_system varchar2(200);--": "VIS",
   --     "Purpose": { Цель
v_trn_description        varchar2(200);--": "text value",
v_trn_detailed_id        varchar2(200); --": "string value",
v_trn_fund_source        varchar2(200);--": "string value"
v_trn_chanel             varchar2(200);--": "ATM"

  --  "Representative": { Представитель
v_repr_status            varchar2(200);--": "PAT",
v_repr_citizenship       varchar2(200);--": "AFG",
v_repr_id_serial         varchar2(200);--": "string value",
v_repr_id_number         varchar2(200);--": "string value",
v_repr_id_issue_date     varchar2(200);--": "2024-06-13T11:29:58.335Z",
v_repr_id_expiry_date    varchar2(200);--": "2024-06-13T11:29:58.335Z",
v_repr_birth_date        varchar2(200);--": "2024-06-13T11:29:58.335Z",
v_repr_phone             varchar2(200);--": "string value",
v_repr_pin               varchar2(200);--": "string value",
v_repr_first_name        varchar2(200);--": "string value",
v_repr_last_name         varchar2(200);--": "string value",
v_repr_middle_name       varchar2(200);--": "string value",
v_repr_id_type           varchar2(200);--": "DOM"

   --     "Sender": {
v_sen_country_name    varchar2(200);--": "AFG",
---          "Entity": { Юридическое лицо
v_sen_entity_name     varchar2(200);--": "string value",
v_sen_entity_reg_country  varchar2(200);--": "AFG",
v_sen_entity_tax_number   varchar2(200);--": "string value"

   --       "Person": {
v_sen_first_name          varchar2(200);--": "string value",
v_sen_last_name           varchar2(200);--": "string value",
v_sen_middle_name         varchar2(200);--": "string value",
v_sen_citizenship         varchar2(200);--": "AFG",
v_sen_id_serial           varchar2(200);--": "string value",
v_sen_id_number           varchar2(200);-- ": "string value",
v_sen_pin_code            varchar2(200);--": "string value",
v_sen_id_issue_date       varchar2(200);--": "2024-06-13T11:29:58.335Z",
v_sen_id_expiry_date      varchar2(200);--": "2024-06-13T11:29:58.335Z",
v_sen_birth_date          varchar2(200);---": "2024-06-13T11:29:58.335Z",
v_sen_phone_number        varchar2(200);--": "string value",
v_sen_tax_id              varchar2(200);--": "string value",
v_sen_id_type             varchar2(200);--": "DOM"
---
 --         "Funds": {
v_sen_curr_type           varchar2(200);--": "AED",
v_sen_curr_amount         varchar2(200);--": 1,
v_sen_amount_local        varchar2(200);--": 1

 --         "Account": {
v_sen_account_number          varchar2(200);--": "string value",
v_sen_account_dubl_number     varchar2(200);--": "string value",
v_sen_account_currency        varchar2(200);--": "AED",
v_sen_account_pan_id_number   varchar2(200);--": "string value",
v_sen_account_3d_secure       varchar2(200);--": "string value",
v_sen_account_fin_inst_country  varchar2(200);--": "AFG",
v_sen_account_fin_inst_bic_id   varchar2(200);--": "string value",

v_sen_account_fin_inst_name     varchar2(200);--": "string value",
v_sen_account_fin_inst_branch   varchar2(200);--": "string value",
v_sen_account_fin_inst_bic      varchar2(200);--": "string value",
v_sen_account_fin_inst_tax_id   varchar2(200);--": "string value"

 ---         "Payment_System": {
v_sen_merchant_name             varchar2(200);--": "string value",
v_sen_merchant_code             varchar2(200);--": "string value",
v_sen_ekvayer_bank              varchar2(200);--": "string value",
v_sen_mcc_code                  varchar2(200);--": "string value"

   ---     "Receiver": {
v_rec_country_name            varchar2(200);--": "AFG",
     ---     "Entity": {
v_rec_entity_name             varchar2(200);--": "string value",
v_rec_entity_reg_country      varchar2(200);--": "AFG",
v_rec_entity_tax_number       varchar2(200);--": "string value"
--
  --        "Person": {
v_rec_first_name              varchar2(200);--": "string value",
v_rec_last_name               varchar2(200);--": "string value",
v_rec_middle_name             varchar2(200);--": "string value",
v_rec_citizenship             varchar2(200);--": "AFG",
v_rec_id_serial               varchar2(200);--": "string value",
v_rec_id_number               varchar2(200);--": "string value",
v_rec_pin_code                varchar2(200);--": "string value",
v_rec_id_issue_date           varchar2(200);--": "2024-06-13T11:29:58.335Z",
v_rec_id_expiry_date          varchar2(200);--": "2024-06-13T11:29:58.335Z",
v_rec_phone_number            varchar2(200);--": "string value",
v_rec_tax_id                  varchar2(200);--": "string value",
v_rec_birth_date              varchar2(200);--": "2024-06-13T11:29:58.335Z",
v_rec_id_type                 varchar2(200);--": "DOM"

     --     "Funds": {
v_rec_curr_type               varchar2(200);--": "AED",
v_rec_curr_amount             varchar2(200);--": 1,
v_rec_amount_local            varchar2(200);--": 1

   ---       "Account": {
v_rec_account_number          varchar2(200);--": "string value",
v_rec_account_dubl_number     varchar2(200);--": "string value",
v_rec_account_currency        varchar2(200);--": "AED",
v_rec_account_pan_id_number   varchar2(200);--": "string value",
v_rec_account_3dsecure        varchar2(200);--": "string value",
v_rec_account_fin_inst_name   varchar2(200);--": --"string value",
v_rec_account_fin_inst_country  varchar2(200);--": "AFG",
v_rec_account_fin_inst_tax_id   varchar2(200);--": "string value",
v_rec_account_fin_inst_bic_id   varchar2(200);--": "string value",
v_rec_account_fin_inst_branch   varchar2(200);--": "string value",
v_rec_account_fin_inst_bic      varchar2(200);--": "string value"

     ---     "Payment_System": {
v_rec_merchant_code    varchar2(200);--": "string value",
v_rec_merchant_name    varchar2(200);--": "string value",
v_rec_ekvayer_bank     varchar2(200);--": "string value",
v_rec_mcc_code         varchar2(200);--": "string value"

    --    "Correspondence": {
v_debet    varchar2(200);--": "string value",
v_Credit   varchar2(200); --": "string value"
---

cursor c_goal is
select * from goaml_clients s
where s.date_oper BETWEEN p_date_S AND  p_date_PO
AND VIP_NET_DA =1
and TIP=V_tip
AND REPORT_CODE=v_report_code
--AND DEBET='41020000019010000001'
;

v_goal c_goal%ROWTYPE;


cursor c_S3C (P_HESAB VARCHAR2) is
select * from S3CONTRA C
where C.ODB_NUMBER=P_HESAB AND ROWNUM=1
;

V_S3C c_S3C%ROWTYPE;

----

cursor C_KOL  is
 SELECT COUNT(TRANSACTIONNUMBER)
   from ODB.GOAML_CLIENTS S
           where s.date_oper BETWEEN p_date_S AND  p_date_PO
AND VIP_NET_DA =1
and TIP=V_tip
AND REPORT_CODE=v_report_code
--AND DEBET='41020000019010000001'
             ;

V_KOL NUMBER;
--
V_SEQ  NUMBER;
-------

cursor c_MN is
SELECT * FROM odb.Malumatin_novu S
WHERE S.ISARE=v_report_code
 AND ROWNUM=1
;

V_MN c_MN%ROWTYPE;

----
cursor c_log(p_renum varchar2/*,P_date DATE*/ ) is

select to_char(S.DATETIMEDOCINS, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"') AS DATETIMEDOCINS,
to_char(S.DATETIMEDOCEXEC, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"') AS DATETIMEDOCEXEC
from DOCDNA_INFO S --log_docdna_all s
 where /*s.date_oper=P_date --
and*/ s.RECNUM=p_renum
--and s.STATUS='INSERT'
;

V_log c_log%ROWTYPE;

----
V_START  NUMBER;
---
CURSOR C_REGISTRATION_NUMBER (P_VOEN VARCHAR2) IS
SELECT DISTINCT S.REGISTRATION_NUMBER
 FROM huquqi_shexs S
  WHERE S.QEYDIYYAT_NOMRESI_VE_YA_VOEN=P_VOEN;

  V_REGISTRATION_NUMBER_REC  VARCHAR2(200);
  V_REGISTRATION_NUMBER_SEN  VARCHAR2(200);

   -------
   CURSOR C_fizik  (P_filialnumber VARCHAR2, p_regnom varchar2 , p_fin varchar2) IS
   select * from fiziki_shexs s
    where   s.REGNOM = p_regnom
     and s.FIN=p_fin
   ;
   v_fizik C_fizik%ROWTYPE;
   -------
   CURSOR C_REGNUMBER (P_filialnumber VARCHAR2, p_regnom varchar2) IS
SELECT *
 FROM pv_regnom_all S
  WHERE S.FILIALNUMBER=P_FILIALNUMBER
  and s.REGNOM =p_regnom
  ;
  v_REGNUMBER C_REGNUMBER%ROWTYPE;

  v_REGNUMBER_cif C_REGNUMBER%ROWTYPE;
   -------
   CURSOR C_imza_huquq (P_filialnumber VARCHAR2, p_regnom varchar2, p_fin varchar2) IS
SELECT *
 FROM imza_huquqi_olan_shexsler S
  WHERE   s.REGNOM =p_regnom
  and s.FIN = p_fin
  ;
  v_imza_huquq C_imza_huquq%ROWTYPE;

  v_typeOfConnectionToPep varchar2(200);
   ----
  CURSOR C_emitent_benefisiar (P_filialnumber VARCHAR2, p_transaction varchar2) IS
SELECT *
 FROM r_emitent_benefisiar S
  WHERE S.FILIALNUMBER=P_FILIALNUMBER
  and s.DOC_ID =p_transaction
  ;
  v_emitent_benefisiar C_emitent_benefisiar%ROWTYPE;
  ------
  -----------
CURSOR c_PV_HUQUQI (P_VOEN VARCHAR2, p_Regnom VARCHAR2) IS
SELECT *
 FROM huquqi_shexs S
  WHERE S.QEYDIYYAT_NOMRESI_VE_YA_VOEN=P_VOEN
  and s.REGNOM=p_Regnom;

  v_PV_HUQUQI  c_PV_HUQUQI%ROWTYPE;


  -------
   ----
   /*
  cursor c_mcc (p_transaction_number varchar2) IS
  select SUBSTR(D.TRANSACTION_DETAILS,1,199) AS MERCHAT_NAME,
         D.MERCHANT_ID AS merchant_code,
         D.ACQUIRER AS Bank,
         d.merchant_category_code AS mcc_code
         from odb.arh_f11crd d ---F12  INCOM_FLEL
 where TRIM(d.transaction_number)=TRIM(p_transaction_number)
 --'3857635200D'
  ;

  v_merchant_name varchar2(200);
  v_merchant_code varchar2(200);
  v_ekvayer_bank  varchar2(200);
  v_mcc_code      varchar2(200);
  */
  ----
  /*
  CURSOR C_subsytem(P_ODB_NUMBER VARCHAR2) IS
  select f.service_name
 from s1cardsc_structure s , s1cardsc f, s3contra  g
where s.structure_id=f.card_structure and g.odb_number=P_ODB_NUMBER
and g.package_of_services=f.package_of_services ;

V_service_name  VARCHAR2(4000);
*/
------
/*
cursor c_risk (p_risk number) is
select s.kateq from kateq_setup s
where s.riskler is not null
 and s.riskler = p_risk ;

 v_risk c_risk%ROWTYPE;
 */
 v_customerRiskDegree number;
 v_customerRiskDegree_cif number;

-----

cursor c_pla_sys (p_id varchar2, p_acccount varchar2, p_datoper date) is

SELECT id,
       d.message_id,
       CASE plat_system
            WHEN 0 THEN 'XKS'
            WHEN 1 THEN 'SWF'
            WHEN 2 THEN 'CBB'
            WHEN 4 THEN 'AZP'
            WHEN 6 THEN 'AOS'
            ELSE TO_CHAR(d.plat_system)
       END AS plat_system
FROM doc_vnesh_postupl d
where date_oper =p_datoper
and d.id = p_id
and d.kredit like '%'|| p_acccount ||'%'

UNION ALL

SELECT id,
       b.message_id,
       CASE plat_system
            WHEN 0 THEN 'XKS'
            WHEN 1 THEN 'SWF'
            WHEN 2 THEN 'CBB'
            WHEN 3 THEN 'APUS'
            WHEN 4 THEN 'AZP'
            WHEN 5 THEN 'HOP'
            WHEN 6 THEN 'AOS'
            ELSE TO_CHAR(plat_system)
       END AS plat_system
FROM doc_Vnesh_nacval b
where date_oper =p_datoper
and id = p_id
and b.debet=p_acccount

UNION ALL

SELECT id,
       v.message_id,
       CASE plat_system
            WHEN 0 THEN 'SWF'
            WHEN 1 THEN 'VBT'
            ELSE TO_CHAR(plat_system)
       END AS plat_system
FROM doc_Vnesh_inval v
where date_oper =p_datoper
and id = p_id
and v.account_no=p_acccount

UNION ALL

SELECT id,
       to_char(id) as message_id,
       CASE plat_system
            WHEN 0 THEN 'SWF'
            WHEN 1 THEN 'VBT'
            ELSE TO_CHAR(plat_system)
       END AS plat_system
FROM doc_Vnesh_swift s
where date_oper =p_datoper
and id = p_id
and s.beneficiary_account like '%'||p_acccount || '%'
;



v_pla_sys c_pla_sys%ROWTYPE;

--------------
CURSOR C_INN_BANK (P_BANK VARCHAR2)
IS
SELECT DISTINCT INN FROM MFO S WHERE S.BIC_SWIFT LIKE  P_BANK||'%';

V_INN_BANK C_INN_BANK%ROWTYPE;
-----------
   CURSOR C_BANK_NAME IS
   SELECT * FROM ATTRIB;

V_ATTRIB C_BANK_NAME%ROWTYPE;
-----------
CURSOR C_YURIK__NAME (P_LICSCH VARCHAR2) IS
SELECT S.INN_LICSCH, S.NAME_LICSCH, S.LICSCH,S.REGISTRAC_NOMER, S.COUNTRYCODE, F.BANK_LARGE_NAME  AS BANK_BRANCH, 'Premium Bank ASC' AS BANK , F.INN, F.BIC_SWIFT
FROM ODB.VIEW_LICSCH_ALL S , ODB.MFO F
WHERE S.LICSCH=P_LICSCH
AND S.MFO=F.MFO
 AND SUBSTR(LICSCH,1,5) IN (select S.BALSCHT from ODB.BALSCHT_UR S);

V_YURIK__NAME C_YURIK__NAME%ROWTYPE;

---------------
-----------
v_individualPerson VARCHAR2(20);

 v_typeOfConnectionToHrz VARCHAR2(20);
-----------

 cursor c_cif (p_fin varchar2)is
 select * from fiziki_shexs p
where p.FIN =p_fin;

v_cif c_cif%ROWTYPE;

BEGIN
  /*
  SELECT * FROM DOC_VNESH_INVAL_INFO

SELECT * FROM DOCDNA_INFO
  */

  ------------

OPEN C_BANK_NAME;
  LOOP
   fetch C_BANK_NAME INTO V_ATTRIB ;
   EXIT  when C_BANK_NAME %NOTFOUND;
   END LOOP;
   CLOSE C_BANK_NAME;

  -----------
OPEN C_KOL;
  LOOP
   fetch C_KOL INTO V_KOL ;
   EXIT  when C_KOL %NOTFOUND;
   END LOOP;
   CLOSE C_KOL;

SELECT SEQ INTO V_SEQ FROM ODB.GOAML_PERSON_SETUP;

--V_NAME:='D:\GOAML\XML\'||v_bank||'_'||v_report_code||'_'||V_KOL||'_F_'||V_SEQ||'_'||to_char(SYSDATE,'YYYYMMDD')||'.xml';


UPDATE ODB.GOAML_PERSON_SETUP SET SEQ=SEQ+1;
COMMIT;

  V_NAME:=v_bank||'_'||v_report_code||P_TIP_FI||V_KOL||'_'||V_SEQ||'_'||to_char(SYSDATE,'YYYYMMDD')||'_TEST.JSON';


OPEN c_MN;
  LOOP
   fetch c_MN INTO V_MN ;
   EXIT  when c_MN %NOTFOUND;
   END LOOP;
   CLOSE c_MN;

V_reportType:= V_MN.REPORTTYPE;

V_reportReason:=V_MN.REPORTREASON ;




 v_file := UTL_FILE.FOPEN ( 'GOAML', v_name, 'w', 32767 );
 ---UTL_FILE.PUTF ( v_File,  str1, SYSDATE );

  --- select to_char(sysdate, 'YYYY-MM-DD"T"HH24:MI:SS"S"') from dual;
 /*
 UTL_FILE.PUTF ( v_File,
                 '
                 {
  "report": {
    "header": {
      "sendDate": "'|| to_char(sysdate+0.011, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"') ||'",
      "reportType": '||V_reportType||',
      "reportReason":'|| V_reportReason ||'
    },
    "transactions": [', SYSDATE );
    */
    ----------
    UTL_FILE.PUTF ( v_File,
                 '

                 {
                 "reports": [
                 ', SYSDATE );
    -------

    V_START :=0;

OPEN c_goal;
LOOP
    FETCH c_goal INTO v_goal;
    EXIT WHEN c_goal %NOTFOUND;

    ------------
    OPEN C_fizik(v_goal.filialnumber, v_goal.registrac_nomer, v_goal.snn);
    LOOP
      FETCH C_fizik INTO v_fizik;
    EXIT WHEN C_fizik %NOTFOUND;
      end loop;
      close C_fizik;
      ----------
      IF v_goal.Tip in( 6, 7)  THEN
       OPEN C_YURIK__NAME(v_goal.Debet);  --- ESLI PO DEBETU YUR LICO BANK
    LOOP
      FETCH C_YURIK__NAME INTO V_YURIK__NAME;
    EXIT WHEN C_YURIK__NAME %NOTFOUND;
      end loop;
      close C_YURIK__NAME;
      END IF;

      ---------
      OPEN C_REGNUMBER(v_goal.filialnumber, v_goal.registrac_nomer);
    LOOP
      FETCH C_REGNUMBER INTO v_REGNUMBER;
    EXIT WHEN C_REGNUMBER %NOTFOUND;
      end loop;
      close C_REGNUMBER;

    ------------
     OPEN C_imza_huquq(v_goal.filialnumber, v_goal.registrac_nomer , v_goal.snn);
    LOOP
      FETCH C_imza_huquq INTO v_imza_huquq;
    EXIT WHEN C_imza_huquq %NOTFOUND;
      end loop;
      close C_imza_huquq;
    --------
    OPEN C_emitent_benefisiar(v_goal.filialnumber, v_goal.transactionnumber);
    LOOP
      FETCH C_emitent_benefisiar INTO v_emitent_benefisiar;
    EXIT WHEN C_emitent_benefisiar %NOTFOUND;
      end loop;
      close C_emitent_benefisiar;
    ----
     -----E_SIYASI_XADIM_DEYIL=1 or E_SIYASI_XADIMIN_AILE_UZVLERI
   if v_fizik.Political_Local =1 or v_imza_huquq.POLITICAL_LOCAL=1
      or v_emitent_benefisiar.E_SIYASI_XADIM_DEYIL=1  then
      v_typeOfConnectionToPep:=0;
   elsif v_emitent_benefisiar.E_SIYASI_XADIMIN_AILE_UZVLERI =1 then
      v_typeOfConnectionToPep:=2;
   elsif v_fizik.political_foreign =1 or v_imza_huquq.political_foreign=1 then
      v_typeOfConnectionToPep:=1;
      end if;
    ------------
    ----

OPEN c_S3C(v_goal.Account);
    FETCH c_S3C INTO V_S3C;
     CLOSE c_S3C;

OPEN c_log(v_goal.Transactionnumber/*, v_goal.Date_Oper*/);
    FETCH c_log INTO V_log;
     CLOSE c_log;

      --------
/* if
   instr ( nvl(v_goal.document_source,0), 'POS') >0 or
  instr ( nvl(v_goal.document_source,0), 'ATM') >0 or
  instr ( nvl(v_goal.document_source,0), '(CIN)') >0 OR
  instr ( nvl(v_goal.document_source,0), '(RET)') >0 OR
  instr ( nvl(v_goal.document_source,0), '(CTC)') >0
  THEN
    */
/*
 OPEN c_mcc(TRIM(SUBSTR(v_goal.Primechanie,3,11)));
 LOOP
    FETCH c_mcc INTO v_merchant_name,
                     v_merchant_code,
                     v_ekvayer_bank ,
                     v_mcc_code;
    EXIT WHEN c_mcc %NOTFOUND;
    END LOOP;
     CLOSE c_mcc;
     */
         -----
           -----
      /*     if instr ( nvl(v_goal.document_source,0), 'POS') >0 or
             instr ( nvl(v_goal.document_source,0), '(RET)') >0 OR
             instr ( nvl(v_goal.document_source,0), '(CTC)') >0 THEN

             V_sen_merchant_name:=v_merchant_name;
          V_sen_merchant_code:=v_merchant_code;
          V_sen_ekvayer_bank:=v_ekvayer_bank;
          V_sen_mcc_code :=v_mcc_code;


          end if;
          */
        /*  if  instr ( nvl(v_goal.document_source,0), 'ATM') >0 or
             instr ( nvl(v_goal.document_source,0), '(CIN)') >0
         THEN

          V_REC_merchant_name:=v_merchant_name;
          V_REC_merchant_code:=v_merchant_code;
          V_REC_ekvayer_bank:=v_ekvayer_bank;
          V_REC_mcc_code :=v_mcc_code;

          end if;
          */
         -----
         -----====
        /*  OPEN C_subsytem(v_goal.Account);
 LOOP
    FETCH C_subsytem INTO V_service_name;
    EXIT WHEN C_subsytem %NOTFOUND;
    END LOOP;
     CLOSE C_subsytem;

     IF instr (UPPER(V_service_name),'VISA') >0 THEN
       V_trn_payment_sub_system:='VIS';
     ELSIF   instr (UPPER(V_service_name),'MASTER')>0 OR instr (UPPER(V_service_name),'STANDARD')>0  THEN
       V_trn_payment_sub_system:='MCR';
       ELSE
         V_trn_payment_sub_system:='BCC';
     END IF;
     */
     ------====


   --  END IF;

 -------

 -------

 --------


     if instr ( nvl(v_goal.document_source,0), 'EXPRESS') >0 or
  instr ( nvl(v_goal.document_source,0), 'EMANAT') >0 or
  instr ( nvl(v_goal.document_source,0), 'MILLION') >0 or
  instr ( nvl(v_goal.document_source,0), 'SWPAY') >0 or
  instr ( nvl(v_goal.document_source,0), 'ATM') >0 or
  instr ( nvl(v_goal.document_source,0), '(CIN)') >0 or
  instr ( nvl(v_goal.document_source,0), '(OC)') >0  or
  instr ( nvl(v_goal.document_source,0), '(POS)') >0 or

  SUBSTR(v_goal.Debet,1,3) ='100' OR SUBSTR(v_goal.KREDIT,1,3) ='100'
    THEN


V_trn_type := 'CSH'; -- --Nağd əməliyyat  CSH  Qeyri-nağd əməliyyat NCH

ELSIF instr ( nvl(v_goal.document_source,0), 'IPS') >0 or
  instr ( nvl(v_goal.document_source,0), 'IPS') >0 or
  instr ( nvl(v_goal.document_source,0), 'XOHKS') >0 or
  instr ( nvl(v_goal.document_source,0), 'VSHAPE') >0 or
  instr ( nvl(v_goal.document_source,0), 'SWIFT') >0 or
  instr ( nvl(v_goal.document_source,0), 'B2B') >0 or
  V_GOAL.REPORT_CODE='ETR'
  THEN

  V_trn_type := 'NCH';

END IF;


if
  instr ( nvl(v_goal.document_source,0), 'OC') >0 or
  SUBSTR(v_goal.Debet,1,3) ='100' OR SUBSTR(v_goal.KREDIT,1,3) ='100' THEN


V_trn_payment_system:='CBB'; --

END IF;

if instr ( nvl(v_goal.document_source,0), 'EXPRESS') >0 or
  instr ( nvl(v_goal.document_source,0), 'EMANAT') >0 or
  instr ( nvl(v_goal.document_source,0), 'MILLION') >0 or
  instr ( nvl(v_goal.document_source,0), 'SWPAY') >0 THEN


V_trn_payment_system:='IAP'; --

END IF;

/*
if instr ( nvl(v_goal.document_source,0), 'XOHKS') >0 THEN

V_trn_payment_system:='XKS'; --

ELSif instr ( nvl(v_goal.document_source,0), 'VSHAPE') >0 THEN

V_trn_payment_system:='AZP'; --

ELSif instr ( nvl(v_goal.document_source,0), 'IPS') >0 THEN

V_trn_payment_system:='AOS'; --

ELSIF instr ( nvl(v_goal.document_source,0), 'SWIFT') >0 THEN

V_trn_payment_system:='SWT';

ELSIF instr ( nvl(v_goal.document_source,0), 'B2B') >0 THEN

  V_trn_payment_system := 'B2B';

end if;
*/


OPEN c_pla_sys (v_goal.Id_Vd , v_goal.Account , v_goal.Date_Oper ) ;
  LOOP
   fetch c_pla_sys INTO v_pla_sys ;
   EXIT  when c_pla_sys %NOTFOUND;
   END LOOP;
   CLOSE c_pla_sys;

if   nvl(v_goal.OPCODE_2,0) IN ('2317', '2316') THEN


V_trn_payment_system:='UTF'; --

END IF;


if
  instr ( nvl(v_goal.document_source,0), 'POS') >0 or
  instr ( nvl(v_goal.document_source,0), 'ATM') >0 or
  instr ( nvl(v_goal.document_source,0), '(CIN)') >0 OR
  instr ( nvl(v_goal.document_source,0), '(CTC)') >0 OR
  instr ( nvl(v_goal.document_source,0), '(RET)') >0 OR
  instr ( nvl(v_goal.document_source,0), '(PST)') >0 OR
  instr ( nvl(v_goal.document_source,0), '(ELC)') >0 OR
  instr ( nvl(v_goal.document_source,0), '(CTC)') >0 THEN


V_trn_payment_system:='PST'; --

END IF;




--V_trn_payment_sub_system:= NULL;
---V_trn_detailed_id:=v_goal.Registrac_Nomer; --?
V_trn_fund_source:='A'; -- nujno uznati u MMX

if instr ( nvl(v_goal.document_source,0), 'EXPRESS') >0 or
  instr ( nvl(v_goal.document_source,0), 'EMANAT') >0 or
  instr ( nvl(v_goal.document_source,0), 'MILLION') >0 or
  instr ( nvl(v_goal.document_source,0), 'SWPAY') >0
   THEN


v_trn_chanel:='PTM';

END IF;




if
  instr ( nvl(v_goal.document_source,0), 'EC') >0 --OR
  --instr (UPPER(V_service_name),'VISA') >0 or
--  instr (UPPER(V_service_name),'MASTER')>0 or
--  instr (UPPER(V_service_name),'STANDARD')>0

   THEN


v_trn_chanel:='ECM'; --

END IF;

if
  instr ( nvl(v_goal.document_source,0), 'ATM') >0 OR instr ( nvl(v_goal.document_source,0), '(CIN)') >0 THEN


v_trn_chanel:='ATM'; --

END IF;

if
  instr ( nvl(v_goal.document_source,0), 'POS') >0  THEN


v_trn_chanel:='POS'; --

END IF;

if V_MN.REPORTTYPE= 'CTR'  THEN
  v_trn_chanel:='FOT';
  END IF;

--V_repr_status:='PAT'; --ETABARNEME
V_repr_citizenship:=(v_goal.Nationality1_Con);
---sender_tax_id ??


---------------
if v_goal.TIP not in( 4,14) then
OPEN C_REGISTRATION_NUMBER (v_goal.incorporation_number_to) ;
  LOOP
   fetch C_REGISTRATION_NUMBER INTO V_REGISTRATION_NUMBER_SEN ;
   EXIT  when C_REGISTRATION_NUMBER %NOTFOUND;
   END LOOP;
   CLOSE C_REGISTRATION_NUMBER;

   end if;

   if v_goal.TIP = 4 then
OPEN C_REGISTRATION_NUMBER (v_goal.Incorporation_Number) ;
  LOOP
   fetch C_REGISTRATION_NUMBER INTO V_REGISTRATION_NUMBER_SEN ;
   EXIT  when C_REGISTRATION_NUMBER %NOTFOUND;
   END LOOP;
   CLOSE C_REGISTRATION_NUMBER;

   end if;

 OPEN C_REGISTRATION_NUMBER (v_goal.Incorporation_Number) ;
  LOOP
   fetch C_REGISTRATION_NUMBER INTO V_REGISTRATION_NUMBER_REC ;
   EXIT  when C_REGISTRATION_NUMBER %NOTFOUND;
   END LOOP;
   CLOSE C_REGISTRATION_NUMBER;


------------
       /*
  OPEN c_risk (v_REGNUMBER.Riskler) ;
  LOOP
   fetch c_risk INTO v_risk ;
   EXIT  when c_risk %NOTFOUND;
   END LOOP;
   CLOSE c_risk;

*/

if v_REGNUMBER.Riskler =3 then
   v_customerRiskDegree :=0;
elsif v_REGNUMBER.Riskler =2 then
   v_customerRiskDegree :=1;
elsif v_REGNUMBER.Riskler =1 then
   v_customerRiskDegree :=2;
end if;

-----
OPEN c_PV_HUQUQI (v_goal.Incorporation_Number,  v_goal.registrac_nomer_k) ;
  LOOP
   fetch c_PV_HUQUQI INTO v_PV_HUQUQI ;
   EXIT  when c_PV_HUQUQI %NOTFOUND;
   END LOOP;
   CLOSE c_PV_HUQUQI;

--------

UTL_FILE.PUTF ( v_File,
                 '

  {
    "header": {
      "sendDate": "'|| to_char(sysdate+0.011, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"') ||'",
      "reportType": '||V_reportType||',
      "reportReason":'|| V_reportReason ||'
      ', SYSDATE );

  if v_report_code <>'CTR' AND v_report_code ='QHT'THEN ---QHT NE CTR

       UTL_FILE.PUTF ( v_File,',
                "tin": "'||v_REGNUMBER.Inn_Regnom||'",
                "tinDate": "'|| to_char(TO_DATE(v_PV_HUQUQI.Qeydiyyat_Tarixi, 'DD-MM-YYYY')+0.1 , 'YYYY-MM-DD"T"HH24:MI:SS".001Z"') ||'",
                "organizationType": 1,
                "note": "'||v_PV_HUQUQI.Aml_Comment ||'"', SYSDATE );

      END IF; ----QHT NE CTR

      if v_report_code <>'CTR' AND v_report_code IN ('PEP')THEN ---PEP NE CTR


    UTL_FILE.PUTF ( v_File,',
      "tin": "'||v_REGNUMBER.Inn_Regnom||'",
        "countryOfRegistration": "'||nvl(v_fizik.vetendashligi, v_goal.Nationality1)||'",
        "citizenship": "'|| nvl(v_fizik.vetendashligi, v_goal.Nationality1)||'",
        "identityType":'|| R_TYPES_OF_DOCUME(v_goal.sender_Id_Type,v_goal.sender_id_serial)||',
        "serial": "'|| v_goal.sender_id_serial||'",
        "documentNumber": "'|| v_goal.SENDER_ID_NUMBER  ||'",
        "fin": "'||v_goal.Snn||'",
        "customerRiskDegree": '|| v_customerRiskDegree ||',
        "pepCountry": "'||nvl( v_fizik.vetendashligi,v_goal.Nationality1) ||'",
        "country": "'||  nvl(v_fizik.yashadigi_olke,v_goal.Country_Code_Sig)||'",
        "residency": "'||  nvl(v_fizik.vetendashligi, v_goal.Nationality1)||'",
        "city": "'||  nvl(v_fizik.sheher, v_imza_huquq.sheher)||'",
        "district": "'||  nvl(v_fizik.pocht_indeksi, v_imza_huquq.pocht_indeksi)||'",
        "street": "'|| replace(  nvl(v_fizik.Kuchenin_Adi_Ve_Nomresi, v_imza_huquq.kuchenin_adi_ve_nomresi),'"')||'",
        "home": "'|| replace(  nvl(v_fizik.Kuchenin_Adi_Ve_Nomresi, v_imza_huquq.kuchenin_adi_ve_nomresi),'"')||'",
        "flat": "'|| ''||'",
        "averageMonthlyIncome": '|| to_char(NVL( nvl(v_fizik.AYLIQ_GELIR, v_imza_huquq.ayliq_gelir),0), 'FM99999999999990.00')||',
        "position": "'||nvl(v_fizik.Vezifesi, v_imza_huquq.vezifesi) ||'",
        "positionStartDate": "'|| to_char(nvl(v_fizik.pf_vezifeyeteyinolunmatarixi, v_imza_huquq.imza_bashlama), 'YYYY-MM-DD"T"HH24:MI:SS".001Z"') ||'",
        "positionEndDate": "'|| ''/*to_char(nvl(v_fizik.pf_vezifeyeteyinolunmatarixi, v_imza_huquq.imza_bitme) , 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')*/ ||'",
        "businessStartDate": "'|| to_char(nvl(v_fizik.pf_vezifeyeteyinolunmatarixi, v_imza_huquq.imza_bashlama), 'YYYY-MM-DD"T"HH24:MI:SS".001Z"') ||'",
        "businessEndDate": "'|| ''/*to_char(v_fizik.pf_vezifeyeteyinolunmatarixi, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')*/ ||'",
        "note": "'||nvl(v_fizik.aml_comment,v_PV_HUQUQI.Aml_Comment) ||'",
        "workplace": "'||replace(trim(replace( nvl(v_fizik.Ish_Yerinin_Adi, v_imza_huquq.gelir_menbeyi),'"')),'/') ||'",
        "typeOfConnectionToPep":'|| v_typeOfConnectionToPep ||',
        "fullName": "'||v_goal.First_Name_Sig || ' '|| v_goal.Middle_Name_Sig || ' '|| v_goal.Last_Name_Sig ||'",
        "influentialPerson": "'||nvl(v_fizik.Vezifesi, v_imza_huquq.vezifesi) ||'",
        "sofIncome": '||0/*to_char(NVL( v_fizik.AYLIQ_GELIR,0), 'FM99999999999990.00')*/||',
        "institution": "'||replace(trim(replace( nvl(v_fizik.Ish_Yerinin_Adi, v_imza_huquq.gelir_menbeyi),'"')),'/') ||'",
        "politicallyInfluentialPersonPositions":[11]
        ', SYSDATE );
        ------

        end if; ---pep NE CTR
        ----

         if v_REGNUMBER.Fizik =1 OR SUBSTR(v_goal.DEBET,1,5) ||SUBSTR(v_goal.KREDIT,1,5) IN ('1005010060','1006010050')
           then
          v_individualPerson:='true';
          v_typeOfConnectionToHrz:='1';

          elsif  v_REGNUMBER.Yurik =1 or  v_REGNUMBER.Predprinimatel =1 then
            v_individualPerson:='false';
            v_typeOfConnectionToHrz:='11';
            end if;

        --
     if  SUBSTR(v_goal.DEBET,1,5) ||SUBSTR(v_goal.KREDIT,1,5) IN ('1005010060','1006010050')
           then
    OPEN c_cif(v_emitent_benefisiar.e_pincode);
    LOOP
      FETCH c_cif INTO v_cif;
      EXIT WHEN c_cif %NOTFOUND;
    end loop;
    close c_cif;


  OPEN C_REGNUMBER (v_goal.filialnumber, v_cif.regnom) ;

  LOOP
   fetch C_REGNUMBER INTO v_REGNUMBER_cif ;
   EXIT  when C_REGNUMBER %NOTFOUND;
   END LOOP;
   CLOSE C_REGNUMBER;



if v_REGNUMBER_cif.Riskler =3 then
   v_customerRiskDegree_cif :=0;
elsif v_REGNUMBER_cif.Riskler =2 then
   v_customerRiskDegree_cif :=1;
elsif v_REGNUMBER_cif.Riskler =1 then
   v_customerRiskDegree_cif :=2;
end if;

end if;
        ---blr hrz
             if v_report_code <>'CTR' AND v_report_code IN ('BLR')THEN ---blr NE CTR

    UTL_FILE.PUTF ( v_File,',
      "individualPerson": '|| v_individualPerson ||',
          "tin": "'|| v_REGNUMBER.Inn_Regnom||'",
          "countryOfRegistration": "'||nvl(v_goal.Nationality1 , nvl(v_emitent_benefisiar.e_vetendashligi, v_fizik.vetendashligi))||'",
          "citizenship": "'| nvl(v_goal.Nationality1 , nvl(v_emitent_benefisiar.e_Vetendashligi, v_fizik.vetendashligi))||'",
          "identityType": '|| R_TYPES_OF_DOCUME(v_goal.sender_Id_Type,v_goal.sender_id_serial)||',
          "serial": "'|| v_goal.sender_id_serial||'",
          "documentNumber": "'|| v_goal.SENDER_ID_NUMBER  ||'",
          "fin": "'||v_goal.Snn||'",
          "sphereOfActivity": "'|| nvl(v_fizik.taxactivitycode, v_PV_HUQUQI.Business)||'",
          "customerRiskDegree": '|| nvl(v_customerRiskDegree_cif,v_customerRiskDegree)  ||',
          "country":  "'||nvl( v_fizik.yashadigi_olke,v_goal.Country_Code_Sig)/*----v_cif.yashadigi_olke*/||'",
          "residency": "'|| nvl(v_fizik.vetendashligi, v_goal.Nationality1)/*---v_cif.vetendashligi*/||'",
          "city": "'|| nvl(nvl(v_cif.sheher,v_fizik.sheher), v_imza_huquq.sheher)||'",
          "district": "'|| nvl(nvl(v_cif.pocht_indeksi,v_fizik.pocht_indeksi), v_imza_huquq.pocht_indeksi)||'",
          "street": "'|| replace( nvl(nvl(v_cif.kuchenin_adi_ve_nomresi,v_fizik.Kuchenin_Adi_Ve_Nomresi), v_imza_huquq.kuchenin_adi_ve_nomresi),'"')||'",
          "home": "'|| replace( nvl(nvl(v_cif.kuchenin_adi_ve_nomresi,v_fizik.Kuchenin_Adi_Ve_Nomresi), v_imza_huquq.kuchenin_adi_ve_nomresi),'"')||'",
          "flat": "'|| ''||'",
          "sofIncome": '|| 0/*to_char(NVL( v_fizik.AYLIQ_GELIR,0), 'FM99999999999990.00')*/||',
          "averageMonthlyIncome": '|| to_char(NVL(NVL( nvl(v_cif.ayliq_gelir, v_fizik.AYLIQ_GELIR), v_imza_huquq.ayliq_gelir),0), 'FM99999999999990.00')||',
          "position": "'||NVL(nvl(v_cif.vezifesi,v_fizik.Vezifesi), v_imza_huquq.vezifesi) ||'",
          "workplace": "'||replace(trim(replace(nvl( nvl(v_cif.ish_yerinin_adi,v_fizik.Ish_Yerinin_Adi), v_imza_huquq.gelir_menbeyi),'"')),'/') ||'",
          "businessStartDate": "'|| to_char(nvl(nvl(nvl(v_PV_HUQUQI.Customercreationdate, v_cif.customercreationdate), v_fizik.customercreationdate), v_imza_huquq.imza_bashlama), 'YYYY-MM-DD"T"HH24:MI:SS".001Z"') ||'",
          "businessEndDate": "'|| ''/*to_char(v_fizik.customercreationdate, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')*/ ||'",
          "note": "'||nvl(nvl(v_cif.aml_comment,v_fizik.aml_comment),v_PV_HUQUQI.Aml_Comment) ||'",
          "typeOfConnectionToHrz": '|| v_typeOfConnectionToHrz ||',
          "hrzCountry": "'|| nvl(nvl (nvl(v_goal.Nationality1,v_cif.vetendashligi),v_fizik.vetendashligi),v_goal.Nationality1)||'",
          "fullName": "'||v_goal.First_Name_Sig || ' '|| v_goal.Middle_Name_Sig || ' '|| v_goal.Last_Name_Sig ||'",
          "idNumber": "'||NVL(nvl(v_PV_HUQUQI.Regnom,v_cif.regnom),v_fizik.Regnom) ||'"
        ', SYSDATE );
        ------
        ---v_PV_HUQUQI.Incorporation_Country_Code --Ilkana skazala
        end if; -- blr-NE CTR
        ----ednd blr hrz
    ------
    UTL_FILE.PUTF ( v_File,
                 '},', SYSDATE );
    -----------

     ---------
           UTL_FILE.NEW_LINE(v_File);
           -----

    UTL_FILE.PUTF ( v_File,
                 '"transactions": [', SYSDATE );
-----------v_goal.nomer_docum

 UTL_FILE.PUTF ( v_File,
                 '

      {
        "Transaction": {
          "trn_status": "EXC",
          "trn_actual_date": "'||NVL( nvl(V_log.Datetimedocexec, V_log.Datetimedocins), to_char(v_goal.date_oper+0.301111, 'YYYY-MM-DD"T"HH24:MI:SS".002Z"')) ||'",
          "trn_bank_date": "'||  NVL( nvl(V_log.Datetimedocexec, V_log.Datetimedocins), to_char(v_goal.date_oper+0.301111, 'YYYY-MM-DD"T"HH24:MI:SS".002Z"')) ||'",
          "trn_external_number": "'|| GREATEST( NVL(v_pla_sys.message_id,NVL(v_goal.Id_Vd,v_goal.Transactionnumber)), RPAD( NVL(v_pla_sys.message_id,NVL(v_goal.Id_Vd,v_goal.Transactionnumber)), 5, '0'))  ||'",
          "trn_internal_number": "'|| v_goal.Transactionnumber  ||'",
          "trn_type": "'||NVL( V_trn_type, 'NCH'/*'CSH'*/)||'",
          "trn_payment_system": "'|| NVL(NVL(V_pla_sys.Plat_System ,V_trn_payment_system),'CBB') ||'",
          "trn_payment_sub_system": "'|| V_trn_payment_sub_system ||'",
          "Purpose": {
            "trn_description": "'||replace(replace(v_goal.Primechanie, CHR(10)), CHR(10)) ||'",
            "trn_detailed_id": "'||v_goal.Registrac_Nomer ||'",
            "trn_fund_source": "'|| '' ||'"
          },
          "trn_chanel": "'|| NVL(v_trn_chanel,'FOT') ||'"
        },', SYSDATE );

        ----
       if  v_goal.First_Name_Con is not null AND v_goal.Tip not in (5,6,7,11) then --FiZiK OUT  1;FiZiK IN 2;Hцuq OUT - 3;Hцquq in-4;H TO H -5;H TO H in -6;
                                                                                   ---H to F in -7;F to F -8;H to F out-11;ACC TO ACC - 12;F to H in -14

        ---

        V_repr_status:='PAT'; --ETABARNEME


        ---

        UTL_FILE.PUTF ( v_File,'
        "Representative": {
          "repr_status": "'||V_repr_status||'",
          "repr_citizenship": "'|| v_goal.Nationality1_Con||'",
          "repr_id_serial": "'||v_goal.repr_id_serial ||'",
          "repr_id_number": "'||v_goal.repr_id_number ||'",
          "repr_id_issue_date": "'|| to_char(TO_DATE(v_goal.Id_Issue_Date_Con, 'DD-MM-YYYY')+0.201111, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')||'",
          "repr_id_expiry_date": "'|| to_char(TO_DATE(v_goal.Id_Expiry_Date, 'DD-MM-YYYY')+0.201111, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')||'",
          "repr_birth_date": "'||  to_char(TO_DATE(v_goal.Birthdate_Con, 'DD-MM-YYYY')+0.201111, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')||'",
          "repr_phone": "'||REPLACE(v_goal.Tph_Number_Con,'-') ||'",
          "repr_pin": "'||v_goal.Snn_Con ||'",
          "repr_first_name": "'||trim(v_goal.First_Name_Con) ||'",
          "repr_last_name": "'||trim(v_goal.Last_Name_Con) ||'",
          "repr_middle_name": "'||REPLACE(trim(v_goal.Middle_Name_Con),'.') ||'",
          "repr_id_type": "'||v_goal.repr_id_type ||'"
        },', SYSDATE );

        end if;

        if  v_goal.First_Name_Con is null OR v_goal.tip  in (5,6,7,11)   then --FiZiK OUT  1;FiZiK IN 2;Hцuq OUT - 3;Hцquq in-4;H TO H -5;H TO H in -6;
                                                                                   ---H to F in -7;F to F -8;H to F out-11;ACC TO ACC - 12;F to H in -14

        UTL_FILE.PUTF ( v_File,'
        "Representative": {
           "repr_status": "NRP"
         },', SYSDATE );

         elsif v_goal.First_Name_Con is null and v_goal.tip in (1,2,3,4)   then

         V_repr_status:='SIG'; --ETABARNEME

         UTL_FILE.PUTF ( v_File,'
        "Representative": {
          "repr_status": "'||V_repr_status||'",
          "repr_citizenship": "'| v_goal.tip || v_goal.nationality1||'",
          "repr_id_serial": "'||v_goal.sender_id_serial ||'",
          "repr_id_number": "'||v_goal.sender_id_number ||'",
          "repr_id_issue_date": "'|| to_char(TO_DATE(v_goal.Id_Issue_Date_sig, 'DD-MM-YYYY')+0.201111, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')||'",
          "repr_id_expiry_date": "'|| to_char(TO_DATE(v_goal.Id_Expiry_Date_sig, 'DD-MM-YYYY')+0.201111, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')||'",
          "repr_birth_date": "'||  to_char(TO_DATE(v_goal.Birthdate_sig, 'DD-MM-YYYY')+0.201111, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')||'",
          "repr_phone": "'||REPLACE(v_goal.Tph_Number_sig,'-') ||'",
          "repr_pin": "'||v_goal.Snn ||'",
          "repr_first_name": "'||trim(v_goal.First_Name_sig) ||'",
          "repr_last_name": "'||trim(v_goal.Last_Name_sig) ||'",
          "repr_middle_name": "'||REPLACE(trim(v_goal.Middle_Name_sig),'.') ||'",
          "repr_id_type": "'||v_goal.sender_id_type ||'"
        },', SYSDATE );

        end if;


        ----

        UTL_FILE.PUTF ( v_File,'
        "Sender": {
          "sender_country_name": "'||(v_goal.From_Country) ||'",', SYSDATE );

        --------Юридическое лицо

         IF v_goal.Name_Ur IS NOT NULL and v_goal.name_ur_to IS NULL  and  v_goal.tip not in (1,2,8,12,14) THEN
        UTL_FILE.PUTF ( v_File,'
          "Entity": {
            "sender_entity_name": "'||replace(trim(replace(v_goal.Name_Ur,'"')),'/') ||'",
            "sender_entity_reg_country": "'||r_country(v_goal.Incorporation_Country_Code) ||'",
            "sender_entity_tax_number": "'|| v_goal.Incorporation_Number ||'"
          },', SYSDATE );

          END IF;

        IF v_goal.name_ur_to IS NOT NULL   and  v_goal.tip not in (1,2,8,12,14) THEN
        UTL_FILE.PUTF ( v_File,'
          "Entity": {
            "sender_entity_name": "'||replace(trim(replace(NVL(V_YURIK__NAME.NAME_LICSCH, v_goal.name_ur_to),'"')),'/') ||'",
            "sender_entity_reg_country": "'||r_country(NVL(V_YURIK__NAME.COUNTRYCODE, v_goal.incorporation_country_code_to)) ||'",
            "sender_entity_tax_number": "'|| NVL(V_YURIK__NAME.INN_LICSCH,v_goal.incorporation_number_to) ||'"

          },', SYSDATE );

          END IF;
          ---------
          /*
FiZiK OUT -1; FiZiK IN -2; Hцuq OUT -3;Hцquq in -4;
H TO H-5; H TO H in -6; H to F in -7; F to F -8;
H to F out - 11; ACC TO ACC -12; F to H in -14;
C2C OUT - 17; F to H out - 18; C2C IN - 19;
*/
          ---------
           UTL_FILE.NEW_LINE(v_File);
           -----
           if v_goal.tip not in (3,4,5,6,7,11) then

        /*   if  --instr ( nvl(v_goal.document_source,0), '(PC)') =0 and
         instr ( nvl(v_goal.document_source,0), 'POS') =0 and
  instr ( nvl(v_goal.document_source,0), 'ATM') =0 and
  instr ( nvl(v_goal.document_source,0), '(CIN)') >0 and
  instr ( nvl(v_goal.document_source,0), '(RET)') =0
  THEN
      */
          UTL_FILE.PUTF ( v_File,
          replace(
          '
          "Person": {
            "sender_first_name": "'||trim(v_goal.first_name_to)||'",
            "sender_last_name": "'| trim(v_goal.last_name_to)||'",
            "sender_middle_name": "'| REPLACE(REPLACE(trim(v_goal.middle_name_to),'-'),'.')||'",
            "sender_citizenship": "'||(v_goal.nationality1_to) ||'",
            "sender_id_serial": "'| v_goal.rec_id_serial||'",
            "sender_id_number": "'| v_goal.rec_id_number||'",
            "sender_pin_code": "'| v_goal.snn_to||'",
            "sender_id_issue_date": "'| to_char(TO_DATE(v_goal.id_issue_date_to, 'DD-MM-YYYY')+0.201111, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')||'",
            "sender_id_expiry_date": "'| to_char(TO_DATE(v_goal.id_expiry_date_to, 'DD-MM-YYYY')+0.201111, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')||'",
            "sender_birth_date": "'| to_char(v_goal.birthdate_to+0.201111, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')||'",
            "sender_phone_number": "'| REPLACE(v_goal.tph_number_to,'-')||'",
            "sender_tax_id": "'| replace(v_goal.incorporation_number_to,V_ATTRIB.Inn_Bank) ||'",
            "sender_id_type": "'| v_goal.Rec_Id_Type||'"
          },','"sender_tax_id": "",'), SYSDATE );

         -- end if;

          end if;

          -------
          if SUBSTR(v_goal.DEBET,1,5)||SUBSTR(v_goal.KREDIT,1,5) IN ('1005010060')  then

          UTL_FILE.PUTF ( v_File,'
          "Funds": {
            "sender_curr_type": "'| 'AZN'||'",
            "sender_curr_amount": '| TO_CHAR(  v_goal.Summa_v_Nacval ,'FM999999999990.90')||',
            "sender_amount_local": '| TO_CHAR( v_goal.Summa_v_Nacval ,'FM999999999990.90')||'
          } ', SYSDATE );

          ELSIF SUBSTR(v_goal.DEBET,1,5)||SUBSTR(v_goal.KREDIT,1,5) IN ('1006010050') then

          UTL_FILE.PUTF ( v_File,'
          "Funds": {
            "sender_curr_type": "'||  v_goal.foreign_currency_code_from||'",
            "sender_curr_amount": '| TO_CHAR(  v_goal.Summa_v_Inval ,'FM999999999990.90')||',
            "sender_amount_local": '| TO_CHAR( v_goal.Summa_v_Nacval ,'FM999999999990.90')||'
          } ', SYSDATE );

          end if;

          if SUBSTR(v_goal.DEBET,1,5)||SUBSTR(v_goal.KREDIT,1,5) NOT IN ('1005010060', '1006010050') and
            func_get_Ekv(SUBSTR(v_goal.Debet,6,2))<> 'AZN'
            then

          UTL_FILE.PUTF ( v_File,'
          "Funds": {
            "sender_curr_type": "'| func_get_Ekv(SUBSTR(v_goal.Debet,6,2)) /* v_goal.Foreign_Currency_Code*/||'",
            "sender_curr_amount": '| TO_CHAR( R_NOL( v_goal.Summa_v_Inval, v_goal.Summa_v_Nacval) ,'FM999999999990.90')||',
            "sender_amount_local": '| TO_CHAR( v_goal.Summa_v_Nacval ,'FM999999999990.90')||'
          } ', SYSDATE );

          end if;

          if SUBSTR(v_goal.DEBET,1,5)||SUBSTR(v_goal.KREDIT,1,5) NOT IN ('1005010060', '1006010050') and
            func_get_Ekv(SUBSTR(v_goal.Debet,6,2))= 'AZN'
            then

          UTL_FILE.PUTF ( v_File,'
          "Funds": {
            "sender_curr_type": "'| func_get_Ekv(SUBSTR(v_goal.Debet,6,2)) /* v_goal.Foreign_Currency_Code*/||'",
            "sender_curr_amount": '| TO_CHAR( v_goal.Summa_v_Nacval ,'FM999999999990.90')||',
            "sender_amount_local": '| TO_CHAR( v_goal.Summa_v_Nacval ,'FM999999999990.90')||'
          } ', SYSDATE );

          end if;
          ---------
           UTL_FILE.NEW_LINE(v_File);
           ----
           if SUBSTR(v_goal.DEBET,1,5)||SUBSTR(v_goal.KREDIT,1,5) not IN ('1006010050','1005010060' ) AND
            v_goal.tip NOT IN (2,4,8 )
            then --ne exch HUGUG IN
           ----
           OPEN C_INN_BANK (NVL(v_goal.swift_to,v_goal.Swift)) ;
               LOOP
               fetch C_INN_BANK INTO V_INN_BANK ;
               EXIT  when C_INN_BANK %NOTFOUND;
               END LOOP;
               CLOSE C_INN_BANK;
           ------
     if  instr ( nvl(v_goal.document_source,0), '(PC)') =0 and
         instr ( nvl(v_goal.document_source,0), 'POS') =0 and
  instr ( nvl(v_goal.document_source,0), 'ATM') =0 and
  instr ( nvl(v_goal.document_source,0), '(CIN)') =0 and
  instr ( nvl(v_goal.document_source,0), '(RET)') =0
  THEN
           -----
          UTL_FILE.PUTF ( v_File,
          replace(replace(',
          "Account": {
            "sender_account_number": "' ||NVL(NVL(REPLACE(v_goal.account_to,'-'),V_YURIK__NAME.LICSCH),v_goal.account) ||'",
            "sender_account_dubl_number": "'| NVL(V_YURIK__NAME.REGISTRAC_NOMER,V_REGISTRATION_NUMBER_SEN) ||'",
            "sender_account_currency": "'||  R_GET_KOD_EKV(NVL(SUBSTR(V_YURIK__NAME.LICSCH,6,2),SUBSTR(v_goal.Account,6,2))) ||'",
            "sender_account_pan_id_number": "'||''/*SUBSTR(V_S3C.CONTRACT_NUMBER,1,5) || '****' || SUBSTR(V_S3C.CONTRACT_NUMBER,-4,4)*/||'",
            "sender_account_3d_secure": "'| ''/*REPLACE(V_S3C.MOBILE_PHONE,'-')*/ ||'",
            "sender_account_fin_inst_country": "'||NVL(v_goal.FROM_COUNTRY , V_YURIK__NAME.COUNTRYCODE) ||'",
            "sender_account_fin_inst_bic_id": "'||NVL(NVL(v_goal.swift_to, V_YURIK__NAME.BIC_SWIFT),v_goal.Swift) ||'",
            "sender_account_fin_inst_name": "'||REPLACE(trim(NVL(NVL(v_goal.Institution_Name_TO,V_YURIK__NAME.BANK),v_goal.institution_name)),'"')/*trim(NVL(v_goal.Institution_Name_TO, v_goal.Institution_Name))*/ ||'",
            "sender_account_fin_inst_branch": "'||REPLACE( trim(NVL(NVL((v_goal.Institution_Name_TO),V_YURIK__NAME.BANK_BRANCH),v_goal.institution_name)),'"')/*trim(NVL(v_goal.Institution_Name_TO, v_goal.Institution_Name))*/ ||'",
            "sender_account_fin_inst_bic": "'||NVL(NVL(v_goal.swift_to,V_YURIK__NAME.BIC_SWIFT),v_goal.Swift) ||'",
            "sender_account_fin_inst_tax_id": "'| NVL(V_YURIK__NAME.INN,V_INN_BANK.INN) /*replace(V_REC_account_fin_inst_tax_id,V_INN_BANK.INN)*/ ||'"
          } ', '"sender_account_pan_id_number": "****",'),'"sender_account_3d_secure": "",'), SYSDATE );

          V_INN_BANK.INN:=NULL;

          END IF;
          -------
          end if;--ne exch
          -------
           UTL_FILE.NEW_LINE(v_File);
          --------
          if V_sen_merchant_name is not null then
          UTL_FILE.PUTF ( v_File,',
          "Payment_System": {
            "sender_merchant_name": "'||trim(V_sen_merchant_name) ||'",
            "sender_merchant_code": "'||V_sen_merchant_code ||'",
            "sender_ekvayer_bank": "'||trim(V_sen_ekvayer_bank) ||'",
            "sender_mcc_code": "'||V_sen_mcc_code ||'"
          }
        }', SYSDATE );

        end if;

        if V_sen_merchant_name is  null then
          UTL_FILE.PUTF ( v_File,'}', SYSDATE );

        end if;
        ---

        ----

        UTL_FILE.PUTF ( v_File,',
        "Receiver": {
          "receiver_country_name": "'| ( v_goal.to_country_to)||'",', SYSDATE );

          ------ yur licho

          if  v_goal.name_ur is not null and v_goal.tip <>3 /*AND v_goal.To_Yurik=1*/ then

          UTL_FILE.PUTF ( v_File,'
          "Entity": {
            "receiver_entity_name": "'||replace(trim(replace( v_goal.name_ur,'"')),'/')||'",
            "receiver_entity_reg_country": "'| ( v_goal.Incorporation_Country_Code)||'",
            "receiver_entity_tax_number": "'| v_goal.Incorporation_Number||'"
          }, ', SYSDATE );

          end if;

          if   v_goal.tip =3 AND v_goal.To_Yurik=1 then

          UTL_FILE.PUTF ( v_File,'
          "Entity": {
            "receiver_entity_name": "'||replace(replace( v_goal.Name_Ur,'"'),'/')||'",
            "receiver_entity_reg_country": "'| ( v_goal.Incorporation_Country_Code)||'",
            "receiver_entity_tax_number": "'| v_goal.Incorporation_Number||'"
          }, ', SYSDATE );

          end if;

          ----
          -------
           UTL_FILE.NEW_LINE(v_File);
          --------
          if (v_goal.tip not in (3,4,5,6,11,14) AND  NVL(v_goal.To_Yurik,0) <> 1 ) or
              (v_goal.tip  in (7) AND  NVL(v_goal.To_Yurik,0) = 1)
            then

          UTL_FILE.PUTF ( v_File,
          replace('
          "Person": {
            "receiver_first_name": "'| trim(v_goal.first_name_sig)||'",
            "receiver_last_name": "'| trim(v_goal.last_name_sig)||'",
            "receiver_middle_name": "'| REPLACE(REPLACE(trim(v_goal.middle_name_sig),'-'),'.')||'",
            "receiver_citizenship": "'| (v_goal.nationality1)||'",
            "receiver_id_serial": "'| v_goal.sender_id_serial||'",
            "receiver_id_number": "'| v_goal.sender_id_number||'",
            "receiver_pin_code": "'| v_goal.snn||'",
            "receiver_id_issue_date": "'| to_char(TO_DATE(v_goal.id_issue_date_sig, 'DD-MM-YYYY')+0.201111, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')||'",
            "receiver_id_expiry_date": "'| to_char(TO_DATE(v_goal.id_expiry_date_sig, 'DD-MM-YYYY')+0.201111, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')||'",
            "receiver_phone_number": "'||REPLACE(v_goal.tph_number_sig,'-') ||'",
            "receiver_tax_id": "'||replace((v_goal.incorporation_number),V_ATTRIB.Inn_Bank) ||'",
            "receiver_birth_date": "'| to_char(TO_DATE(v_goal.birthdate_sig, 'DD-MM-YYYY')+0.201111, 'YYYY-MM-DD"T"HH24:MI:SS".001Z"')||'",
            "receiver_id_type": "'| v_goal.sender_Id_Type||'"
          },','"receiver_tax_id": "",'), SYSDATE );

           end if;
           ---

          -------
          if SUBSTR(v_goal.DEBET,1,5)||SUBSTR(v_goal.KREDIT,1,5) IN ('1006010050') then

          UTL_FILE.PUTF ( v_File,'
          "Funds": {
            "receiver_curr_type": "'| 'AZN'||'",
            "receiver_curr_amount": '| TO_CHAR(  v_goal.Summa_v_Nacval ,'FM999999999990.90')||',
            "receiver_amount_local": '| TO_CHAR( v_goal.Summa_v_Nacval ,'FM999999999990.90')||'
          } ', SYSDATE );

          ELSIF SUBSTR(v_goal.DEBET,1,5)||SUBSTR(v_goal.KREDIT,1,5) IN ('1005010060') then

          UTL_FILE.PUTF ( v_File,'
          "Funds": {
            "receiver_curr_type": "'| v_goal.Foreign_Currency_Code||'",
            "receiver_curr_amount": '| TO_CHAR(  v_goal.Summa_v_Inval ,'FM999999999990.90')||',
            "receiver_amount_local": '| TO_CHAR( v_goal.Summa_v_Nacval ,'FM999999999990.90')||'
          } ', SYSDATE );

          end if;

          if SUBSTR(v_goal.DEBET,1,5)||SUBSTR(v_goal.KREDIT,1,5) NOT IN ('1005010060', '1006010050') AND
            v_goal.Foreign_Currency_Code <>'AZN' then

          UTL_FILE.PUTF ( v_File,'
          "Funds": {
            "receiver_curr_type": "'| v_goal.Foreign_Currency_Code||'",
            "receiver_curr_amount": '| TO_CHAR( R_NOL( v_goal.Summa_v_Inval, v_goal.Summa_v_Nacval) ,'FM999999999990.90')||',
            "receiver_amount_local": '| TO_CHAR( v_goal.Summa_v_Nacval ,'FM999999999990.90')||'
          } ', SYSDATE );

          end if;

          if SUBSTR(v_goal.DEBET,1,5)||SUBSTR(v_goal.KREDIT,1,5) NOT IN ('1005010060', '1006010050') AND
            v_goal.Foreign_Currency_Code ='AZN' then

          UTL_FILE.PUTF ( v_File,'
          "Funds": {
            "receiver_curr_type": "'| v_goal.Foreign_Currency_Code||'",
            "receiver_curr_amount": '| TO_CHAR( v_goal.Summa_v_Nacval ,'FM999999999990.90')||',
            "receiver_amount_local": '| TO_CHAR( v_goal.Summa_v_Nacval ,'FM999999999990.90')||'
          } ', SYSDATE );

          end if;


          ---------
          if v_report_code <> 'CTR' OR v_goal.tip  IN (2,4,8 ) THEN ---NE CTR--MEDAHIL HYGUN
          -----------
           UTL_FILE.NEW_LINE(v_File);
          --------
           OPEN C_INN_BANK (NVL(v_goal.swift,v_goal.Swift_to)) ;
               LOOP
               fetch C_INN_BANK INTO V_INN_BANK ;
               EXIT  when C_INN_BANK %NOTFOUND;
               END LOOP;
               CLOSE C_INN_BANK;


          ------

          UTL_FILE.PUTF ( v_File,
          replace(replace(',
          "Account": {
            "receiver_account_number": "'| NVL(replace(v_goal.account,'-'), v_goal.Account_to)||'",
            "receiver_account_dubl_number": "'| V_REGISTRATION_NUMBER_REC ||'",
            "receiver_account_currency": "'| R_GET_KOD_EKV(SUBSTR(v_goal.Account,6,2))||'",
            "receiver_account_pan_id_number": "'| NVL( v_rec_account_pan_id_number, SUBSTR(V_S3C.CONTRACT_NUMBER,1,5) || '****' || SUBSTR(V_S3C.CONTRACT_NUMBER,-4,4))||'",
            "receiver_account_3dsecure": "'| REPLACE(NVL(v_rec_account_3dsecure, V_S3C.MOBILE_PHONE),'-')||'",
            "receiver_account_fin_inst_name": "'| REPLACE(V_ATTRIB.NAIMEN_UCHREJ,'"')/*trim(NVL(v_goal.institution_name, v_goal.Institution_Name_to))*/||'",
            "receiver_account_fin_inst_country": "'| (v_goal.incorporation_country_code)||'",
            "receiver_account_fin_inst_tax_id": "'| V_INN_BANK.INN /*replace(nvl(v_goal.incorporation_number,v_goal.Incorporation_Number_to),V_INN_BANK.INN)*/||'",
            "receiver_account_fin_inst_bic_id": "'| NVL(v_goal.swift,v_goal.Swift_to)||'",
            "receiver_account_fin_inst_branch": "'| NVL(v_goal.institution_name, v_goal.Institution_Name_to)||'",
            "receiver_account_fin_inst_bic": "'| NVL(v_goal.swift,v_goal.Swift_to)||'"
          }','"receiver_account_pan_id_number": "****",'),'"receiver_account_3dsecure": "",') , SYSDATE );

          V_INN_BANK.INN := NULL;
          -----
           END IF; ---NE CTR
          -------
           UTL_FILE.NEW_LINE(v_File);
          --------
          if V_REC_merchant_code is not null then

          UTL_FILE.PUTF ( v_File,',
          "Payment_System": {
            "receiver_merchant_code": "'| V_REC_merchant_code||'",
            "receiver_merchant_name": "'| trim(V_REC_merchant_name)||'",
            "receiver_ekvayer_bank": "'| V_REC_ekvayer_bank||'",
            "receiver_mcc_code": "'| V_REC_mcc_code||'"
          }
        },' , SYSDATE );

        end if;

        ----------

        -------
        /*if  v_report_code = 'CTR'then
          UTL_FILE.PUTF ( v_File,',', SYSDATE );

        end if;
        */
        if V_REC_merchant_code is  null then
          UTL_FILE.PUTF ( v_File,'},', SYSDATE );

        end if;
        ---

        UTL_FILE.PUTF ( v_File,'
        "Correspondence": {
          "debet": "'| substr(v_goal.debet,1,5) ||'",
          "Credit": "'| substr(v_goal.kredit,1,5)||'"
        }
      }
   ]
  }
  ' , SYSDATE );

  --V_service_name:=NULL;

   v_goal.filialnumber :=NULL;
   v_cif.regnom:=NULL;
   v_REGNUMBER_cif.Riskler:=NULL;


  V_REGISTRATION_NUMBER_REC :=NULL;
  V_REGISTRATION_NUMBER_SEN:=NULL;

  --v_merchant_name:=NULL;
  --v_merchant_code:=NULL;
  --v_ekvayer_bank:=NULL;
  --v_mcc_code:=NULL;
  V_trn_payment_sub_system:=NULL;
  v_trn_chanel :=NULL;
  V_trn_payment_system:=NULL;
  v_typeOfConnectionToPep:=NULL;
  v_customerRiskDegree:=NULL;
  v_sen_merchant_code:=null;
  V_REC_merchant_code:=null;
  V_REC_merchant_name:=null;
  v_goal.Primechanie:=null;

  V_START:=V_START+1;
  V_pla_sys.Plat_System:=NULL;
  v_pla_sys.message_id:=null;
  v_goal.document_source:=NULL;
  V_YURIK__NAME.INN_LICSCH:=NULL;
  V_YURIK__NAME.NAME_LICSCH:=NULL;
  V_YURIK__NAME.LICSCH:=NULL;
  V_YURIK__NAME.REGISTRAC_NOMER:=NULL;
  V_YURIK__NAME.COUNTRYCODE:=NULL;
  V_YURIK__NAME.BANK_BRANCH:=NULL;
  V_YURIK__NAME.BANK:=NULL;
  V_YURIK__NAME.INN:=NULL;
  V_YURIK__NAME.BIC_SWIFT:=NULL;


  IF V_START <V_KOL THEN

                 -----------
           UTL_FILE.PUTF ( v_File,',' , SYSDATE );
                 -----------
    END IF;


                 END LOOP;
                 CLOSE c_goal;

UTL_FILE.PUTF ( v_File, ']

  }


                 '

                 , SYSDATE );





   UTL_FILE.FCLOSE ( v_File );


end;
