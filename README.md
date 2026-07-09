# Bank Ürün Yönetimi

ASP.NET Core MVC + PostgreSQL ile ana ürün, alt ürün ve dönem bazlı bağlantıları yöneten küçük bir web uygulaması.

## Yeni Bilgisayarda Kurulum

Ön koşul:

- `.NET SDK` kurulu olmalı.
- PostgreSQL için üç seçenek var:
  - Docker varsa: `docker compose` ile PostgreSQL container.
  - Docker yoksa/admin yoksa: portable PostgreSQL ZIP.
  - PostgreSQL zaten kuruluysa: local servis.

Repo'yu aldıktan sonra:

```powershell
git clone https://github.com/ufukzkn/bank_urun.git
cd bank_urun
```

## Docker ile PostgreSQL + Uygulama

Bu yol PostgreSQL'i ve browser tabanlı pgAdmin'i Docker container olarak başlatır. Uygulama yine `dotnet run` ile lokal çalışır ve `localhost:5432` üzerinden container PostgreSQL'e bağlanır.

```powershell
dotnet tool restore
docker compose up -d
dotnet tool run dotnet-ef database update --project BankUrun.Web\BankUrun.Web.csproj --startup-project BankUrun.Web\BankUrun.Web.csproj
dotnet run --project BankUrun.Web\BankUrun.Web.csproj --urls http://localhost:5188
```

Uygulama: `http://localhost:5188`
pgAdmin: `http://127.0.0.1:5050`

Sayfalar:

- `http://localhost:5188/Products`
- `http://localhost:5188/Organization`
- `http://localhost:5188/Scores`

Docker PostgreSQL bağlantı bilgileri:

```text
Host=localhost
Port=5432
Database=bank_urun
Username=bank_urun
Password=bank_urun
```

pgAdmin genelde direkt açılır. Login ekranı gelirse:

```text
Email=admin@bankurun.com
Password=bank_urun
```

pgAdmin içinde `Bank Urun PostgreSQL` server'ı hazır gelir. Server şifresi sorarsa `bank_urun` yaz.

Docker ile mock veri yüklemek için:

```powershell
Get-Content scripts\seed-mock-data.sql | docker exec -i bank_urun_postgres psql -U bank_urun -d bank_urun
```

## Docker Olmadan Çalıştırma

Bu seçenek için bilgisayarda PostgreSQL kurulu ve servis olarak çalışıyor olmalı. Varsayılan connection string:

```text
Host=localhost;Port=5432;Database=bank_urun;Username=bank_urun;Password=bank_urun
```

PostgreSQL içinde kullanıcı ve database yoksa önce şunu çalıştır:

```powershell
psql -U postgres -d postgres -c "create user bank_urun with password 'bank_urun';" -c "create database bank_urun owner bank_urun;"
```

Sonra projeyi çalıştır:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project BankUrun.Web\BankUrun.Web.csproj --startup-project BankUrun.Web\BankUrun.Web.csproj
dotnet run --project BankUrun.Web\BankUrun.Web.csproj --urls http://localhost:5188
```

Tek komut halinde çalıştırmak istersen:

```powershell
dotnet tool restore; dotnet tool run dotnet-ef database update --project BankUrun.Web\BankUrun.Web.csproj --startup-project BankUrun.Web\BankUrun.Web.csproj; dotnet run --project BankUrun.Web\BankUrun.Web.csproj --urls http://localhost:5188
```

## Portable PostgreSQL ile Çalıştırma

Şirket bilgisayarında admin yetkisi yoksa Docker veya PostgreSQL installer çoğu zaman çalışmaz. Bu durumda PostgreSQL'in ZIP binary paketini kullanıcı klasöründen portable olarak çalıştırabilirsin.

1. PostgreSQL Windows x86-64 ZIP binaries paketini indir:
   - Resmi EDB sayfası: https://www.enterprisedb.com/download-postgresql-binaries
   - PostgreSQL 17.x Windows x86-64 ZIP dosyasını indir.

2. ZIP dosyası `Downloads` klasöründeyse repo klasöründe şu komutu çalıştır:

```powershell
$pgZip = Get-ChildItem "$env:USERPROFILE\Downloads\postgresql-17*-windows-x64-binaries.zip" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
powershell -ExecutionPolicy Bypass -File .\scripts\Run-WithPortablePostgres.ps1 -PgZipPath $pgZip.FullName -Seed
```

ZIP dosyasını daha önce açtıysan `-PgRoot` ile klasörü göster:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Run-WithPortablePostgres.ps1 -PgRoot "$env:USERPROFILE\Tools\postgresql"
```

Bu script şunları yapar:

- Portable PostgreSQL'i `.tools\postgres` altına çıkarır.
- Data klasörünü `.data\postgres` altında oluşturur.
- PostgreSQL'i servis kurmadan process olarak başlatır.
- `bank_urun` user/database oluşturur.
- EF migration uygular.
- İstersen `-Seed` ile mock veriyi yükler.
- Uygulamayı `http://localhost:5188` adresinde başlatır.

Portable PostgreSQL'i durdurmak için:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Stop-PortablePostgres.ps1
```

## Mock Veri

Docker ile:

```powershell
Get-Content scripts\seed-mock-data.sql | docker exec -i bank_urun_postgres psql -U bank_urun -d bank_urun
```

Docker olmadan mock veri yüklemek için:

```powershell
psql -U bank_urun -d bank_urun -f scripts\seed-mock-data.sql
```

## pgAdmin Bağlantısı

Browser üzerinden Docker pgAdmin kullanmak için:

```powershell
docker compose up -d pgadmin
```

Sonra şu adrese gir:

```text
http://127.0.0.1:5050
```

PgAdmin genelde direkt açılır. Login ekranı gelirse:

```text
Email: admin@bankurun.com
Password: bank_urun
```

Server hazır görünür: `Bank Urun PostgreSQL`. Şifre isterse PostgreSQL şifresi: `bank_urun`.

Kendi pgAdmin uygulamanda server eklerken:

- `Servers` üzerinde sağ tıkla, `Register` -> `Server...` seç.
- `General` sekmesinde `Name`: `Bank Urun Local`.
- `Connection` sekmesinde:
  - `Host name/address`: `localhost`
  - `Port`: `5432`
  - `Maintenance database`: `bank_urun`
  - `Username`: `bank_urun`
  - `Password`: `bank_urun`
  - `Save password`: açık olabilir.

Not: pgAdmin kendi bilgisayarında uygulama olarak çalışıyorsa host `localhost` olmalı. Sadece başka bir Docker container içinden bağlanırken host adı `postgres` olur.

## Veritabanı Şemasını Görüntüleme

pgAdmin browser içinde:

- `Servers` -> `Bank Urun PostgreSQL` -> `Databases` -> `bank_urun` -> `Schemas` -> `public` -> `Tables`
- Her tablo için `Columns`, `Constraints`, `Indexes` bölümlerinden yapıyı görebilirsin.
- Diagram için pgAdmin'de `Tools` -> `ERD Tool` kullanılabilir.

Terminalden hızlı tablo/kolon görünümü:

```powershell
docker exec -it bank_urun_postgres psql -U bank_urun -d bank_urun -c "\dt"
docker exec -it bank_urun_postgres psql -U bank_urun -d bank_urun -c "\d product_definitions"
docker exec -it bank_urun_postgres psql -U bank_urun -d bank_urun -c "\d main_product_instances"
docker exec -it bank_urun_postgres psql -U bank_urun -d bank_urun -c "\d sub_product_instances"
docker exec -it bank_urun_postgres psql -U bank_urun -d bank_urun -c "\d branch_product_scores"
```

Tüm şemayı SQL olarak dump etmek için:

```powershell
docker exec bank_urun_postgres pg_dump -U bank_urun -d bank_urun --schema-only
```

## Temel Kurallar

- Ana/alt ürün kod-ad tanımları `product_definitions` tablosunda tutulur.
- Ana ürünün yıl/dönem varlığı `main_product_instances` tablosunda tutulur.
- Alt ürünün ana ürün instance'ını beslemesi `sub_product_instances` tablosunda tutulur.
- Ürün adı veya kodu değişince tek tanım satırı güncellenir; dönem ve bağlantı satırları yeni bilgiyi join ile görür.
- Ürün kodları 2 karakterli alfanumeriktir.
- Ürün kodu `(product_type, code)` bazında benzersizdir.
- `main_product_instances.product_definition_type = 'Main'` ve composite foreign key ile yalnızca ana ürün tanımına bağlanabilir.
- `sub_product_instances.product_definition_type = 'Sub'` ve composite foreign key ile yalnızca alt ürün tanımına bağlanabilir.
- Aynı alt ürün birden fazla ana ürün dönemine bağlanabilir.
- Seçili instance silme sadece dönem/bağlantı satırını kaldırır; tüm tablodan silme tanım satırını ve ilişkilerini kaldırır.
- Pasifleştirme tanım satırını pasif yapar ve audit log yazar.
- Grup tanımları `group_definitions`, şubeler `branches` tablosunda tutulur.
- Şubeler tek bir gruba `branches.group_id` ile bağlanır.
- Grup segmenti `Karma`, `Kurumsal`, `Ticari`, `Kobi`, `Diger` değerlerinden biri olur.
- Şube performans puanları `branch_product_scores` tablosunda alt ürün instance'a bağlanır.
- Puan ve hedef negatif olamaz; HGO, gelişim ve büyüklük payları DB'de `0-1`, ekranda `0-100` yüzde formatındadır.
- Halkbank logosu resmi logo sayfasındaki JPG varlığından alınmıştır: https://www.halkbank.com.tr/tr/bankamiz/kurumsal-iletisim/logolarimiz
