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

insert into main_product_instances (main_product_id, product_definition_type, year, term, created_at)
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

insert into sub_product_instances (main_product_instance_id, sub_product_id, product_definition_type, created_at)
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

insert into group_definitions (
  group_no,
  name,
  group_segment,
  is_active,
  branch_performance_enabled,
  miy_performance_enabled,
  scale_enabled,
  created_at,
  updated_at
)
values
  ('1001', 'KARMA - 1', 'Karma', true, true, true, true, now(), now()),
  ('1002', 'KURUMSAL - 1', 'Kurumsal', true, true, true, true, now(), now()),
  ('1003', 'TICARI - 1', 'Ticari', true, true, true, true, now(), now()),
  ('1004', 'KOBI - 1', 'Kobi', true, true, true, true, now(), now()),
  ('1005', 'DIGER - 1', 'Diger', true, true, false, false, now(), now())
on conflict (group_no) do update
set
  name = excluded.name,
  group_segment = excluded.group_segment,
  is_active = excluded.is_active,
  branch_performance_enabled = excluded.branch_performance_enabled,
  miy_performance_enabled = excluded.miy_performance_enabled,
  scale_enabled = excluded.scale_enabled,
  updated_at = now();

insert into branches (group_id, branch_code, name, created_at, updated_at)
select group_definition.id, seed.branch_code, seed.name, now(), now()
from (
  values
    ('1001', '0601', 'Ankara Merkez Şube'),
    ('1001', '0101', 'Adana Bölge Şube'),
    ('1002', '3401', 'İstanbul Kurumsal Şube'),
    ('1003', '3501', 'İzmir Ticari Şube'),
    ('1004', '1601', 'Bursa Kobi Şube'),
    ('1005', '9901', 'Operasyon Destek Şube')
) as seed(group_no, branch_code, name)
join group_definitions group_definition
  on group_definition.group_no = seed.group_no
on conflict (branch_code) do update
set
  group_id = excluded.group_id,
  name = excluded.name,
  updated_at = now();

insert into branch_product_scores (
  branch_id,
  sub_product_instance_id,
  score,
  target_value,
  hgo_share,
  development_share,
  size_share,
  created_at,
  updated_at
)
select
  branch.id,
  sub_instance.id,
  seed.score,
  seed.target_value,
  seed.hgo_share,
  seed.development_share,
  seed.size_share,
  now(),
  now()
from (
  values
    ('0601', 'MA', 2026, 1, 'KP', 85.50, 120.00, 0.7000, 0.1500, 0.1500),
    ('0601', 'MA', 2026, 1, 'AB', 64.25, 90.00, 0.6500, 0.2000, 0.1500),
    ('0101', 'MA', 2026, 2, 'AB', 69.10, 95.00, 0.7200, 0.1300, 0.1500),
    ('3401', 'NB', 2026, 1, 'AB', 72.00, 110.00, 0.6000, 0.2500, 0.1500),
    ('3401', 'NB', 2026, 1, 'OH', 58.75, 80.00, 0.5500, 0.3000, 0.1500),
    ('3501', 'MC', 2026, 1, '42', 54.40, 70.00, 0.6200, 0.2300, 0.1500),
    ('1601', 'B1', 2025, 2, 'KP', 42.00, 60.00, 0.5800, 0.2700, 0.1500),
    ('9901', 'K0', 2025, 2, '24', 31.00, 50.00, 0.5000, 0.2500, 0.2500)
) as seed(branch_code, main_code, year, term, sub_code, score, target_value, hgo_share, development_share, size_share)
join branches branch
  on branch.branch_code = seed.branch_code
join main_product_instances main_instance
  on main_instance.year = seed.year
 and main_instance.term = seed.term
join product_definitions main_product
  on main_product.id = main_instance.main_product_id
 and main_product.product_type = 'Main'
 and main_product.code = seed.main_code
join sub_product_instances sub_instance
  on sub_instance.main_product_instance_id = main_instance.id
join product_definitions sub_product
  on sub_product.id = sub_instance.sub_product_id
 and sub_product.product_type = 'Sub'
 and sub_product.code = seed.sub_code
on conflict (branch_id, sub_product_instance_id) do update
set
  score = excluded.score,
  target_value = excluded.target_value,
  hgo_share = excluded.hgo_share,
  development_share = excluded.development_share,
  size_share = excluded.size_share,
  updated_at = now();

insert into audit_logs (action, entity_name, entity_key, description, actor, created_at)
values ('SeedMockData', 'Database', 'mock-data-v6', 'Ürün, şube-grup ve performans mock verileri yüklendi.', 'seed-script', now());

commit;
