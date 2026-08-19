-- ══════════════════════════════════════════════════════════════════════════
-- AML Hesab üzrə sorğu — TİP DİAQNOSTİKASI
-- ORA-00932 (expected NUMBER got CHAR) səbəbini tapmaq üçün.
-- Sadəcə bunu işlət və nəticəni olduğu kimi göndər.
-- ══════════════════════════════════════════════════════════════════════════

select c.table_name,
       c.column_name,
       c.data_type,
       c.data_length,
       c.data_precision,
       c.data_scale,
       c.nullable
  from all_tab_columns c
 where c.owner = 'ODB'
   and (
        (c.table_name in ('DOC_VNESH_INVAL','DOC_VNESH_NACVAL',
                          'DOC_VNESH_POSTUPL','DOC_VNESH_SWIFT')
         and c.column_name in ('ID','PLAT_SYSTEM','KREDIT_INN','INN_CREDIT',
                               'INN_DEBET','BIC_DEBET','BIC_KREDIT',
                               'SENDER_BIC','RECEIVER_BIC','SENDER_BANK_BIC',
                               'BENEFICIARY_BANK_BIC','MFO_DEBET','MFO_CREDIT',
                               'MFO_KREDIT','KREDIT','DEBET','ACCOUNT_NO',
                               'BENEFICIARY_ACCOUNT','SENDER_ACCOUNT',
                               'AMOUNT','SUM1','SUMMA_V_NACVAL','DATE_OPER',
                               'VALUE_DATE','NOMER_DOCUM','CURRENCY'))
     or (c.table_name = 'MUXBIR_HESAB'
         and c.column_name in ('KOD','SWIFT_KODU','VOEN','TESHKILATIN_ADI',
                               'VALYUTA_KODU','MUXBIR_HESAB'))
     or (c.table_name = 'ARH_DD'
         and c.column_name in ('RECNUM','ID_VD','DEBET','KREDIT','KOD_VALUTI',
                               'DATE_OPER','SUMMA_V_INVAL','SUMMA_V_NACVAL',
                               'PRIMECHANIE','VID_OPERACII','NOMER_DOCUM'))
     or (c.table_name = 'REGNOM'
         and c.column_name in ('REGNOM','INN_REGNOM','PINCODE','NAME_REGNOM'))
     or (c.table_name = 'LICSCH'
         and c.column_name in ('LICSCH','INN_LICSCH','COUNTRYCODE','NAME_LICSCH'))
     or (c.table_name = 'MFO'
         and c.column_name in ('MFO','BANK_LARGE_NAME'))
       )
 order by c.table_name, c.column_name;
