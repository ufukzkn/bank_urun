# Bank Ürün Yönetimi

ASP.NET Core MVC + PostgreSQL ile ana ürün ve ana ürüne bağlı alt ürün kayıtlarını yöneten küçük bir web uygulaması.

## Çalıştırma

```powershell
dotnet tool restore
docker compose up -d
dotnet tool run dotnet-ef database update --project BankUrun.Web\BankUrun.Web.csproj --startup-project BankUrun.Web\BankUrun.Web.csproj
dotnet run --project BankUrun.Web\BankUrun.Web.csproj --urls http://localhost:5188
```

Uygulama: `http://localhost:5188`

## Mock Veri

```powershell
docker cp scripts\seed-mock-data.sql bank_urun_postgres:/tmp/seed-mock-data.sql
docker exec bank_urun_postgres psql -U bank_urun -d bank_urun -f /tmp/seed-mock-data.sql
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

- Ana tablo yapısı `main_products`, `sub_products` ve `audit_logs` şeklindedir.
- Ana ürün yıl/dönem bilgisini kendi üzerinde taşır.
- Alt ürün `main_product_id` foreign key'i ile ana ürüne bağlanır; alt ürünün yıl/dönemi bağlı olduğu ana üründen gelir.
- Ürün kodları 2 karakterli alfanumeriktir.
- Ana ürün kodları otomatik üretimde tekrar etmez.
- Alt ürün kodu aynı ana ürün altında tekrar etmez; farklı ana ürünlerin altında aynı alt ürün kodu kullanılabilir.
- Pasifleştirme veriyi saklar; kalıcı silme ilişkili kayıtları temizler ve audit log yazar.
