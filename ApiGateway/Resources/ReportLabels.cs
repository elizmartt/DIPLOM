namespace ApiGateway.Resources;

public static class ReportLabels
{
    // ── Header ────────────────────────────────────────────────────────────────
    public const string SystemTitle  = "Բժշկական Ախտորոշման Համակարգ";
    public const string ReportTitle  = "ԱԻ-Աջակցված Ախտորոշման Հաշվետվություն";
    public const string Confidential = "Գաղտնի";
    public const string Generated    = "Ստեղծված";
    public const string Page         = "Էջ";
    public const string Of           = "-ից";

    // ── Patient Info ──────────────────────────────────────────────────────────
    public const string PatientInfo      = "Հիվանդի Տվյալներ";
    public const string PatientName      = "Հիվանդի Անուն";
    public const string PatientCode      = "Հիվանդի Կոդ";
    public const string Age              = "Տարիք";
    public const string Gender           = "Սեռ";
    public const string CaseId           = "Գործի ID";
    public const string DiagnosisType    = "Ախտորոշման Տեսակ";
    public const string CaseDate         = "Ամսաթիվ";
    public const string AttendingDoctor  = "Բուժող Բժիշկ";
    public const string Years            = "տ.";

    // ── Diagnosis Summary ─────────────────────────────────────────────────────
    public const string DiagnosisSummary   = "Ախտորոշման Ամփոփում";
    public const string OverallConfidence  = "Ընդհանուր Վստահություն";
    public const string RiskLevel          = "Ռիսկի Մակարդակ";
    public const string Recommendations    = "Առաջարկություններ";

    // ── Risk Levels ───────────────────────────────────────────────────────────
    public const string RiskHigh   = "ԲԱՐՁՐ";
    public const string RiskMedium = "ՄԻՋԻՆ";
    public const string RiskLow    = "ՑԱԾՐ";

    // ── Module Scores ─────────────────────────────────────────────────────────
    public const string ModuleScores   = "Մոդուլների Վստահության Գնահատականներ";
    public const string Module         = "Մոդուլ";
    public const string Confidence     = "Վստահություն";
    public const string Score          = "Գնահատական";
    public const string ImagingModule  = "Բժշկ. Պատկերների Վերլ. (ResNet18)";
    public const string LabModule      = "Լաբ. Վերլուծություն (Random Forest)";
    public const string ClinicalModule = "Կլինիկ. Ախտ. Վերլ. (Log. Regression)";
    public const string EnsembleModule = "Համակցված Մոդել (40/30/30)";

    // ── Section Titles ────────────────────────────────────────────────────────
    public const string ImagingResults  = "Պատկ. Վերլ. Արդյունքներ";
    public const string LabResults      = "Լաբ. Վերլ. Արդյունքներ";
    public const string ClinicalResults = "Կլինիկ. Ախտ. Արդյունքներ";
    public const string DoctorNotes     = "Բժշկի Նշումներ և Գնահատություն";

    // ── Common Fields ─────────────────────────────────────────────────────────
    public const string Prediction  = "Կանխատեսում";
    public const string Status      = "Կարգավիճակ";
    public const string Success     = "✓ Հաջողված";
    public const string Failed      = "✗ Ձախողված";
    public const string NoNotes     = "[Այս գործի համար բժշկի նշումներ չկան]";
    public const string DoctorSignature = "Բժշկի ստորագրություն: ___________________________";
    public const string Date        = "Ամսաթիվ";

    // ── Grad-CAM / Images ─────────────────────────────────────────────────────
    public const string GradCamTitle        = "Grad-CAM Բացատրելիության Վիզուալիզացիա";
    public const string GradCamDescription  = "Կարմիր/դեղին գույնով նշված տարածքները ամենամեծ ազդեցությունն են ունեցել ԱԻ ախտորոշման վրա։";
    public const string GradCamOverlayLabel = "Grad-CAM Ծածկույթ";
    public const string OriginalScanLabel   = "Բնօրինակ Սկան";
    public const string GradCamNotAvailable = "Grad-CAM պատկերները հասանելի չեն։ Պատկերի մշակումը կարող է դեռ ընթանալ։";

    // ── AI Interpretation block ───────────────────────────────────────────────
    public const string AIInterpretation = "ԱԻ Մեկնաբանություն";
    public const string AgreementScore   = "Համաձայնության Գնահատական";
    public const string Reliable         = "✓ Հուսալի";
    public const string NotReliable      = "⚠ Անհուսալի";

    // ── System / Footer ───────────────────────────────────────────────────────
    public const string SystemSignature = "Ստեղծվել է ԱԻ-Ախտ. Համակարգի կողմից";
    public const string ReportId        = "Հաշ. ID";

    // ── Disclaimer ────────────────────────────────────────────────────────────
    public const string Disclaimer =
        "⚠ ԶԳՈՒՇԱՑՈՒՄ. Այս հաշվետվությունը ստեղծվել է ԱԻ-աջակցված ախտորոշման համակարգի կողմից " +
        "և նախատեսված է բացառապես որակավորված բժիշկների համար՝ որպես երկրորդ կարծիք։ " +
        "Այն չի փոխարինում կլինիկական դատողությանը։ Բոլոր ախտորոշումները պետք է հաստատվեն " +
        "լիցենզավորված բժշկի կողմից։";

    // ─────────────────────────────────────────────────────────────────────────
    // Static helper methods
    // ─────────────────────────────────────────────────────────────────────────

    public static string TranslateGender(string? gender) => gender?.ToLower() switch
    {
        "male"   or "m" or "արական"  => "Արական",
        "female" or "f" or "իգական" => "Իգական",
        _ => gender ?? "—"
    };

    public static string TranslateDiagnosisType(string? diagnosisType) =>
        diagnosisType?.ToUpper() switch
        {
            "BRAIN_TUMOR"   => "Ուղեղի Ուռուցք",
            "LUNG_CANCER"   => "Թոքի Քաղցկեղ",
            "ALZHEIMER"     => "Ալցհեյմերի Հիվանդություն",
            "GENERAL"       => "Ընդհանուր Ախտորոշում",
            _ => diagnosisType ?? "—"
        };

    /// <summary>
    /// Translates AI model prediction class names to Armenian.
    /// Falls back to the original value if no mapping found.
    /// </summary>
    public static string TranslatePrediction(string? prediction)
    {
        if (string.IsNullOrWhiteSpace(prediction)) return "—";
        return PredictionTranslations.TryGetValue(prediction.Trim().ToLower(), out var translated)
            ? translated
            : prediction;
    }

    private static readonly Dictionary<string, string> PredictionTranslations = new()
    {
        // Brain tumor classes
        { "glioma",                "Գլիոմա" },
        { "meningioma",            "Մենինգիոմա" },
        { "pituitary",             "Հիպոֆիզի Ուռուցք" },
        { "notumor",               "Ուռուցք Չկա" },
        { "no_tumor",              "Ուռուցք Չկա" },
        { "normal",                "Նորմալ" },
        // Alzheimer classes
        { "milddementiaresnet18",         "Ալցհ. (Մեղմ)" },
        { "moderatedementia",      "Ալցհ. (Չափ.)" },
        { "nondemented",           "Ալցհ. Չկա" },
        { "verymilddementia",      "Ալցհ. (Շ.Մեղ.)" },
        { "alzheimer_mild",        "Ալցհ. (Մեղմ)" },
        { "alzheimer_moderate",    "Ալցհ. (Չափ.)" },
        { "alzheimer_very_mild",   "Ալցհ. (Շ.Մեղ.)" },
        { "alzheimer_none",        "Ալցհ. Չկա" },
        // Lung cancer classes
        { "lung_cancer",           "Թոքի Քաղ." },
        { "no_cancer",             "Քաղցկեղ Չկա" },
        { "malignant",             "Չարորակ" },
        { "benign",                "Բարորակ" },
        { "inconclusive",          "Անորոշ" },
        { "adenocarcinoma",        "Ադենոկ." },
        { "squamous_cell",         "Թ.Բ.Ք." },
        { "large_cell",            "Մ.Բ.Ք." },
        // Generic / legacy
        { "class_6",               "—" },   // legacy artifact — suppress
        { "positive",              "Դրական" },
        { "negative",              "Բացասական" },
    };

    /// <summary>
    /// Translates recommendation strings generated by the orchestrator to Armenian.
    /// Falls back to the original if no mapping found.
    /// </summary>
    public static string TranslateRecommendation(string? recommendation)
    {
        if (string.IsNullOrWhiteSpace(recommendation)) return "—";
        return RecommendationTranslations.TryGetValue(recommendation.Trim(), out var translated)
            ? translated
            : recommendation;
    }

    private static readonly Dictionary<string, string> RecommendationTranslations = new()
    {
       { "MRI with contrast recommended",              "Խորհուրդ է տրվում կատարել ՄՌՏ հակադրությամբ" },
       { "Immediate neurosurgical consultation",       "Անհրաժեշտ է անհապաղ նյարդավիրաբուժական խորհրդատվություն" },
       { "Follow-up imaging in 3 months",              "Խորհուրդ է տրվում կրկնել պատկերային հետազոտությունը 3 ամսից" },
       { "Biopsy recommended",                         "Խորհուրդ է տրվում կատարել բիոպսիա" },
       { "Cognitive assessment recommended",           "Խորհուրդ է տրվում կատարել կոգնիտիվ գնահատում" },
       { "Neurological examination required",          "Անհրաժեշտ է նյարդաբանական հետազոտություն" },
       { "No immediate action required",               "Անհապաղ միջամտություն չի պահանջվում" },
       { "Pulmonary function tests recommended",       "Խորհուրդ է տրվում թոքերի ֆունկցիոնալ թեստեր" },
       { "CT scan of chest recommended",               "Խորհուրդ է տրվում կրծքավանդակի ՀՇ (համակարգչային շերտագրություն)" },
       { "Oncology consultation required",             "Անհրաժեշտ է ուռուցքաբանի խորհրդատվություն" },
       { "Repeat laboratory tests in 2 weeks",         "Խորհուրդ է տրվում կրկնել լաբորատոր հետազոտությունները 2 շաբաթից" },
       { "Monitor symptoms closely",                   "Անհրաժեշտ է ուշադիր հետևել ախտանիշներին" },
       { "Lifestyle modifications advised",            "Խորհուրդ է տրվում փոփոխել կենսակերպը" },
       { "Specialist referral recommended",            "Խորհուրդ է տրվում դիմել համապատասխան մասնագետի" },
       { "Regular monitoring recommended",             "Խորհուրդ է տրվում կանոնավոր վերահսկում" },
       { "Chemotherapy evaluation needed",             "Անհրաժեշտ է քիմիաթերապիայի գնահատում" },
       { "Radiation therapy consultation",             "Անհրաժեշտ է ճառագայթային թերապիայի մասնագետի խորհրդատվություն" },
       { "Brain biopsy recommended",                   "Խորհուրդ է տրվում գլխուղեղի բիոպսիա" },
       { "Stereotactic radiosurgery evaluation",       "Անհրաժեշտ է ստերեոտակտիկ ռադիովիրաբուժության գնահատում" },
       { "MRI follow-up in 6 months",                  "Խորհուրդ է տրվում վերահսկիչ ՄՌՏ 6 ամսից" },
       { "Neurosurgery consultation required",         "Անհրաժեշտ է նյարդավիրաբույժի խորհրդատվություն" },
       { "Neurosurgical consultation required",        "Անհրաժեշտ է նյարդավիրաբուժական խորհրդատվություն" },
       { "Monitor for symptom progression",            "Անհրաժեշտ է վերահսկել ախտանիշների առաջընթացը" },
       { "Low confidence - consider additional diagnostic tests", "Վստահության ցածր մակարդակ — խորհուրդ է տրվում լրացուցիչ ախտորոշիչ հետազոտություններ" },
       { "Low confidence - additional tests recommended", "Վստահության ցածր մակարդակ — խորհուրդ են տրվում լրացուցիչ հետազոտություններ" },
       { "Consider additional imaging",                "Խորհուրդ է տրվում լրացուցիչ պատկերային հետազոտություն" },
       { "Surgical evaluation recommended",            "Խորհուրդ է տրվում վիրաբուժական գնահատում" },
       { "Histological examination required",          "Անհրաժեշտ է հյուսվածաբանական հետազոտություն" },
       { "Multidisciplinary team review",              "Անհրաժեշտ է բազմամասնագիտական թիմի քննարկում" },
       { "Follow-up in 1 month",                       "Խորհուրդ է տրվում վերահսկիչ այց 1 ամսից" },
       { "Follow-up in 3 months",                      "Խորհուրդ է տրվում վերահսկիչ այց 3 ամսից" },
       { "Genetic counseling advised",                 "Խորհուրդ է տրվում գենետիկական խորհրդատվություն" },
    };


    public static string FormatDateArmenian(DateTime date)
    {
        string[] months =
        {
            "Հունվարի", "Փետրվարի", "Մարտի",    "Ապրիլի",
            "Մայիսի",   "Հունիսի",  "Հուլիսի",  "Օգոստոսի",
            "Սեպտեմբերի","Հոկտեմբերի","Նոյեմբերի","Դեկտեմբերի"
        };
        return $"{date.Day} {months[date.Month - 1]} {date.Year}";
    }
}