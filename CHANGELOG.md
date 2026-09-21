# Changelog

All notable player-facing changes to **Seljuk Empire: Sword of Islam** are listed here, most recent
first. This is the short version — full technical detail (root causes, decompile-verified engine
behavior, every fix's history) lives in [`graphify-out/graph.md`](graphify-out/graph.md) for
contributors.

[ TÜRKÇE ] için aşağı kaydırın.

## v1.9.0 — 2026-09-21

- **Three new languages: Italian, Polish, and Brazilian Portuguese.** All ~1,900 of the mod's strings —
  troops, lords, dialogue, Encyclopedia entries, UI — are translated, for 11 languages in total. These are
  fresh translations, so wording fixes from native speakers are very welcome.
- **Your companions now have personalities.** Each of the 25 recruitable companions — the 14 named
  historical figures and the 11 Seljuk wanderers — has three distinct greetings that rotate whenever you
  talk to them again after their opening introduction, so a returning conversation no longer repeats one
  canned line. (Translated in English, Turkish, Italian, Polish, and Portuguese; the other six languages
  show the English text for these lines until they are translated.)
- **A richer Seljuk Encyclopedia entry.** The culture's in-game history now runs from Tughril Beg's march
  on Baghdad, through Alp Arslan at Manzikert, to Malik-Shah's iqta cavalry and Nizamiye madrasas — and the
  succession crises that always followed.
- For contributors: a public architecture wiki (English + Turkish) is now online at
  https://keremtrgl.github.io/OttomanJanissariesAndTurkicHeroes/, and the language-sync check now covers the three new languages.

## v1.8.4 — 2026-09-07

- Byzantine lords across the whole empire — north, south, and west — now all have their own
  historical greeting lines (15 new ones this update, in all 8 languages).
- Added a check that catches a settlement showing the wrong culture's flavor after changing hands
  (the "captured city still looks foreign" bug class).

## v1.8.3

- 11 of the mod's 13 custom Seljuk/Turkic clan banners are now actually displayed on their clans'
  flags — previously they existed but weren't assigned to anyone, so you'd never see them in-game.
- 7 new Abbasid and Georgian lords got their own greeting lines.
- Added a one-command check + a tactical AI performance benchmark for contributors.

## v1.8.2 — Ansiklopedi, turnuva ödülleri, diyalog düzeltmesi

- Every one of the 8 kingdoms now has its own written history in the in-game Encyclopedia — previously
  only the Seljuks did, everyone else showed the vanilla Calradian entry.
- All 7 rival kingdoms now award their own unique tournament champion prize, matching what Seljuk
  towns already gave.
- Fixed 117 lines of custom lord/companion dialogue that were quietly skipping Native's own follow-up
  line after a greeting — cosmetic, but noticeable once you knew to look for it.

## v1.8.1 — Front-facing charge reaction & terrain mastery

- Infantry now snaps into a defensive brace the instant an incoming cavalry charge is about to hit
  its front, instead of only reacting after taking casualties.
- The army's hilltop staging position now prefers the engine's own terrain-slope search for smarter,
  more natural defensive ground.

## v1.8.0 — Reactive infantry & archer AI

- Infantry only raises a shield wall when actually facing cavalry or missile fire — not automatically
  every battle — and falls back in a controlled way from a losing advance.
- Horse archers and foot archers hold range and keep shooting instead of charging into melee the
  moment they run low on ammunition.

## v1.7.9 — Reactive cavalry AI

- Cavalry no longer blindly charges a braced enemy spear/shield line. It waits for a real opening,
  and pulls back if a charge is clearly going badly instead of fighting to the last horse.

## v1.7.5 – v1.7.8 — Recruitment, localization, and companion fixes

- Tavern mercenaries, caravan guards, settlement patrols, siege militia, and loyalty-gift troops now
  come from each kingdom's own culture instead of a generic Native roster (28 new templates across
  8 kingdoms).
- All 11 generic Seljuk wanderer companions got their own backstories, fully translated into all 8
  supported languages.
- Fixed a rebel/militia party template leak that occasionally spawned Native troops instead of this
  mod's own.

## v1.7.0 – v1.7.4 — Foundational content pass

- Added full character-creation backstories for 3 rival kingdoms, 14 real-historical-figure tavern
  companions (2 per rival culture), and renamed 126 villages/castles to their real historical names.
- Fixed every rival kingdom's troop tree missing leg/hand armor at higher tiers (a real, if quiet,
  balance gap versus the Seljuk tree).
- Fixed mounted troops being able to upgrade into heavier cavalry without actually owning a horse.

## v1.6.1 – v1.6.7 — Early stability fixes

- Fixed a hard crash on the culture-selection screen (a stray `<cultural_feats>` block Bannerlord
  expects to be registered in C#, not just declared in XML).
- Fixed a missing texture on the culture-selection screen.
- Swept 16 categories of "ERROR: Text with id ... doesn't exist!" placeholder text that could appear
  in dialogue before the mod's localization was complete.

---

## [ TÜRKÇE ]

## v1.9.0 — 2026-09-21

- **Üç yeni dil: İtalyanca, Lehçe ve Brezilya Portekizcesi.** Modun yaklaşık 1.900 metninin tamamı —
  birlikler, lordlar, diyaloglar, Ansiklopedi maddeleri, arayüz — çevrildi; toplam 11 dil. Çeviriler yeni
  olduğundan, ana dili konuşanların düzeltmeleri çok değerli.
- **Yoldaşlarınızın artık kişilikleri var.** 25 işe alınabilir yoldaşın her biri — 14 adlı tarihi figür ve
  11 Selçuklu gezgini — ilk tanışma konuşmasından sonra her sohbetinizde dönüşümlü olarak değişen üç ayrı
  karşılama repliğine sahip; tekrar eden konuşmalar artık hep aynı kalıp cümleyi söylemiyor. (Türkçe,
  İngilizce, İtalyanca, Lehçe ve Portekizce çevrildi; diğer altı dil bu satırlar çevrilene kadar İngilizce
  metni gösterir.)
- **Daha zengin bir Selçuklu Ansiklopedi maddesi.** Kültürün oyun içi tarihi artık Tuğrul Bey'in Bağdat
  seferinden Malazgirt'te Alp Arslan'a, Melikşah'ın ikta süvarileri ve Nizamiye medreselerine, ardından gelen
  veraset krizlerine kadar uzanıyor.
- Katkıcılar için: herkese açık, İngilizce + Türkçe mimari viki artık yayında: https://keremtrgl.github.io/OttomanJanissariesAndTurkicHeroes/ —
  dil-senkron kontrolü de üç yeni dili kapsıyor.

## v1.8.4 — 2026-09-07

- İmparatorluğun her yerindeki Bizans lordları — kuzey, güney ve batı — artık kendi tarihi karşılama
  repliklerine sahip (bu güncellemede 15 yeni satır, 8 dilin hepsinde).
- Bir yerleşkenin el değiştirdikten sonra yanlış kültürün görünümünü göstermesini yakalayan yeni bir
  kontrol eklendi.

## v1.8.3

- Modun 13 özel Selçuklu/Türk klan tamgasından 11'i artık gerçekten klanlarının bayrağında
  görünüyor — önceden vardılar ama hiçbir klana atanmamışlardı.
- 7 yeni Abbasi ve Gürcü lorduna kendi karşılama replikleri eklendi.

## v1.8.2 — Ansiklopedi, turnuva ödülleri, diyalog düzeltmesi

- 8 krallığın hepsi artık oyun içi Ansiklopedi'de kendi tarihine sahip — önceden sadece Selçuklu
  vardı, geri kalanı vanilya Calradia metnini gösteriyordu.
- 7 rakip krallığın hepsi artık kendi benzersiz turnuva şampiyonu ödülünü veriyor.
- 117 satırlık özel lord/yoldaş diyaloğunun, karşılamadan sonra Native'in kendi devam repliğini
  sessizce atladığı bir hata düzeltildi.

## v1.8.1 — Cepheden şarj tepkisi & arazi ustalığı

- Piyade artık gelen bir süvari şarjı cepheye çarpmak üzereyken anında savunma pozisyonuna geçiyor.
- Ordunun tepe konuşlanma noktası artık motorun kendi arazi-eğim aramasını tercih ediyor.

## v1.8.0 — Reaktif piyade & okçu AI

- Piyade artık sadece gerçekten süvari veya ok tehdidi altındayken kalkan duvarı kuruyor.
- Atlı ve yaya okçular artık ok bitince göğüs göğüse dalmak yerine mesafeyi koruyup atmaya devam
  ediyor.

## v1.7.9 — Reaktif süvari AI

- Süvari artık mızrak/kalkan duvarı kurmuş bir düşmana körce saldırmıyor — gerçek bir açıklık
  bekliyor, kötü giden bir hücumu son ata kadar savaşmak yerine kontrollü geri çekiliyor.

## v1.7.5 – v1.7.8 — Asker alımı, lokalizasyon ve companion düzeltmeleri

- Han paralı askerleri, kervan muhafızları, yerleşke devriyeleri, kuşatma milisi ve bağlılık hediyesi
  askerleri artık her krallığın kendi kültüründen geliyor.
- 11 jenerik Selçuklu gezgin companion'ının hepsi kendi geçmiş hikayesine kavuştu, 8 dilin hepsine
  çevrildi.

## v1.7.0 – v1.7.4 — Temel içerik turu

- 3 rakip krallığa tam karakter yaratma özgeçmişi, 14 gerçek tarihi figürlü meyhane companion'ı
  eklendi, 126 köy/kale gerçek tarihi ismine kavuştu.
- Her rakip krallığın asker ağacındaki eksik bacak/eldiven zırhı dolduruldu.

## v1.6.1 – v1.6.7 — Erken kararlılık düzeltmeleri

- Kültür seçim ekranındaki sert çökme düzeltildi.
- Kültür seçim ekranındaki eksik doku düzeltildi.
- Lokalizasyon tamamlanmadan görünebilecek 16 kategori "ERROR: Text with id..." yer tutucu metni
  temizlendi.
