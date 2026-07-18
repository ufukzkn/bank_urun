# Şube, Portföy ve Ürün Performans Yönetimi

ASP.NET Core MVC ve PostgreSQL ile şube, portföy, ürün gamı ve ana ürün performansını yıl/yarıyıl bağlamında yöneten örnek iç operasyon uygulaması.

Uygulamanın varsayılan ekranı salt okunur `/Performance` dashboard'udur. Şube, şube–ürün, tüm şubelerden toplanan ana ürün ve portföy sonuçları dört ayrı görünümde karşılaştırılır. Hedefler portföy + ana ürün seviyesinde, batch gerçekleşmeleri portföy + stabil alt ürün seviyesinde tutulur. Aynı alt ürün birden fazla ana ürünü beslediğinde tek ham gerçekleşme kaydı her ana ürüne tam katkı verir. Aylık kırılımlar yalnız ilgili detay düğmesine basıldığında yüklenir; dönem sonucu ve sıralamalar saklanmaz, güncel girdilerden hesaplanır.

## Docker ile Tek Komut Kurulum

Gereken tek ön koşul Docker Desktop veya Docker Engine + Compose eklentisidir.

```powershell
git clone https://github.com/ufukzkn/bank_urun.git
cd bank_urun
docker compose up -d --build
```

Bu komut PostgreSQL 17, web uygulaması ve browser pgAdmin'i başlatır. Web uygulaması açılırken EF Core migration'larını kendisi uygular; yalnız ilk kurulumda mock verisini yükler. Bunun için ayrıca çalışan `migrate` veya `seed` container'ı bulunmaz.

- Performans: `http://localhost:5188/Performance`
- Parametreler: `http://localhost:5188/Parameters`
- pgAdmin: `http://localhost:5050`
- PostgreSQL: `localhost:5432`

Servisleri ve logları yönetmek için:

```powershell
docker compose logs -f web
docker compose down
docker compose up -d --build pgadmin
```

Veritabanı dahil bütün Docker volume'larını sıfırlamak için:

```powershell
docker compose down -v
```

> `docker compose down -v` PostgreSQL verisini ve pgAdmin ayar volume'unu kalıcı olarak siler. Kullanıcı verisi olan ortamda çalıştırmayın.

Mock seed, `audit_logs` içindeki `mock-v20` işaretiyle korunur. Dengeli demo seti 4 grup, 62 şube, 12 ana ürün, grup bazlı BI/KO/PR ürün gamları, ST/UZ/OZ portföy tipleri, 198 portföy ve 2024-2026 arasındaki altı yıl–dönem kombinasyonunu içerir. Normal container restart'larında aynı sürüm tekrar uygulanmaz. Compose yalnız sürekli çalışan `postgres`, `web` ve `pgadmin` servislerini içerir; ayrı migrate veya seed container'ı yoktur.

## Bağlantı Bilgileri

Lokal PostgreSQL bağlantısı:

```text
Host=localhost
Port=5432
Database=bank_urun
Username=bank_urun
Password=bank_urun
```

pgAdmin girişi:

```text
Email=admin@bankurun.com
Password=bank_urun
```

`Bank Urun PostgreSQL` sunucusu hazır gelir. Sunucu şifresi sorulursa `bank_urun` yazın. Container ağı içinde PostgreSQL host adı `postgres` olur.

## Docker Olmadan Çalıştırma

Bilgisayarda .NET 10 SDK ve PostgreSQL 17 kurulu olmalıdır. Önce kullanıcı ve veritabanını oluşturun:

```powershell
psql -U postgres -d postgres -c "create user bank_urun with password 'bank_urun';" -c "create database bank_urun owner bank_urun;"
```

Migration, mock veri ve uygulama:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project BankUrun.Web\BankUrun.Web.csproj --startup-project BankUrun.Web\BankUrun.Web.csproj
$env:PGPASSWORD = "bank_urun"
psql -h localhost -U bank_urun -d bank_urun -v ON_ERROR_STOP=1 -f scripts\seed-mock-data.sql
dotnet run --project BankUrun.Web\BankUrun.Web.csproj --urls http://localhost:5188
```

Mock veri istenmiyorsa `psql ... seed-mock-data.sql` satırını atlayabilirsiniz.

## Portable PostgreSQL

Admin yetkisi bulunmayan Windows bilgisayarlarda PostgreSQL 17 ZIP binary paketi kullanıcı klasöründen çalıştırılabilir.

1. PostgreSQL 17 Windows x86-64 ZIP paketini [EDB PostgreSQL Binaries](https://www.enterprisedb.com/download-postgresql-binaries) sayfasından indirin.
2. Repo klasöründe aşağıdaki komutu çalıştırın:

```powershell
$pgZip = Get-ChildItem "$env:USERPROFILE\Downloads\postgresql-17*-windows-x64-binaries.zip" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
powershell -ExecutionPolicy Bypass -File .\scripts\Run-WithPortablePostgres.ps1 -PgZipPath $pgZip.FullName -Seed
```

ZIP daha önce açıldıysa:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Run-WithPortablePostgres.ps1 -PgRoot "$env:USERPROFILE\Tools\postgresql" -Seed
```

Portable servisi durdurmak için:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Stop-PortablePostgres.ps1
```

Portable dosyalar `.tools\postgres`, veritabanı verisi `.data\postgres` altında tutulur ve Git'e eklenmez.

## Sayfalar

- `/Parameters`: Ana ürün kriteri/hesaplama yöntemi ile portföy + ana ürün aylık hedeflerini yönetir. Alt ürün batch gerçekleşmeleri salt okunurdur.
- `/Performance`: Şube, şube–ürün, ana ürün ve portföy sonuçlarını dört modda gösterir. Tüm yıllar ve tüm dönemler seçildiğinde her dönem ayrı satır kalır.
- `/Products`: Ana/alt ürünler, ürün gamları ve dönemsel gam–ana ürün atamalarını yönetir.
- `/Organization`: Grup, şube, portföy, portföy tipi ve dönemsel şube–ürün istisnalarını yönetir.
- `/Dashboard` ve `/Scores`: Geriye uyumluluk için `/Performance` sayfasına yönlenir.

## Hesaplama Kuralları

- Dönem 1 Ocak-Haziran, dönem 2 Temmuz-Aralık aylarını kapsar.
- `Average`: Portföy + ana ürün için altı aylık hedef ve gerçekleşme ortalaması alınır.
- `Cumulative`: Portföy + ana ürün için altı aylık hedef ve gerçekleşme toplamı alınır.
- Bir ana ürünün aylık gerçekleşmesi, portföyde ona bağlı bütün stabil alt ürün gerçekleşmelerinin toplamıdır.
- Aynı stabil alt ürün birden fazla ana ürünü besleyebilir; ham değer bölünmeden her birine katkı verir.
- Hedef dönem kapanmadan görülebilir; gerçekleşme ve puan ancak dönem kapandıktan ve altı aylık batch tamamlandıktan sonra oluşur.
- `H/G = gerçekleşme / hedef`; hedef sıfırsa oran ve puan sıfırdır.
- `HGO puanı = kriter puanı × min(H/G, 1)`.
- İlk sürümde toplam puan HGO puanına eşittir ve kriter puanını aşmaz.
- Eksik batch ayı bulunan satır sıralamaya katılmaz.
- Şube sırası grup + yıl + dönem; şube–ürün sırası grup + yıl + dönem + ana ürün; ana ürün sırası seçili grup kapsamı + yıl + dönem; portföy resmî sırası grup + ürün gamı + yıl + dönem havuzunda `DENSE_RANK` ile hesaplanır.
- Portföy şube içi sırası, şube + yıl + dönem içinde `toplam puan / atanmış kriter` başarı oranına göredir.

## Temel Veritabanı Tabloları

- `product_definitions`: Ana/alt ürün kodu ve güncel adı.
- `main_product_instances`: Ana ürünün yıl/dönem kaydı.
- `sub_product_instances`: Alt ürünün ana ürün instance bağlantısı.
- `group_definitions`: Grup tanımları ve hesaplamaya etki etmeyen grup tipi.
- `branches`: Gruba bağlı şubeler.
- `main_product_parameters`: Grup + ana ürün instance hesaplama tipi ve kriter puanı.
- `product_gamuts`: Gruba bağlı BI/KO/PR gibi ürün gamları.
- `product_gamut_main_product_assignments`: Ürün gamının dönemsel ana ürün kapsamı.
- `portfolios`: Şube, ürün gamı ve portföy tipine bağlı performans birimleri.
- `portfolio_main_product_monthly_targets`: Portföy + ana ürün aylık hedefleri.
- `portfolio_sub_product_monthly_metrics`: Portföy + stabil alt ürün aylık batch gerçekleşmeleri.
- `branch_main_product_exclusions`: Ana ürünü belirli şubeden dönemsel çıkaran istisnalar.
- `audit_logs`: Yönetim işlemleri ve seed işaretleri.

Şemayı pgAdmin'de `Databases > bank_urun > Schemas > public > Tables` yolundan görebilirsiniz. SQL ile tablo listesini almak için:

```sql
select table_name
from information_schema.tables
where table_schema = 'public'
order by table_name;
```

## Geliştirme Doğrulaması

```powershell
dotnet restore BankUrun.slnx
dotnet build BankUrun.slnx
dotnet test BankUrun.slnx
docker compose config
```
