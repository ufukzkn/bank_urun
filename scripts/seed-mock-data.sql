begin;

insert into products (code, name, type, is_active, created_at, updated_at)
values
  ('AU', 'Bireysel Ürün Sahipliği', 'Main', true, now(), now()),
  ('B1', 'Bireysel Kredi', 'Main', true, now(), now()),
  ('D2', 'Dış İşlemler', 'Main', true, now(), now()),
  ('G1', 'Gayrinakit Krediler', 'Main', true, now(), now()),
  ('K0', 'Nakit Krediler', 'Main', true, now(), now()),
  ('MA', 'TL Vadesiz Kaynak', 'Main', true, now(), now()),
  ('MC', 'TL Vadeli Kaynak', 'Main', true, now(), now()),
  ('ME', 'TL Vadeli Kaynak', 'Main', true, now(), now()),
  ('NB', 'Kobi Ürün Grubu', 'Main', true, now(), now()),
  ('KP', 'Kredi Paketleri', 'Sub', true, now(), now()),
  ('VS', 'Vadeli/Vadesiz Segment', 'Sub', true, now(), now()),
  ('X1', 'Özel İşlem Alt Ürünü', 'Sub', true, now(), now()),
  ('XD', 'Dış İşlem Detayı', 'Sub', true, now(), now()),
  ('24', 'İki Dört Alt Ürün', 'Sub', true, now(), now()),
  ('42', 'Kırk İki Alt Ürün', 'Sub', true, now(), now()),
  ('9B', 'Dokuz B Alt Ürün', 'Sub', true, now(), now()),
  ('BV', 'Bireysel Varlık', 'Sub', true, now(), now()),
  ('AB', 'Ortak Besleyen Alt Ürün', 'Sub', true, now(), now()),
  ('OH', 'Operasyonel Hizmet', 'Sub', true, now(), now()),
  ('Q1', 'Q1 Kampanya', 'Sub', true, now(), now()),
  ('UY', 'Uyum Alt Ürünü', 'Sub', true, now(), now())
on conflict (type, code) do update
set
  name = excluded.name,
  is_active = true,
  updated_at = now();

insert into periods (year, term)
values
  (2025, 1),
  (2025, 2),
  (2026, 1),
  (2026, 2)
on conflict (year, term) do nothing;

insert into main_product_periods (main_product_id, period_id, created_at)
select product.id, period.id, now()
from products product
cross join periods period
where product.type = 'Main'
  and (
    (period.year = 2026 and period.term in (1, 2) and product.code in ('AU', 'B1', 'D2', 'G1', 'K0', 'MA', 'MC', 'NB'))
    or (period.year = 2025 and period.term in (1, 2) and product.code in ('MA', 'MC', 'ME', 'B1', 'K0'))
  )
on conflict (main_product_id, period_id) do nothing;

insert into sub_product_assignments (main_product_period_id, sub_product_id, created_at)
select main_period.id, sub_product.id, now()
from main_product_periods main_period
join products main_product on main_product.id = main_period.main_product_id
join periods period on period.id = main_period.period_id
join products sub_product on sub_product.type = 'Sub'
where
  (main_product.code = 'MA' and period.year = 2026 and period.term = 2 and sub_product.code in ('KP', 'VS', 'X1', 'XD', '24', '42', '9B', 'BV', 'AB'))
  or (main_product.code = 'NB' and period.year = 2026 and period.term = 2 and sub_product.code in ('AB', 'OH', 'Q1', 'UY'))
  or (main_product.code = 'MA' and period.year = 2026 and period.term = 1 and sub_product.code in ('AB', 'KP', 'VS'))
  or (main_product.code = 'MC' and period.year = 2026 and period.term = 2 and sub_product.code in ('AB', '42'))
  or (main_product.code = 'B1' and period.year = 2025 and period.term = 1 and sub_product.code in ('KP', 'BV'))
  or (main_product.code = 'K0' and period.year = 2025 and period.term = 2 and sub_product.code in ('24', '9B'))
on conflict (main_product_period_id, sub_product_id) do nothing;

insert into audit_logs (action, entity_name, entity_key, description, actor, created_at)
values ('SeedMockData', 'Database', 'mock-data-v1', 'Mock ürün, dönem ve bağlantı verileri yüklendi.', 'seed-script', now());

commit;
