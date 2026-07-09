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

Bu yol PostgreSQL'i Docker container olarak başlatır. Uygulama yine `dotnet run` ile lokal çalışır ve `localhost:5432` üzerinden container PostgreSQL'e bağlanır.

```powershell
dotnet tool restore
docker compose up -d postgres
dotnet tool run dotnet-ef database update --project BankUrun.Web\BankUrun.Web.csproj --startup-project BankUrun.Web\BankUrun.Web.csproj
dotnet run --project BankUrun.Web\BankUrun.Web.csproj --urls http://localhost:5188
```

Uygulama: `http://localhost:5188`

Docker PostgreSQL bağlantı bilgileri:

```text
Host=localhost
Port=5432
Database=bank_urun
Username=bank_urun
Password=bank_urun
```

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

## Temel Kurallar

- Ana/alt ürün kod-ad tanımları `product_definitions` tablosunda tutulur.
- Ana ürünün yıl/dönem varlığı `main_product_instances` tablosunda tutulur.
- Alt ürünün ana ürün instance'ını beslemesi `sub_product_instances` tablosunda tutulur.
- Ürün adı veya kodu değişince tek tanım satırı güncellenir; dönem ve bağlantı satırları yeni bilgiyi join ile görür.
- Ürün kodları 2 karakterli alfanumeriktir.
- Ürün kodu `(product_type, code)` bazında benzersizdir.
- `main_product_instances.main_product_type = 'Main'` ve composite foreign key ile yalnızca ana ürün tanımına bağlanabilir.
- `sub_product_instances.sub_product_type = 'Sub'` ve composite foreign key ile yalnızca alt ürün tanımına bağlanabilir.
- Aynı alt ürün birden fazla ana ürün dönemine bağlanabilir.
- Seçili instance silme sadece dönem/bağlantı satırını kaldırır; tüm tablodan silme tanım satırını ve ilişkilerini kaldırır.
- Pasifleştirme tanım satırını pasif yapar ve audit log yazar.
