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

insert into group_definitions (group_no, name, created_at, updated_at)
values
  ('1001', 'Ankara Karma Grup', now(), now()),
  ('1002', 'İstanbul Kurumsal Grup', now(), now()),
  ('1003', 'Ege Ticari Grup', now(), now())
on conflict (group_no) do update
set
  name = excluded.name,
  updated_at = now();

insert into unit_definitions (unit_no, name, created_at, updated_at)
values
  ('BIR01', 'Kredi Tahsis Birimi', now(), now()),
  ('BIR02', 'Mevduat Yönetimi Birimi', now(), now()),
  ('BIR03', 'Dış İşlemler Birimi', now(), now()),
  ('BIR04', 'Portföy Yönetimi Birimi', now(), now())
on conflict (unit_no) do update
set
  name = excluded.name,
  updated_at = now();

insert into branches (branch_code, name, branch_type, created_at, updated_at)
values
  ('0601', 'Ankara Merkez Şube', 'Karma', now(), now()),
  ('3401', 'İstanbul Kurumsal Şube', 'Kurumsal', now(), now()),
  ('3501', 'İzmir Ticari Şube', 'Ticari', now(), now())
on conflict (branch_code) do update
set
  name = excluded.name,
  branch_type = excluded.branch_type,
  updated_at = now();

insert into group_units (group_id, unit_id, created_at)
select group_definition.id, unit_definition.id, now()
from (
  values
    ('1001', 'BIR01'),
    ('1001', 'BIR02'),
    ('1002', 'BIR01'),
    ('1002', 'BIR04'),
    ('1003', 'BIR02'),
    ('1003', 'BIR03')
) as seed(group_no, unit_no)
join group_definitions group_definition
  on group_definition.group_no = seed.group_no
join unit_definitions unit_definition
  on unit_definition.unit_no = seed.unit_no
on conflict (group_id, unit_id) do nothing;

insert into branch_units (branch_id, unit_id, created_at)
select branch.id, unit_definition.id, now()
from (
  values
    ('0601', 'BIR01'),
    ('0601', 'BIR02'),
    ('3401', 'BIR01'),
    ('3401', 'BIR04'),
    ('3501', 'BIR02'),
    ('3501', 'BIR03')
) as seed(branch_code, unit_no)
join branches branch
  on branch.branch_code = seed.branch_code
join unit_definitions unit_definition
  on unit_definition.unit_no = seed.unit_no
on conflict (branch_id, unit_id) do nothing;

insert into group_product_scores (
  group_id,
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
  group_definition.id,
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
    ('1001', 'MA', 2026, 1, 'KP', 85.50, 1200.00, 0.9800, 0.4500, 0.7200),
    ('1001', 'MA', 2026, 1, 'AB', 64.25, 900.00, 0.8500, 0.5000, 0.6100),
    ('1002', 'NB', 2026, 1, 'AB', 72.00, 1100.00, 0.9100, 0.5600, 0.6700),
    ('1002', 'NB', 2026, 1, 'OH', 58.75, 800.00, 0.7600, 0.4300, 0.5900),
    ('1003', 'MA', 2026, 2, 'AB', 69.10, 950.00, 0.8800, 0.4700, 0.6300),
    ('1003', 'MC', 2026, 1, '42', 54.40, 700.00, 0.7300, 0.3900, 0.5200)
) as seed(group_no, main_code, year, term, sub_code, score, target_value, hgo_share, development_share, size_share)
join group_definitions group_definition
  on group_definition.group_no = seed.group_no
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
on conflict (group_id, sub_product_instance_id) do update
set
  score = excluded.score,
  target_value = excluded.target_value,
  hgo_share = excluded.hgo_share,
  development_share = excluded.development_share,
  size_share = excluded.size_share,
  updated_at = now();

insert into audit_logs (action, entity_name, entity_key, description, actor, created_at)
values ('SeedMockData', 'Database', 'mock-data-v5', 'Ürün, organizasyon ve puan mock verileri yüklendi.', 'seed-script', now());

commit;
