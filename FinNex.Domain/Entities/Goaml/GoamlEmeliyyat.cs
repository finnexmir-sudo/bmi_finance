namespace FinNex.Domain.Entities.Goaml
{
    /// <summary>
    /// GOAML — Risk/AML şübhəli əməliyyat hesabatı, "Əməliyyatlar" tabı.
    ///
    /// Köhnə BMI Oracle Forms "AYSEL/GOAML" modulunun köçürülməsi (05-06.11.2026
    /// sessiyasında real Oracle sxemi araşdırılıb). Oracle-un öz `ODB.GOAML_CLIENTS`
    /// cədvəli bizim üçün YALNIZ oxuna bilər (layihənin "Oracle-a yazı qadağandır"
    /// qaydası) — ona görə bu entity O cədvəlin GÜZGÜSÜDÜR, amma bizim öz SQL
    /// Server bazamızdadır. Sütun adları Oracle-un real `INSERT INTO
    /// ODB.GOAML_CLIENTS(...)` ifadəsindən (v$sql ilə tutulub, 06.11.2026)
    /// BİR-BİR köçürülüb ki, gələcəkdə ixrac/müqayisə asan olsun — hər sahənin
    /// şərhində Oracle-dakı ƏSL adı saxlanılır.
    ///
    /// ARXİTEKTUR QƏRARLARI (istifadəçi ilə bu sessiyada müzakirə olunub):
    /// - Namizəd əməliyyat `odb.DOC_DNA_ALL_TAB` + `odb.DOC_DNA_ALL_RISK`-dən
    ///   (JOIN: DATE_OPER+FILIALNUMBER+RECNUM) OXUNUR — bax <see cref="GoamlOracleService"/>.
    ///   Namizədin Oracle açarı (<see cref="CandidateFilialNumber"/>/<see cref="CandidateDateOper"/>/
    ///   <see cref="CandidateRecNum"/>) audit üçün SAXLANILIR, amma Oracle-a HEÇ VAXT
    ///   geri yazılmır.
    /// - 5 KYC blokunun forma başlıqları ilə Oracle sütun suffiksi uyğunluğu
    ///   (06.11.2026, canlı AYSEL ekranından TƏSDİQLƏNİB):
    ///     "Nümayəndənin"                       → _CON (Conductor)
    ///     "Hesab imza hüququ olan həxsin"       → _SIG (Signatory)
    ///     "Hüquqi şəxsin"                       → suffikssiz (göndərən tərəfin hüquqi şəxsi)
    ///     "Alan fiziki həxsin"                  → _TO, fiziki hissə
    ///     "Banka aid olmayan müş. Hüquq şəxsin" → _TO, hüquqi hissə (EYNİ _TO qrupu)
    /// - `TIP` (<see cref="GoamlTipi"/>, 13 dəyər) və `REPORT_CODE`
    ///   (<see cref="GoamlHesabatNovu"/>, ~16 dəyər, "Məlumatın növü") operator
    ///   tərəfindən ƏL İLƏ seçilir — xam əməliyyat datasından avtomatik
    ///   hesablanmır (Oracle-da da belədir, `GOAML_CLIENTS`-də bunlara uyğun
    ///   gələn heç bir hesablama sütunu yoxdur).
    /// - `REPR_ID_*` sütunları Nümayəndənin ÖZ şəxsiyyət sənədindən (ConSenedin...)
    ///   AYRI saxlanılıb — Oracle-da da iki qrup fərqlidir (bax `ID_TYPE_CON` vs
    ///   `repr_id_type`), çox güman nümayəndəni SƏLAHİYYƏTLƏNDİRƏN sənədin (etibarnamə)
    ///   öz seriya/nömrəsidir, nümayəndənin şəxsi sənədi DEYİL. Dəqiqləşdirilməyib,
    ///   formada ayrıca xana kimi görünməyib — gələcəkdə forma ekran-ekran izlənərkən
    ///   təsdiqlənə bilər.
    /// - `SENDER_ID_*`/`REC_ID_*` və (suffikssiz) `gender`/`snn` sütunlarının HANSI
    ///   bloka aid olduğu formanın görünən hissəsindən TAM aydınlaşmayıb (FinNex
    ///   özünün ad-şərhi "ehtimal olunan" işarəsi daşıyır) — Oracle adı ilə
    ///   saxlanılıb, səhv aid edilmə riski olmasın deyə.
    /// </summary>
    public class GoamlEmeliyyat : BaseEntity
    {
        // ── Bizim öz iş axınımız (Oracle-da QARŞILIĞI YOXDUR) ──────────────
        /// <summary>Hazırda YALNIZ "Qaralama" (hələ ixrac olunmayıb). Gələcək
        /// mərhələdə ixrac statusu əlavə oluna bilər.</summary>
        public GoamlEmeliyyatStatusu Status { get; set; } = GoamlEmeliyyatStatusu.Qaralama;

        /// <summary>Təsdiqləyən operator (FinNex işçisi) — Oracle-da USER/US_NAME-ə
        /// uyğun gəlir, amma bizdə sistem istifadəçisinin ÖZÜdür.</summary>
        public int TesdiqleyenIsciId { get; set; }

        // ── Namizəd əməliyyatın Oracle açarı (audit, YALNIZ OXU) ───────────
        /// <summary>Oracle `DOC_DNA_ALL_TAB.FILIALNUMBER` / `DOC_DNA_ALL_RISK.FILIALNUMBER`.</summary>
        public int CandidateFilialNumber { get; set; }
        /// <summary>Oracle `DOC_DNA_ALL_TAB.DATE_OPER`.</summary>
        public DateTime CandidateDateOper { get; set; }
        /// <summary>Oracle `DOC_DNA_ALL_TAB.RECNUM` — JOIN açarının üçüncü hissəsi.</summary>
        public long CandidateRecNum { get; set; }

        // ══ ƏMƏLİYYATIN ÖZÜ (Oracle: suffikssiz, əməliyyat-səviyyəli sahələr) ══
        public string? Debet { get; set; }                 // DEBET
        public string? Kredit { get; set; }                 // KREDIT
        public string? VipNetDa { get; set; }               // VIP_NET_DA
        public decimal? SummaVInval { get; set; }           // SUMMA_V_INVAL
        public decimal? SummaVNacval { get; set; }          // SUMMA_V_NACVAL
        public int? KodValuti { get; set; }                 // KOD_VALUTI
        public decimal? KursValuti { get; set; }             // KURS_VALUTI
        /// <summary>"Tip" — Fizik/Hüquqi + İstiqamət (əl ilə seçilir).</summary>
        public GoamlTipi? Tip { get; set; }                 // TIP
        /// <summary>"Məlumatın növü" — hesabat kateqoriyası (STR, CTR, PEP, QHT, TPR1-3...).</summary>
        public GoamlHesabatNovu? ReportCode { get; set; }   // REPORT_CODE
        public string? TransactionNumber { get; set; }      // TRANSACTIONNUMBER — "Əməl. nömrəsi"
        public string? Primechanie { get; set; }            // PRIMECHANIE — "Qeydlər"
        public DateTime? DateOper { get; set; }              // DATE_OPER — "Əməl. tarixi"
        public string? TransmodeCode { get; set; }           // TRANSMODE_CODE — "Vəsaitin növü"
        public string? TransmodeComment { get; set; }        // TRANSMODE_COMMENT — "Əməliyyatın növün İzahatı"
        public decimal? AmountLocal { get; set; }            // AMOUNT_LOCAL — "Manat ekvivalenti"
        public string? FromFundsCode { get; set; }           // FROM_FUNDS_CODE — "Əməliyyatın növü"
        public string? ForeignCurrencyCode { get; set; }     // FOREIGN_CURRENCY_CODE — "Mədaxil Valyutası"
        public string? ForeignCurrencyCodeFrom { get; set; } // foreign_currency_code_from
        public decimal? ForeignAmount { get; set; }          // FOREIGN_AMOUNT — "Xarici Valyutada məbləği"
        public decimal? ForeignExchangeRate { get; set; }    // FOREIGN_EXCHANGE_RATE — "Mezənnə"
        public string? ToFundsCode { get; set; }             // TO_FUNDS_CODE
        public string? Action { get; set; }                  // ACTION
        public string? Reason { get; set; }                  // REASON — "Səbəb"
        public string? Url { get; set; }                     // URL
        public DateTime? Opened { get; set; }                 // OPENED
        public string? Business { get; set; }                // BUSINESS
        public string? RegistracNomer { get; set; }          // REGISTRAC_NOMER
        public string? Branch { get; set; }                  // branch
        public string? Iban { get; set; }                    // IBAN
        public string? PersonalAccountType { get; set; }     // personal_account_type
        public string? GelirMenbeyi { get; set; }             // GELIR_MENBEYI

        // Alıcının növ bayraqları (sağdakı "Fizik/Sah/Huq" checkbox-ları)
        public bool? ToFizik { get; set; }                    // TO_FIZIK
        public bool? ToYurik { get; set; }                    // TO_YURIK
        public bool? ToPred { get; set; }                     // TO_PRED (Sahibkar)
        public string? PanTo { get; set; }                    // PAN_TO — "Pan To"

        // ══ BLOK 1 — "Nümayəndənin" (Oracle: _CON, Conductor) ══════════════
        public string? ConAdi { get; set; }                   // FIRST_NAME_CON
        public string? ConAtasininAdi { get; set; }           // MIDDLE_NAME_CON
        public string? ConSoyadi { get; set; }                // LAST_NAME_CON
        public DateTime? ConDogulduguTarix { get; set; }      // BIRTHDATE_CON
        public string? ConDogulduguYer { get; set; }          // BIRTH_PLACE_CON
        public string? ConVetendasligi { get; set; }          // NATIONALITY1_CON
        public string? ConTelefonNovu { get; set; }           // TPH_CONTACT_TYPE_CON
        public string? ConRabiteNovu { get; set; }            // TPH_COMMUNICATION_TYPE_CON
        public string? ConTelefonNomresi { get; set; }        // TPH_NUMBER_CON
        public string? ConUnvaninNovu { get; set; }           // ADDRESS_TYPE_CON
        public string? ConUnvan { get; set; }                 // ADDRESS_CON
        public string? ConSeher { get; set; }                 // CITY_CON
        public string? ConOlke { get; set; }                  // COUNTRY_CODE_CON
        public string? ConSenedinNovu { get; set; }           // ID_TYPE_CON
        public string? ConSenedinSeriyaNomresi { get; set; }  // ID_NUMBER_CON
        public DateTime? ConSenedinVerilmeTarixi { get; set; } // ID_ISSUE_DATE_CON
        public DateTime? ConSenedinEtibarliliqMuddeti { get; set; } // ID_EXPIRY_DATE (Oracle-da _CON suffiksi YOXDUR)
        public string? ConVerenOrqan { get; set; }            // ID_ISSUED_BY_CON
        public string? ConVerildiyiOlke { get; set; }         // ID_ISSUE_COUNTRY_CON
        public string? ConCinsi { get; set; }                 // gender_CON
        public string? ConPin { get; set; }                   // SNN_CON

        // Nümayəndəni səlahiyyətləndirən sənəd (etibarnamə, ehtimal olunan — bax yuxarı şərh)
        public string? ReprIdType { get; set; }               // repr_id_type
        public string? ReprIdSerial { get; set; }             // repr_id_serial
        public string? ReprIdNumber { get; set; }             // repr_id_number

        // ══ BLOK 2 — "Hesab imza hüququ olan həxsin" (Oracle: _SIG, Signatory) ══
        public string? SigAdi { get; set; }                   // FIRST_NAME_SIG
        public string? SigAtasininAdi { get; set; }           // MIDDLE_NAME_SIG
        public string? SigSoyadi { get; set; }                // LAST_NAME_SIG
        public DateTime? SigDogulduguTarix { get; set; }      // BIRTHDATE_SIG
        public string? SigDogulduguYer { get; set; }          // BIRTH_PLACE_SIG
        public string? SigVetendasligi { get; set; }          // NATIONALITY1 (SIG blokunun daxilində, suffiksiz)
        public string? SigTelefonNovu { get; set; }           // TPH_CONTACT_TYPE_SIG
        public string? SigRabiteNovu { get; set; }            // TPH_COMMUNICATION_TYPE_SIG
        public string? SigTelefonNomresi { get; set; }        // TPH_NUMBER_SIG
        public string? SigUnvaninNovu { get; set; }           // ADDRESS_TYPE_SIG
        public string? SigUnvan { get; set; }                 // ADDRESS_SIG
        public string? SigSeher { get; set; }                 // CITY_SIG
        public string? SigOlke { get; set; }                  // COUNTRY_CODE_SIG
        public string? SigSenedinNovu { get; set; }           // ID_TYPE_SIG
        public string? SigSenedinSeriyaNomresi { get; set; }  // ID_NUMBER_SIG
        public DateTime? SigSenedinVerilmeTarixi { get; set; } // ID_ISSUE_DATE_SIG
        public DateTime? SigSenedinEtibarliliqMuddeti { get; set; } // ID_EXPIRY_DATE_SIG
        public string? SigVerenOrqan { get; set; }            // ID_ISSUED_BY_SIG
        public string? SigVerildiyiOlke { get; set; }         // ID_ISSUE_COUNTRY_SIG
        public string? SigVezife { get; set; }                // "Vəzifə" — Oracle sütunu formada ayrıca görünüb, adı hələ tutulmayıb
        public string? SigIsYeriAdi { get; set; }             // "İş yerinin adı" — eyni

        // ══ BLOK 3 — "Hüquqi şəxsin" (göndərən tərəf, suffikssiz) ══════════
        public string? InstitutionName { get; set; }          // INSTITUTION_NAME
        public string? Swift { get; set; }                    // SWIFT
        public string? Account { get; set; }                  // ACCOUNT
        public string? IncorporationNumber { get; set; }      // INCORPORATION_NUMBER — "VÖEN"
        public string? IncorporationCountryCode { get; set; } // INCORPORATION_COUNTRY_CODE — "Ölkə"
        public string? NameUr { get; set; }                   // NAME_UR — "ADI"
        public string? FromCountry { get; set; }              // FROM_COUNTRY — "Əməliyyatı başladığı ölkə"
        public DateTime? HesabinAcilmaTarixi { get; set; }     // "HESABIN açılma tarixi" — Oracle adı hələ tutulmayıb
        public string? BankinAdi { get; set; }                 // "Bankın Adı"
        public string? SeherinKodu { get; set; }               // "Şəhərin kodu"
        public string? FealiyyatIstiqameti { get; set; }       // "Fəaliyyət istiqaməti"

        // ══ BLOK 4 — "Alan fiziki həxsin" (Oracle: _TO, fiziki hissə) ══════
        public string? ToAdi { get; set; }                    // FIRST_NAME_TO
        public string? ToAtasininAdi { get; set; }            // MIDDLE_NAME_TO
        public string? ToSoyadi { get; set; }                 // LAST_NAME_TO
        public DateTime? ToDogulduguTarix { get; set; }       // BIRTHDATE_TO
        public string? ToDogulduguYer { get; set; }           // BIRTH_PLACE_TO
        public string? ToVetendasligi { get; set; }           // NATIONALITY1_TO
        public string? ToTelefonNovu { get; set; }            // TPH_CONTACT_TYPE_TO
        public string? ToRabiteNovu { get; set; }             // TPH_COMMUNICATION_TYPE_TO
        public string? ToTelefonNomresi { get; set; }         // TPH_NUMBER_TO
        public string? ToUnvaninNovu { get; set; }            // ADDRESS_TYPE_TO
        public string? ToUnvan { get; set; }                  // ADDRESS_TO
        public string? ToSeher { get; set; }                  // CITY_TO
        public string? ToOlke { get; set; }                   // COUNTRY_CODE_TO
        public string? ToOlkeAdi { get; set; }                // TO_COUNTRY_TO
        public string? ToSenedinNovu { get; set; }            // ID_TYPE_TO
        public string? ToSenedinSeriyaNomresi { get; set; }   // ID_NUMBER_TO
        public DateTime? ToSenedinVerilmeTarixi { get; set; } // ID_ISSUE_DATE_TO
        public DateTime? ToSenedinEtibarliliqMuddeti { get; set; } // ID_EXPIRY_DATE_TO
        public string? ToVerenOrqan { get; set; }             // ID_ISSUED_BY_TO
        public string? ToVerildiyiOlke { get; set; }          // ID_ISSUE_COUNTRY_TO

        // ══ BLOK 5 — "Banka aid olmayan müş. Hüquq şəxsin" (Oracle: _TO, hüquqi hissə) ══
        public string? IncorporationCountryCodeTo { get; set; } // INCORPORATION_COUNTRY_CODE_TO
        public string? IncorporationNumberTo { get; set; }      // INCORPORATION_NUMBER_TO — "VÖEN"
        public string? NameUrTo { get; set; }                   // NAME_UR_TO — "ADI"
        public string? InstitutionNameTo { get; set; }          // INSTITUTION_NAME_TO — "Bankın Adı"
        public string? SwiftTo { get; set; }                    // SWIFT_TO
        public string? AccountTo { get; set; }                  // ACCOUNT_TO — "HESAB nömrəsi"
        public string? AlaqeNovuTo { get; set; }                // "Əlaqə növü" — Oracle adı tutulmayıb

        // ── Tam dəqiq bloka aid edilməmiş, Oracle adı ilə saxlanılan sahələr ──
        public string? Gender { get; set; }                   // gender (suffikssiz)
        public string? Snn { get; set; }                       // snn (suffikssiz)
        public string? GenderTo { get; set; }                  // gender_to
        public string? SnnTo { get; set; }                      // snn_to
        public string? Occupation { get; set; }                 // occupation
        public string? EmployerName { get; set; }               // employer_name
        public string? SenderIdType { get; set; }                // sender_id_type
        public string? SenderIdSerial { get; set; }              // sender_id_serial
        public string? SenderIdNumber { get; set; }              // sender_id_number
        public string? RecIdType { get; set; }                   // rec_id_type
        public string? RecIdSerial { get; set; }                 // rec_id_serial
        public string? RecIdNumber { get; set; }                 // rec_id_number
    }
}
