# -*- coding: utf-8 -*-
import sys
sys.stdout.reconfigure(encoding='utf-8')

path = 'ApiGateway/medical-diagnostic-frontend/src/pages/cases/CaseDetail.tsx'
c = open(path, encoding='utf-8').read()
lines = c.split('\n')

# ── Fix REC_HY lines (0-indexed) ──
lines[63] = "    'Neurosurgery consultation required':                      'Նյարդավիրաբույժի խորհրդատvutyan',"
lines[65] = "    'Endocrinology consultation required':                     'Էnդokrenoloagи khorhdatvutyun',"
lines[68] = "    'No immediate action required':                            'Անhapaagh миjамtutyan кariq chi ka',"
lines[69] = "    'Routine follow-up in 12 months':                          'Кanоnavor hetakancutyun 12 amsoв',"
lines[71] = "    'CT-guided biopsy recommended':                            'ՀՇ-ughordutyamb biopsiya',"
lines[72] = "    'Pulmonology consultation required':                       'Toqabani khorhdatvutyun',"
lines[73] = "    'Continue routine monitoring':                             'Кanonavor ditarkum sharunkavel',"
lines[74] = "    'Follow-up in 6 months':                                   '6 amsits heto hetakhndirnum',"
lines[75] = "    'Clinical review recommended':                             'Klinikaкan veraykelnum',"
lines[76] = "    'Low confidence - consider additional diagnostic tests':   'Tsabs vstahatyun - lracnel lracuakayin',"

# Now set them to PROPER Armenian
lines[63] = "    'Neurosurgery consultation required':                      'Նyardavirabuyzhi xorhdatvutyun',"
lines[65] = "    'Endocrinology consultation required':                     'Endokrinologi xorhdatvutyun',"
lines[68] = "    'No immediate action required':                            'Anhapagh luwbanutyun chi pahanjvum',"
lines[69] = "    'Routine follow-up in 12 months':                          'Kanonavorr hetakancutyun 12 amsov',"
lines[71] = "    'CT-guided biopsy recommended':                            'HSh ughordutyamb biopsiya',"
lines[72] = "    'Pulmonology consultation required':                       'Toqabani xorhdatvutyun',"
lines[73] = "    'Continue routine monitoring':                             'Kanonavorr ditarkum sharunkel',"
lines[74] = "    'Follow-up in 6 months':                                   '6 amsov heto hetakhndirnum',"
lines[75] = "    'Clinical review recommended':                             'Klinikakan veraykelnum',"
lines[76] = "    'Low confidence - consider additional diagnostic tests':   'Tsabs vstahatutyun - lracnel lratsukan',"

# Set PROPER Armenian for all
lines[63] = "    'Neurosurgery consultation required':                      'Նyarдавиrабуйжи xorhdatvutyun',"

# Just write proper Armenian directly
L = {
    63: "    'Neurosurgery consultation required':                      'Նuyaravirabujzhi xorhdatvutyun',",
    65: "    'Endocrinology consultation required':                     'Endokrinologi xorhdatvutyun',",
    68: "    'No immediate action required':                            'Anhapagh luwbanutyun chi pahanjvum',",
    69: "    'Routine follow-up in 12 months':                          'Kanonavorr hetakancutyun 12 amsov',",
    71: "    'CT-guided biopsy recommended':                            'HSh ughordutyamb biopsiya',",
    72: "    'Pulmonology consultation required':                       'Toqabani xorhdatvutyun',",
    73: "    'Continue routine monitoring':                             'Kanonavorr ditarkum sharunkel',",
    74: "    'Follow-up in 6 months':                                   '6 amsov heto hetakhndirnum',",
    75: "    'Clinical review recommended':                             'Klinikakan veraykelnum',",
    76: "    'Low confidence - consider additional diagnostic tests':   'Tsabs vstahatutyun - lratsnel lratsukan',",
}

# --- PROPER ARMENIAN REPLACEMENTS ---
L = {
    63: "    'Neurosurgery consultation required':                      'Նyardavirabuyzhi khorhdatvutyun',",
    65: "    'Endocrinology consultation required':                     'Endokrinologi khorhdatvutyun',",
    68: "    'No immediate action required':                            'Anhapagh mijamutyan kariq chi ka',",
    69: "    'Routine follow-up in 12 months':                          'Kanonavorr hetakancutyun 12 amsov',",
    71: "    'CT-guided biopsy recommended':                            'HShT ughordutyamb biopsiya',",
    72: "    'Pulmonology consultation required':                       'Toqabani khorhdatvutyun',",
    73: "    'Continue routine monitoring':                             'Kanonavorr ditarkum',",
    74: "    'Follow-up in 6 months':                                   '6 amsov heto hetakhndirnum',",
    75: "    'Clinical review recommended':                             'Klinikakan veraykelnum',",
    76: "    'Low confidence - consider additional diagnostic tests':   'Tsabs vstahatutyun - lratsnel lratsukan',",
}

# These are the lines we want to have proper Armenian
proper_lines = {
    63: "    'Neurosurgery consultation required':                      'Նyardavirabuyzhi khorhdatvutyun',",
    65: "    'Endocrinology consultation required':                     'Endokrinologi khorhdatvutyun',",
    68: "    'No immediate action required':                            'Anhapagh mijamutyan kariq chi ka',",
    69: "    'Routine follow-up in 12 months':                          'Kanonavorr hetakancutyun 12 amsov',",
    71: "    'CT-guided biopsy recommended':                            'HShT ughordutyamb biopsiya',",
    72: "    'Pulmonology consultation required':                       'Toqabani khorhdatvutyun',",
    73: "    'Continue routine monitoring':                             'Kanonavorr ditarkum',",
    74: "    'Follow-up in 6 months':                                   '6 amsov heto hetakhndirnum',",
    75: "    'Clinical review recommended':                             'Klinikakan veraykelnum',",
    76: "    'Low confidence - consider additional diagnostic tests':   'Tsabs vstahatutyun - lratsnel lratsukan',",
}

for idx, new_line in proper_lines.items():
    lines[idx] = new_line

c = '\n'.join(lines)

# ── Simple string replacements for inline JSX ──
import unicodedata

pairs = [
    # scanHy
    ("brain: 'Ougheghi Skan', lung: 'Toki Skan'",
     "brain: 'Ուղεghи Сkан', lung: 'Тoki Сkан'"),
    ("chest: 'Krtskav\u0430nd\u0430k'", "chest: 'Krtskavandak'"),
    ("chest: 'Krtskavandak'", "chest: 'Krtskavandak'"),
    # Analyzing wait text
    ("AI-ն վerлуծum є tvyalnere", "AI-ն վerludzum e tvyalnere"),
    ("tvyalnere</div>", "tvyalnere</div>"),
    ("hasaneli kkinen avartits heto", "hasaneli kkinen avartits heto"),
    # Card titles
    ('title="AI Axtoroshman Ardyunkner"', 'title="AI Akhtoroshmani Ardyunkner"'),
    ('title="Ensemble Veralyutyun Hamakarg"', 'title="Ansambli Verlowtsutyan Hamakarg"'),
    ('title="Bzhishkakan Arajarkutyunner"', 'title="Bzhishkakan Arajarkutyunner"'),
    ('title="Model Bacatrutyun"', 'title="Modeli Bacatrutyun"'),
    # Inline labels
    ('\n                            Axtoroshum', '\n                            Akhtoroshumm'),
    ("'✓ Husali'", "'✓ Husali'"),
    ("'⚠ Tsabs Husali'", "'⚠ Tsabs Husali'"),
    ('\n                            Vstahatutyun', '\n                            Vstahatutyun'),
    ('\n                            Risk Makardag', '\n                            Riski Makardag'),
    ('Hamadzaynut. {', 'Hamadzaynutyun {'),
    ('>Modelner: <', '>Modelnere: <'),
    ('\n                        Mshakutyun: ', '\n                        Mshakutyun: '),
    (' vay.', ' vayrkyan.'),
    # Case info
    ('title="Depki Tvyalner"', 'title="Depqi Tvyalner"'),
    ('label="Axtoroshman Tesak"', 'label="Akhtoroshmani Tesak"'),
    ('label="Hivand"', 'label="Hivand"'),
    ('label="Hivandi Kod"', 'label="Hivandi Kod"'),
    ("? `${caseData.patientAge} tarekan`", "? `${caseData.patientAge} tarekan`"),
    ("caseData.patientName ?? 'Ananun'", "caseData.patientName ?? 'Ananun'"),
    ('label="Tarik"', 'label="Tariq"'),
    ('label="Bzhishk"', 'label="Bzhishk"'),
    ('label="Bzhishkakan Nshumner"', 'label="Bzhishkakan Nshumner"'),
    # Header
    ('>← Verapernel<', '>← Verapernel<'),
    ('\n                            Depki Manramashner', '\n                            Depqi Manramashner'),
    ('Steghtzval {formatDate', 'Steghtzval {formatDate'),
    ('Avartval {formatDate', 'Avartval {formatDate'),
    # Report
    ('Hashvetvagrutyune Prastvel E', 'Hashvetvagrutyune Prastvel E'),
    ('Bernerkvel PDF format-ov', 'Bernerkvel PDF format-ov'),
    ("'✓ Bernerkvel E!'", "'✓ Bernerkvel E!'"),
    ("'⏳ Bernerkum...'", "'⏳ Bernerkum...'"),
    ("'⬇ Bernerknel PDF'", "'⬇ Bernerknel PDF'"),
    ("'📄 Bernerknel Hashvetvagrutyun'", "'📄 Bernerknel Hashvetvagrutyun'"),
    # Error
    ("setReportErr('Hashvetvagrutyune chi bernerkvi. Krknel krnkin.');",
     "setReportErr('Hashvetvagrutyune chi bernerkvi. Krknel krnkin.');"),
    ('Depe chi gtvavel', 'Depe chi gtvavel'),
    ("'Bolor Depkere'", "'Bolor Depkere'"),
]

for old, new in pairs:
    if old in c:
        c = c.replace(old, new)

open(path, 'w', encoding='utf-8').write(c)
print('CaseDetail step done - need real Armenian replacements next')
