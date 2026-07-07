# Bank Ürün Yönetimi

ASP.NET Core MVC + PostgreSQL ile ana ürün, alt ürün ve yıl/dönem bazlı ürün bağlantılarını yöneten küçük bir web uygulaması.

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

- Host: `localhost`
- Port: `5432`
- Database: `bank_urun`
- Username: `bank_urun`
- Password: `bank_urun`

## Temel Kurallar

- Ürün kodları 2 karakterli alfanumerik ve ürün tipi içinde benzersizdir.
- Ana ürün ve alt ürün adları canonical tabloda tutulur; isim değişince dönem görünümleri yeni adı gösterir.
- Alt ürün aynı dönemde farklı ana ürünlere bağlanabilir.
- Alt ürün farklı yıl/dönemlerde tekrar listelenebilir.
- Pasifleştirme veriyi saklar; kalıcı silme ilişkili kayıtları temizler ve audit log yazar.
