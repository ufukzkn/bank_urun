begin;

insert into main_products (code, name, year, term, is_active, created_at, updated_at)
values
  ('AU', 'Bireysel Ürün Sahipliği', 2026, 1, true, now(), now()),
  ('B1', 'Bireysel Kredi', 2026, 1, true, now(), now()),
  ('D2', 'Dış İşlemler', 2026, 1, true, now(), now()),
  ('G1', 'Gayrinakit Krediler', 2026, 1, true, now(), now()),
  ('K0', 'Nakit Krediler', 2026, 1, true, now(), now()),
  ('MA', 'TL Vadesiz Kaynak', 2026, 1, true, now(), now()),
  ('MC', 'TL Vadeli Kaynak', 2026, 1, true, now(), now()),
  ('NB', 'Kobi Ürün Grubu', 2026, 1, true, now(), now()),
  ('M2', 'TL Vadesiz Kaynak', 2026, 2, true, now(), now()),
  ('N2', 'Kobi Ürün Grubu', 2026, 2, true, now(), now()),
  ('B2', 'Bireysel Kredi', 2025, 2, true, now(), now()),
  ('K2', 'Nakit Krediler', 2025, 2, true, now(), now())
on conflict (code, year, term) do update
set
  name = excluded.name,
  is_active = true,
  updated_at = now();

insert into sub_products (main_product_id, code, name, is_active, created_at, updated_at)
select main_product.id, seed.code, seed.name, true, now(), now()
from main_products main_product
join (
  values
    ('MA', 2026, 1, 'KP', 'Kredi Paketleri'),
    ('MA', 2026, 1, 'VS', 'Vadeli/Vadesiz Segment'),
    ('MA', 2026, 1, 'X1', 'Özel İşlem Alt Ürünü'),
    ('MA', 2026, 1, 'XD', 'Dış İşlem Detayı'),
    ('MA', 2026, 1, '24', 'İki Dört Alt Ürün'),
    ('MA', 2026, 1, '42', 'Kırk İki Alt Ürün'),
    ('MA', 2026, 1, '9B', 'Dokuz B Alt Ürün'),
    ('MA', 2026, 1, 'AB', 'Ortak Besleyen Alt Ürün'),
    ('NB', 2026, 1, 'AB', 'Ortak Besleyen Alt Ürün'),
    ('NB', 2026, 1, 'OH', 'Operasyonel Hizmet'),
    ('NB', 2026, 1, 'Q1', 'Q1 Kampanya'),
    ('NB', 2026, 1, 'UY', 'Uyum Alt Ürünü'),
    ('M2', 2026, 2, 'AB', 'Ortak Besleyen Alt Ürün'),
    ('M2', 2026, 2, 'KP', 'Kredi Paketleri'),
    ('N2', 2026, 2, 'AB', 'Ortak Besleyen Alt Ürün'),
    ('MC', 2026, 1, '42', 'Kırk İki Alt Ürün'),
    ('B2', 2025, 2, 'KP', 'Kredi Paketleri'),
    ('K2', 2025, 2, '24', 'İki Dört Alt Ürün')
) as seed(main_code, year, term, code, name)
  on main_product.code = seed.main_code
 and main_product.year = seed.year
 and main_product.term = seed.term
on conflict (main_product_id, code) do update
set
  name = excluded.name,
  is_active = true,
  updated_at = now();

insert into audit_logs (action, entity_name, entity_key, description, actor, created_at)
values ('SeedMockData', 'Database', 'mock-data-v2', 'Mock ana ürün ve alt ürün verileri yüklendi.', 'seed-script', now());

commit;
