# Seljuk Empire: Sword of Islam — Mod Mimari & Bağlantı Grafiği (Architecture Graph)

Bu doküman, **"Seljuk Empire: Sword of Islam"** total conversion modundaki tüm modüllerin, C# çok doktrinli taktik yapay zeka motorunun, BattlePerformanceOptimizer FPS sisteminin, **Selçuklu Kervan Devlet Sigortası & İpek Yolu Kâr Ortaklığı sisteminin**, krallık, klan, lord, yerleşke, birlik ağaçları, karakter yaratma özgeçmişleri, eşyalar, politikalar ve diller arasındaki ilişkileri detaylandırmaktadır.

---

## 🏗️ 1. Genel Modül & Dosya Bağımlılık Haritası (Master Engine Architecture)

```mermaid
graph TD
    SM["SubModule.xml<br/>(Master Manifest)"] --> CSHARP["SeljukTactics.dll<br/>(C# Engine Core)"]
    SM --> XML_CC["character_creation.xml<br/>(5 Aşamalı Özgeçmiş)"]
    SM --> XML_F["factions.xml<br/>(Krallık & 11 Klan)"]
    SM --> XML_S["settlements.xml<br/>(10 Mülk + 23 Köy)"]
    SM --> XML_L["lords.xml<br/>(24 Lider + 3 Yoldaş)"]
    SM --> XML_H["heroes.xml<br/>(Soy Ağacı & İlişkiler)"]
    SM --> XML_T["troops.xml<br/>(3 Birlik Ağacı)"]
    SM --> XML_P["party_templates.xml<br/>(14 Ordu Şablonu)"]
    SM --> XML_POL["policies.xml<br/>(7 Devlet Politikası)"]
    SM --> XML_I["items.xml<br/>(12 Efsanevi Yadigar)"]
    SM --> XML_B["banner_icons.xml<br/>(11 Selçuklu Tamgası)"]
    SM --> XML_LANG["Languages/<br/>(8 Dil Desteği)"]

    CSHARP --> TACTIC_AI["TuranTacticMissionBehavior<br/>(4 Doktrinli Taktik FSM)"]
    CSHARP --> TACTIC_MATH["TacticalFormationsHelper<br/>(Sıfır-GC Tepe & Sınır Güvenliği)"]
    CSHARP --> PERF_OPT["BattlePerformanceOptimizer<br/>(FPS & Frametime Dengeleyici)"]
    CSHARP --> ECON_INS["SeljukCaravanInsuranceBehavior<br/>(Devlet Sigortası & İpek Yolu Fonu)"]
```

---

## 🪙 2. Selçuklu Kervan Devlet Sigortası & İpek Yolu Kâr Ortaklığı (Economy Engine)

```mermaid
graph LR
    subgraph "1. Devlet Kervan Sigortası"
        P1["Kervan Sigortası Poliçesi Al<br/>(1,500 Dinar Tek Seferlik)"] --> P2["Kervan Haydutlarca Vurulursa"]
        P2 --> P3["Sultanlık Hazinesi 18,500 Dinar Tam Tazminat Öder!"]
    end

    subgraph "2. İpek Yolu Kervansaray Fonu"
        I1["Şehir Kervansarayına 10,000 Dinar Sermaye Yatır"] --> I2["Haftalık Ticaret Refahı Çarpanı (%4.5 ROI)"]
        I2 --> I3["Her Hafta Düzenli Pasif Altın Temettüsü Tahsil Edilir"]
    end
```

---

## ⚡ 3. Savaş Alanı FPS & Ragdoll Optimizasyon Motoru (BattleOptimizer)

```mermaid
graph LR
    subgraph "1. 2D Spatial Hash Grid"
        G1["Atlı & Yaya Okçular"] --> G2["35m Hücreli Uzamsal Izgara"]
        G2 --> G3["O(1) Anında En Yakın Düşman Tespiti<br/>(İşlemci Yükü -%85)"]
    end

    subgraph "2. Ragdoll Fizik Bütçe Yöneticisi"
        R1["500+ Asker Çarpışması"] --> R2["Aktif Ragdoll Sayısı Max 32 İle Sınırlandırılır"]
        R2 --> R3["Hareketsiz Cesetler Otomatik Uykuya Alınır<br/>(PhysX Çarpışma Hesaplaması Sıfırlanır)"]
    end

    subgraph "3. Mesafe Tabanlı AI Kademelendirme (LOD)"
        L1["Kameradan >140m Uzaktaki Birlikler"] --> L2["Gereksiz Raycast Sorguları Kırpılır<br/>(Stutter ve Mikro-Donmalar Engellenir)"]
    end
```

---

## 🏹 4. Çok Doktrinli Selçuklu Taktik Yapay Zeka Motoru (Multi-Doctrine AI)

```mermaid
graph TD
    START["Muharebe Başlangıcı"] --> EVAL["Ordu & Arazi Analizi"]
    
    EVAL -->|Süvari & Atlı Okçu >= %30| D1["1. DOKTRİN: Kurt Kapanı & Hilal Taktiği<br/>(Sahte Geri Çekilme + Çift Kanat Pusu)"]
    EVAL -->|Piyade >= %45| D2["2. DOKTRİN: Nizamiye Kalkan Duvarı<br/>(Yüksek Tepe Savunması + Mızrak Seddi)"]
    EVAL -->|Düşman >= 1.8x Sayıca Fazla| D3["3. DOKTRİN: Yüksek Tepe Karşı Pususu<br/>(Stratejik Tepe Kilitleme + Çekiç-Örs)"]
    EVAL -->|Dengeli Ordu| D4["4. DOKTRİN: Bozkır Çapraz Ateş Çemberi<br/>(Bileşik Yaylım Ateşi + Yandan Kuşatma)"]

    D1 --> PHASE_1["Aşama 1: Atlı Okçu Tacizi & Yemleme"]
    PHASE_1 --> PHASE_2["Aşama 2: Sahte Çekilme (Feigned Retreat)"]
    PHASE_2 --> PHASE_3["Aşama 3: İki Kanattan Hassa Süvari Baskını"]
    PHASE_3 --> PHASE_4["Aşama 4: Topyekûn Çekiç & Örs İmhası"]
```

---

## 👤 5. Selçuklu Karakter Yaratma Özgeçmiş Aşamaları

```mermaid
graph LR
    subgraph "1. Soy ve Aile Kökeni"
        H1["Oğuz Boyu Beyzadesi"]
        H2["Nizamiye Müderrisi Evladı"]
        H3["Ahi Demir Ustası Çırağı"]
        H4["Sultan Hassa Gulamı Soyu"]
        H5["Uç Boyu Türkmen Göçeri"]
    end

    subgraph "2. Çocukluk Çağı"
        C1["Bozkırda At Üstünde"]
        C2["İkta Kışlasında Güreş & Kılıç"]
        C3["İpek Yolu Yıldızları"]
        C4["Ahi Ocağında Körük"]
    end

    subgraph "3. Gençlik & Tahsil"
        Y1["Nizamiye Medresesi (İlim)"]
        Y2["Akıncı Çerisi (Gaza)"]
        Y3["Subaşı Muhafızı (Asayiş)"]
        Y4["Kervan Muhafızlığı"]
    end

    subgraph "4. İlk Meslek & Hizmet"
        CR1["Hassa Gulam Kıtası"]
        CR2["Danişmend Uç Alpi"]
        CR3["Ahi Yiğitbaşısı"]
        CR4["Çaka Bey Deniz Akıncısı"]
    end

    subgraph "5. Nam & Yiğitlik"
        D1["Kuşatmayı Yarma"]
        D2["Düşman Sancağını Devirme"]
        D3["Mazlumu Koruma"]
    end
```

---

## 👑 6. Krallık, Beylikler ve Tarihi Liderler Hiyerarşisi

```mermaid
graph TD
    KS["Büyük Selçuklu Devleti<br/>(Kingdom.kingdom_seljuks)"] --> CL1["Âl-i Selçuk (T6)<br/>Sultan Alp Arslan"]
    KS --> CL2["Nizamiye Vezirler Divanı (T5)<br/>Hâce Nizamülmülk"]
    KS --> CL3["Danişmendliler (T5)<br/>Melik Danişmend Gazi"]
    KS --> CL4["Artuklular (T5)<br/>Artuk Bey &amp; İlgazi"]
    KS --> CL5["Mengücekliler (T4)<br/>Mengücek Gazi"]
    KS --> CL6["Saltuklular (T4)<br/>Emir Saltuk"]
    KS --> CL7["Çaka Beyliği (T4)<br/>Çaka Bey"]
    KS --> CL8["Ahlatşahlar (T4)<br/>Sökmen el-Kutbî"]
    KS --> CL9["Karamanoğulları (T4)<br/>Kerimüddin Karaman Bey"]
    KS --> CL10["Kayı Boyu (T3)<br/>Ertuğrul Gazi &amp; Hanedan"]
    KS --> CL11["Ahi Evran Ocağı (T3)<br/>Ahi Evran"]
```

---

## 🗺️ 7. Şehirler, Kaleler ve Bağlı Köylerin Mülkiyet Dağılımı

| Yerleşke Türü | Yerleşke Adı | Sahibi Olan Klan | Bağlı Köyler & Özel Üretim |
| :--- | :--- | :--- | :--- |
| **Şehir (Town)** | **Konya (town_K1)** | Âl-i Selçuk (Alp Arslan) | Sille (Tahıl), Akşehir (Zeytin), Ladik (Koyun) |
| **Şehir (Town)** | **Sivas (town_K4)** | Âl-i Selçuk (Melikşah) | Zara (Demir), Kangal (At), Yıldızeli (Tahıl) |
| **Şehir (Town)** | **Kayseriyye (town_ES4)** | Danişmendliler (Danişmend) | Develi (Deri), Erciyes Ovası (Kürk), Talas (İpek) |
| **Şehir (Town)** | **Diyarbekir (town_ES2)** | Artuklular (Artuk Bey) | Ergani (Bakır), Silvan (İpek), Bismil (Pamuk) |
| **Şehir (Town)** | **Erzurum (town_K6)** | Saltuklular (Emir Saltuk) | Pasinler (Savaş Atı), Tortum (Meyve), Oltu (Mücevher) |
| **Şehir (Town)** | **Alâiye (town_A4)** | Çaka Beyliği (Çaka Bey) | Manavgat (Zeytinyağı), Gazipaşa (Tuz/Balık), Anamur (Kereste) |
| **Kale (Castle)** | **Divriği Kalesi (castle_K2)**| Mengücekliler (Mengücek) | Çetinkaya Madeni (Çelik/Demir), Kemaliye (Üzüm) |
| **Kale (Castle)** | **Ahlat Kalesi (castle_K5)** | Ahlatşahlar (Sökmen) | Erciş (Balık), Adilcevaz (Ceviz) |
| **Kale (Castle)** | **Malazgirt Kalesi (castle_K1)**| Ahlatşahlar (Sökmen) | Bulanık (Savaş Atı), Patnos (Tahıl) |
| **Kale (Castle)** | **Korykos Kalesi (castle_A8)**| Çaka Beyliği (Çaka Bey) | Silifke (Zeytin), Erdemli (Tuz) |

---

## 🌍 8. Rakip Krallıklar Hiyerarşisi (Native Kingdom → Tarihi Devlet Dönüşümü)

Bu bölüm, Native'in 6 orijinal krallığının bu modda gerçek tarihi devletlere dönüştürülmesini ve
bu oturumda eklenen yeni içerikleri özetler.

```mermaid
graph TD
    N1["Kingdom.empire_s<br/>(Native: Southern Empire)"] --> B1["🏛️ Bizans İmparatorluğu<br/>İmparator Romanos IV Diogenes"]
    N2["Kingdom.aserai"] --> B2["🏛️ Abbasi Halifeliği<br/>Halife El-Kaim bi-Emrillah"]
    N3["Kingdom.sturgia"] --> B3["🏛️ Gürcistan Krallığı<br/>Kral IV. David (Kurucu)"]
    N4["Kingdom.vlandia"] --> B4["🏛️ Haçlı Devletleri (Antakya)<br/>Bohemond of Taranto"]
    N5["Kingdom.battania"] --> B5["🏛️ Kilikya Ermeni Prensliği<br/>Ruben I (Kurucu)"]
    N6["Kingdom.khuzait"] --> B6["🏛️ Karahanlı Devleti<br/>Şems el-Mülk Nasr"]
    N7["Kingdom.empire_w<br/>(Native: Western Empire - bu oturumda canlandırıldı)"] --> B7["🏛️ Konstantinopolis Latin İmparatorluğu<br/>İmparator I. Henri (Flandre'li)"]

    B7 --> LC1["Flandre Hanesi (clan_empire_west_2)<br/>Edirne + Amfipolis + Midilli"]
    B7 --> LC2["Sanudo Hanesi (clan_empire_west_7)<br/>Naksos - Dük Marco Sanudo"]
```

**Bu oturumda yapılan toprak transferleri:**
- Caleus Kalesi + köyleri: Haçlı Devletleri → Kilikya Ermeni Prensliği (kültür güncellendi)
- Amasya (+3 köy): Bizans → Selçuklu (Danişmendliler)
- Dorylaeum/Eskişehir (+2 köy): Bizans → Selçuklu (Kayı Boyu)
- Midilli (Lesbos): House Kontostephanos → Flandre Hanesi (settlement-level owner override)

**Latin İmparatorluğu'nun kendine özgü 20 birimli asker ağacı** (`latin_empire_custom_troops.xml`,
Culture.empire, 6 kademe): Latin Levy → 4 dal (Frenk Piyadesi/Cenevizli Arbaletçi/Ulah Atlısı/
Silahtar) → ... → Gasmoulos Muhafızı (piyade), Seçkin Cenevizli Arbaletçi (menzilli), Rumeli Baronu
(ağır süvari) - Haçlı Devletleri'nin kendi ağacından tamamen farklı silah/zırh/at seçimleriyle.

**Yeni C# alt sistemleri:**
- `LatinEmpireRecruitmentBehavior` - empire_w/empire_s'in paylaştığı Culture.empire nedeniyle
  Latin İmparatorluğu yerleşkelerinin varsayılan Bizans askeri yerine kendi lat2_ ağacını
  sunmasını sağlar (bkz. `SeljukRecruitmentBehavior` ile aynı desen).
- `NewKingdomsDialogueBehavior` - Haçlı (7), Ermeni (7), Karahanlı (9), Latin İmparatorluğu (2) ve
  Bizans Batı (7) olmak üzere 32 tarihi lorda özel, gerçek tarihe dayalı 70 satırlık diyalog
  ekler (3 varyantlı tekrar-önleme mekanizması `SeljukDialogueBehavior` ile aynı).
