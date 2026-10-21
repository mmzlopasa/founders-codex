# Founders: The Long Rise — početni plan igre

**Status:** početna specifikacija  
**Ciljana platforma:** iOS  
**Prva sezona:** Prehistoric Civilization  
**Repo:** `mmzlopasa/founders-codex`

Repozitorij je prije početka bio prazan. Ovo je početni dokument rada, a ne nastavak starijeg plana.

## 1. Vizija

Igrač osniva naselje i vodi ga kroz povijesne sezone. Svaka sezona donosi novu civilizaciju, građevine, resurse, zadatke i vremenski ograničene evente. Prva sezona počinje u prapovijesti, s malom zajednicom koja razvija naselje i širi se kroz prirodan, velik krajolik.

Temelj igre je slobodna gradnja. Igrač može postaviti građevinu na bilo koji dio dostupnog terena, uključujući šumu ili područje prekriveno kamenjem. Nakon potvrde gradnje uklanjaju se stabla, stijene i dekoracija koji presijecaju tlocrt građevine. Sama lokacija i veličina mape ne ograničavaju gradnju kroz umjetne zabrane poput „ovo polje nije građevinsko”.

## 2. Smjer asseta

### Krajolik i priroda — Ultimate Nature – Starter

Asset Store listing trenutno navodi besplatan paket od 163,9 MB, verzije 1.3.0, objavljen 6. kolovoza 2026., kompatibilan s URP-om u Unityju 6.0.5.3f1. Kategorije i ključne riječi uključuju landscape/terrain, vodu, šume, stijene, vegetaciju i modularne okoliše. Izvor: [Unity Asset Store — Ultimate Nature – Starter](https://assetstore.unity.com/packages/3d/environments/landscapes/ultimate-nature-starter-176906).

Kada se sadržaj uveze u projekt, pregledati sve njegove prefabove, materijale, teksture, modele, varijante i demo scene. Napraviti inventar asseta i koristiti dostupne elemente kroz proceduralnu distribuciju umjesto da se proizvoljno zamjenjuju drugim paketima. Svaki prefabski element treba ostati zamjenjiv kroz katalog, da se mogu podesiti gustoća, LOD i izvedba.

### Prapovijesni sadržaj — EXPLORER – Stone Age

Koristiti kao smjer i, kada su datoteke dostupne u projektu, izvor za likove, odjeću, animacije, građevine, stijene, rekvizite, vegetaciju, čestice i skyboxove. Asset Store listing: [Unity Asset Store — EXPLORER – Stone Age](https://assetstore.unity.com/packages/3d/environments/historic/explorer-stone-age-150867).

Uvezene modele i animacije prvo katalogizirati u Unity Editoru. Ne pretpostavljati putanje ili nazive prefaba prije pregleda sadržaja stvarnog Unity paketa. Ne kopirati Asset Store datoteke u javni repozitorij dok se ne potvrde uvjeti distribucije licence; držati izvorne assete u Unity projektu ili u odobrenom privatnom spremištu.

## 3. Svijet i mapa

Mapa mora djelovati kao vrlo velik, koherentan svijet, a ne kao skup malih odvojenih arena.

- Izraditi svijet od susjednih terenskih segmenata/chunkova s kontinuitetom visine, vode i granica bioma.
- Učitavati i rasterećivati udaljene dijelove prema položaju kamere, uz nižu razinu detalja i rjeđe objekte na daljinu. Početi s ograničenim prototipom, zatim mjeriti na stvarnom iOS uređaju prije određivanja konačne veličine.
- Oblikovati raznovrsne prirodne cjeline: otvorene ravnice i livade, šumovite predjele, stjenovite uzvisine, vodene površine i prijelazne zone. Točne vrste i oblici moraju odgovarati dostupnim assetima nakon inventure.
- Generirati teren iz determinističkog seed-a kako bi se raspored mogao ponoviti i spremiti u podatke naselja.
- Prirodne objekte raspoređivati procedurano po biomeu, nadmorskoj visini, nagibu, udaljenosti od vode i drugim pravilima okoliša.
- Ostaviti prostor za širenje naselja, ali ne blokirati gradnju iz razloga koji su samo vizualna dekoracija.

### Gradnja i uklanjanje prirodnih objekata

Svaka građevina ima definirani tlocrt i pravilo visine/poravnanja. U previewu se prikazuje budući tlocrt i objekti koji će biti uklonjeni. Nakon potvrde, sustav uklanja ili sprema identifikatore objekata koji se preklapaju s tlocrtom, pa se stanje pravilno vraća nakon spremanja i ponovnog učitavanja naselja. Resursi dobiveni čišćenjem mogu se dodati u inventar ako to podržavaju pravila sezone.

Sustav ne smije temeljiti mogućnost gradnje na tome je li tlo prekriveno drvom, grmljem, travom ili kamenjem. Terenska geometrija i ozbiljne visinske razlike mogu zahtijevati poravnanje ili prilagodbu same građevine, ali ne smiju pretvoriti vegetaciju u zabranu gradnje.

## 4. Sezone i eventi

Podaci sezone trebaju biti odvojeni od općeg sustava igre. Svaka sezona definira svoj paket građevina, resursa, stanovnika, zadataka, UI oznaka, otključavanja i vizualnih pravila. Eventi su vremenski ograničeni podaci sa svojim ciljevima i nagradama. Prva sezona:

1. Početak središta naselja i osnovnih potreba.
2. Prikupljanje hrane, drva i kamena.
3. Gradnja skloništa, proizvodnih mjesta i društvenih/sakralnih struktura.
4. Istraživanje i širenje naselja po velikoj mapi.
5. Sezonski događaji koji dodaju kratke zadatke i kozmetičke ili razvojne nagrade.

Vrijeme trajanja sezona i model ekonomije još nisu zadani; ostaviti ih konfigurabilnima.

## 5. Mobilno upravljanje i UI

- Dizajn za dodir: zumiranje, pomicanje kamere, odabir građevine, pregled prije postavljanja i potvrda gradnje.
- Izbjegavati sitne gumbe i oslanjanje na hover.
- Kamera treba omogućiti pregled naselja i putovanje kroz već otkriveni svijet bez trzanja.
- UI treba prikazivati osnovne resurse, gradnju, napredovanje sezone, zadatke i evente.
- Sačuvati jednostavno spremanje naselja i verzionirati podatkovni format radi budućih nadogradnji.

## 6. Tehnička struktura

Unity URP iOS projekt. Odvojiti najmanje ove sustave: world/terrain streaming, proceduralni dekor, građevinski katalog, gradnju i čišćenje, kameru/dodir, spremanje svijeta, sezonu/evente i UI. Generiranje velikih količina vegetacije treba koristiti batching/instancing gdje je moguće, culling i razine detalja.

Izvorne assete ne prepakirati ili objaviti bez pregleda licence i konfiguracije privatnosti repozitorija. Ne verzionirati Unity cache, Library, Temp ni korisničke postavke.

## 7. Put do izdanja

1. **Repo i inventura:** postaviti Unity strukturu i CI osnovu; importirati ili konfigurirati oba asseta te izlistati stvarne assete i kompatibilnost.
2. **Landscape prototype:** generirati reprezentativni mali dio svijeta s assetima, chunk granicama, vodom, biomi i kamerom; potvrditi vizualni stil.
3. **Map streaming:** proširiti prototip u veliku povezanu mapu, podesiti prikaz na iOS-u i sačuvati seed/podatke.
4. **Gradnja:** katalog građevina, preview, rotacija, slobodno postavljanje, čišćenje stabala/kamenja i spremanje.
5. **Season 1 MVP:** osnovni resursi, naseljenici, građevine, zadaci i jedan event.
6. **Sadržaj i dorada:** likovi i animacije, više građevina i okolišnih detalja, UI, zvuk, tutorial i optimizacija.
7. **iOS release:** izgraditi i potpisati `.ipa` u kontroliranom iOS build procesu, provjeriti instalaciju preko Feather te objaviti datoteku u GitHub Releaseu.

Za potpisani iOS build bit će potrebna Apple potpisna konfiguracija. Release se ne smatra gotovim dok `.ipa` nije stvarno izgrađen, potpisan i postavljen u GitHub Releases.

## 8. Otvorene tehničke odluke

- Točan Unity editor patch version potvrditi nakon što se pregleda projekt i oba asset paketa.
- Konačnu veličinu i rezoluciju mape odabrati iz mjerenja na iOS uređaju.
- Provjeriti dozvoljeni način distribucije Asset Store datoteka prije nego što ih uključimo u repo ili build workflow.
- Odabrati minimalni set potrebnih asset prefaba tek nakon uvoza i automatskog inventara paketa.