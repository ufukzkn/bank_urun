begin;

insert into product_definitions (product_type, code, name, is_active, created_at, updated_at)
values
  ('Main', 'AU', 'Bireysel Ürün Sahipliği', true, now(), now()),
  ('Main', 'B1', 'Bireysel Kredi', true, now(), now()),
  ('Main', 'D2', 'Dış İşlemler', true, now(), now()),
  ('Main', 'G1', 'Gayrinakit Krediler', true, now(), now()),
  ('Main', 'K0', 'Nakit Krediler', true, now(), now()),
  ('Main', 'MA', 'TL Vadesiz Kaynak', true, now(), now()),
  ('Main', 'MC', 'TL Vadeli Kaynak', true, now(), now()),
  ('Main', 'NB', 'Kobi Ürün Grubu', true, now(), now()),
  ('Sub', 'KP', 'Kredi Paketleri', true, now(), now()),
  ('Sub', 'VS', 'Vadeli/Vadesiz Segment', true, now(), now()),
  ('Sub', 'X1', 'Özel İşlem Alt Ürünü', true, now(), now()),
  ('Sub', 'XD', 'Dış İşlem Detayı', true, now(), now()),
  ('Sub', '24', 'İki Dört Alt Ürün', true, now(), now()),
  ('Sub', '42', 'Kırk İki Alt Ürün', true, now(), now()),
  ('Sub', '9B', 'Dokuz B Alt Ürün', true, now(), now()),
  ('Sub', 'AB', 'Ortak Besleyen Alt Ürün', true, now(), now()),
  ('Sub', 'OH', 'Operasyonel Hizmet', true, now(), now()),
  ('Sub', 'Q1', 'Q1 Kampanya', true, now(), now()),
  ('Sub', 'UY', 'Uyum Alt Ürünü', true, now(), now())
on conflict (product_type, code) do update
set
  name = excluded.name,
  is_active = true,
  updated_at = now();

insert into main_product_instances (main_product_id, main_product_type, year, term, created_at)
select product.id, 'Main', seed.year, seed.term, now()
from product_definitions product
join (
  values
    ('AU', 2026, 1),
    ('B1', 2026, 1),
    ('D2', 2026, 1),
    ('G1', 2026, 1),
    ('K0', 2026, 1),
    ('MA', 2026, 1),
    ('MC', 2026, 1),
    ('NB', 2026, 1),
    ('MA', 2026, 2),
    ('NB', 2026, 2),
    ('B1', 2025, 2),
    ('K0', 2025, 2)
) as seed(code, year, term)
  on product.product_type = 'Main'
 and product.code = seed.code
on conflict (main_product_id, year, term) do nothing;

insert into sub_product_instances (main_product_instance_id, sub_product_id, sub_product_type, created_at)
select main_instance.id, sub_product.id, 'Sub', now()
from main_product_instances main_instance
join product_definitions main_product
  on main_product.id = main_instance.main_product_id
 and main_product.product_type = 'Main'
join product_definitions sub_product
  on sub_product.product_type = 'Sub'
join (
  values
    ('MA', 2026, 1, 'KP'),
    ('MA', 2026, 1, 'VS'),
    ('MA', 2026, 1, 'X1'),
    ('MA', 2026, 1, 'XD'),
    ('MA', 2026, 1, '24'),
    ('MA', 2026, 1, '42'),
    ('MA', 2026, 1, '9B'),
    ('MA', 2026, 1, 'AB'),
    ('NB', 2026, 1, 'AB'),
    ('NB', 2026, 1, 'OH'),
    ('NB', 2026, 1, 'Q1'),
    ('NB', 2026, 1, 'UY'),
    ('MA', 2026, 2, 'AB'),
    ('MA', 2026, 2, 'KP'),
    ('NB', 2026, 2, 'AB'),
    ('MC', 2026, 1, '42'),
    ('B1', 2025, 2, 'KP'),
    ('K0', 2025, 2, '24')
) as seed(main_code, year, term, sub_code)
  on main_product.code = seed.main_code
 and main_instance.year = seed.year
 and main_instance.term = seed.term
 and sub_product.code = seed.sub_code
on conflict (main_product_instance_id, sub_product_id) do nothing;

insert into audit_logs (action, entity_name, entity_key, description, actor, created_at)
values ('SeedMockData', 'Database', 'mock-data-v4', 'Product definition ve instance mock verileri yüklendi.', 'seed-script', now());

commit;
