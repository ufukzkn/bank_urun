begin;

insert into product_definitions (product_type, code, name, is_active, created_at, updated_at)
values
  ('Main', 'AU', 'Bireysel Ürün Sahipliği', true, now(), now()),
  ('Main', 'B1', 'Bireysel Kredi', true, now(), now()),
  ('Main', 'D2', 'Dış İşlemler', true, now(), now()),
  ('Main', 'G1', 'Gayrinakit Krediler', true, now(), now()),
  ('Main', 'K0', 'Nakit Krediler', true, now(), now()),
  ('Main', 'KR', 'Kredi Kartı Cirosu', true, now(), now()),
  ('Main', 'KK', 'Kartlı Krediler', true, now(), now()),
  ('Main', 'MA', 'TL Vadesiz Kaynak', true, now(), now()),
  ('Main', 'MC', 'TL Vadeli Kaynak', true, now(), now()),
  ('Main', 'NB', 'Kobi Ürün Grubu', true, now(), now()),
  ('Main', 'SG', 'Sigorta ve Emeklilik', true, now(), now()),
  ('Main', 'YP', 'YP Kaynak Ürünleri', true, now(), now()),
  ('Sub', '24', 'İki Dört Alt Ürün', true, now(), now()),
  ('Sub', '42', 'Kırk İki Alt Ürün', true, now(), now()),
  ('Sub', '5F', 'Beş F Performans Ürünü', true, now(), now()),
  ('Sub', '9B', 'Dokuz B Alt Ürün', true, now(), now()),
  ('Sub', 'AB', 'Ortak Besleyen Alt Ürün', true, now(), now()),
  ('Sub', 'CD', 'Çapraz Döviz İşlemi', true, now(), now()),
  ('Sub', 'EF', 'Efektif Tahsilat', true, now(), now()),
  ('Sub', 'GH', 'Gayrinakit Hacim', true, now(), now()),
  ('Sub', 'JK', 'Kısa Vadeli Kredi', true, now(), now()),
  ('Sub', 'KP', 'Kredi Paketleri', true, now(), now()),
  ('Sub', 'LM', 'Limit Yönetimi', true, now(), now()),
  ('Sub', 'MT', 'Mevduat Transferi', true, now(), now()),
  ('Sub', 'NO', 'Nakit Operasyon', true, now(), now()),
  ('Sub', 'OH', 'Operasyonel Hizmet', true, now(), now()),
  ('Sub', 'PR', 'Prim Üretimi', true, now(), now()),
  ('Sub', 'Q1', 'Q1 Kampanya', true, now(), now()),
  ('Sub', 'RA', 'Risk Ağırlıklı Ürün', true, now(), now()),
  ('Sub', 'SA', 'Satış Aktivitesi', true, now(), now()),
  ('Sub', 'ST', 'Sigorta Tahsilatı', true, now(), now()),
  ('Sub', 'TR', 'Ticari Rotatif', true, now(), now()),
  ('Sub', 'UY', 'Uyum Alt Ürünü', true, now(), now()),
  ('Sub', 'UV', 'Uzun Vadeli Ürün', true, now(), now()),
  ('Sub', 'VB', 'Vadeli Bakiye', true, now(), now()),
  ('Sub', 'VS', 'Vadeli/Vadesiz Segment', true, now(), now()),
  ('Sub', 'X1', 'Özel İşlem Alt Ürünü', true, now(), now()),
  ('Sub', 'XA', 'Çapraz Satış Ürünü', true, now(), now()),
  ('Sub', 'XD', 'Dış İşlem Detayı', true, now(), now()),
  ('Sub', 'YZ', 'Yeni Ziyaret Ürünü', true, now(), now())
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
    ('AU', 2026, 1), ('B1', 2026, 1), ('D2', 2026, 1), ('G1', 2026, 1),
    ('K0', 2026, 1), ('KR', 2026, 1), ('KK', 2026, 1), ('MA', 2026, 1),
    ('MC', 2026, 1), ('NB', 2026, 1), ('SG', 2026, 1), ('YP', 2026, 1),
    ('B1', 2026, 2), ('K0', 2026, 2), ('KR', 2026, 2), ('MA', 2026, 2),
    ('MC', 2026, 2), ('NB', 2026, 2), ('SG', 2026, 2), ('YP', 2026, 2),
    ('B1', 2025, 2), ('D2', 2025, 2), ('G1', 2025, 2), ('K0', 2025, 2),
    ('MA', 2025, 2), ('MC', 2025, 2)
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
    ('AU', 2026, 1, 'VS'), ('AU', 2026, 1, 'KP'), ('AU', 2026, 1, 'AB'), ('AU', 2026, 1, 'OH'), ('AU', 2026, 1, '24'),
    ('B1', 2026, 1, 'KP'), ('B1', 2026, 1, 'XA'), ('B1', 2026, 1, '5F'), ('B1', 2026, 1, 'PR'), ('B1', 2026, 1, 'UV'), ('B1', 2026, 1, 'YZ'),
    ('D2', 2026, 1, 'XD'), ('D2', 2026, 1, 'CD'), ('D2', 2026, 1, 'EF'), ('D2', 2026, 1, 'NO'), ('D2', 2026, 1, 'TR'),
    ('G1', 2026, 1, 'GH'), ('G1', 2026, 1, 'JK'), ('G1', 2026, 1, 'LM'), ('G1', 2026, 1, 'PR'), ('G1', 2026, 1, 'ST'),
    ('K0', 2026, 1, '24'), ('K0', 2026, 1, 'CD'), ('K0', 2026, 1, 'EF'), ('K0', 2026, 1, 'GH'), ('K0', 2026, 1, 'JK'), ('K0', 2026, 1, 'LM'),
    ('KR', 2026, 1, 'KP'), ('KR', 2026, 1, 'AB'), ('KR', 2026, 1, 'TR'), ('KR', 2026, 1, 'MT'), ('KR', 2026, 1, 'RA'),
    ('KK', 2026, 1, 'CD'), ('KK', 2026, 1, 'EF'), ('KK', 2026, 1, 'GH'), ('KK', 2026, 1, 'ST'), ('KK', 2026, 1, 'UY'),
    ('MA', 2026, 1, 'KP'), ('MA', 2026, 1, 'VS'), ('MA', 2026, 1, 'X1'), ('MA', 2026, 1, 'XD'), ('MA', 2026, 1, '24'), ('MA', 2026, 1, '42'), ('MA', 2026, 1, '9B'), ('MA', 2026, 1, 'AB'), ('MA', 2026, 1, 'VB'), ('MA', 2026, 1, 'MT'),
    ('MC', 2026, 1, 'KP'), ('MC', 2026, 1, 'VS'), ('MC', 2026, 1, '42'), ('MC', 2026, 1, '9B'), ('MC', 2026, 1, 'RA'), ('MC', 2026, 1, 'SA'),
    ('NB', 2026, 1, 'AB'), ('NB', 2026, 1, 'OH'), ('NB', 2026, 1, 'Q1'), ('NB', 2026, 1, 'UY'), ('NB', 2026, 1, 'TR'), ('NB', 2026, 1, 'ST'),
    ('SG', 2026, 1, 'OH'), ('SG', 2026, 1, 'Q1'), ('SG', 2026, 1, '5F'), ('SG', 2026, 1, 'NO'), ('SG', 2026, 1, 'PR'),
    ('YP', 2026, 1, 'VB'), ('YP', 2026, 1, 'VS'), ('YP', 2026, 1, 'SA'), ('YP', 2026, 1, 'YZ'), ('YP', 2026, 1, 'UV'),
    ('B1', 2026, 2, 'KP'), ('B1', 2026, 2, 'XA'), ('B1', 2026, 2, 'PR'),
    ('K0', 2026, 2, '24'), ('K0', 2026, 2, 'CD'), ('K0', 2026, 2, 'GH'),
    ('KR', 2026, 2, 'AB'), ('KR', 2026, 2, 'MT'), ('KR', 2026, 2, 'TR'),
    ('MA', 2026, 2, 'AB'), ('MA', 2026, 2, 'KP'), ('MA', 2026, 2, 'VS'), ('MA', 2026, 2, 'VB'),
    ('MC', 2026, 2, '42'), ('MC', 2026, 2, '9B'), ('MC', 2026, 2, 'RA'),
    ('NB', 2026, 2, 'AB'), ('NB', 2026, 2, 'OH'), ('NB', 2026, 2, 'UY'),
    ('SG', 2026, 2, 'OH'), ('SG', 2026, 2, 'NO'), ('SG', 2026, 2, '5F'),
    ('YP', 2026, 2, 'VB'), ('YP', 2026, 2, 'SA'), ('YP', 2026, 2, 'UV'),
    ('B1', 2025, 2, 'KP'), ('B1', 2025, 2, 'XA'),
    ('D2', 2025, 2, 'XD'), ('D2', 2025, 2, 'EF'),
    ('G1', 2025, 2, 'GH'), ('G1', 2025, 2, 'LM'),
    ('K0', 2025, 2, '24'), ('K0', 2025, 2, 'CD'),
    ('MA', 2025, 2, 'AB'), ('MA', 2025, 2, 'KP'),
    ('MC', 2025, 2, '42'), ('MC', 2025, 2, 'VS')
) as seed(main_code, year, term, sub_code)
  on main_product.code = seed.main_code
 and main_instance.year = seed.year
 and main_instance.term = seed.term
 and sub_product.code = seed.sub_code
on conflict (main_product_instance_id, sub_product_id) do nothing;

delete from branches
where branch_code in ('1601', '9901')
   or group_id in (select id from group_definitions where group_no in ('1004', '1005'));

delete from group_definitions
where group_no in ('1004', '1005');

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
  ('1003', 'TICARI - 1', 'Ticari', true, true, true, true, now(), now())
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
    ('1001', '0101', 'Adana Bölge Şube'),
    ('1001', '0601', 'Ankara Merkez Şube'),
    ('1001', '0701', 'Antalya Karma Şube'),
    ('1001', '5501', 'Samsun Karma Şube'),
    ('1002', '3401', 'İstanbul Kurumsal Şube'),
    ('1002', '3402', 'Maslak Kurumsal Şube'),
    ('1002', '4101', 'Kocaeli Kurumsal Şube'),
    ('1002', '3502', 'İzmir Kurumsal Şube'),
    ('1003', '3501', 'İzmir Ticari Şube'),
    ('1003', '1602', 'Bursa Ticari Şube'),
    ('1003', '4201', 'Konya Ticari Şube'),
    ('1003', '2701', 'Gaziantep Ticari Şube')
) as seed(group_no, branch_code, name)
join group_definitions group_definition
  on group_definition.group_no = seed.group_no
on conflict (branch_code) do update
set
  group_id = excluded.group_id,
  name = excluded.name,
  updated_at = now();

with selected_branches as (
  select
    branch.id,
    branch.branch_code,
    row_number() over (order by branch.branch_code) as branch_rank
  from branches branch
  where branch.branch_code in ('0101', '0601', '0701', '5501', '3401', '3402', '4101', '3502', '3501', '1602', '4201', '2701')
),
ranked_products as (
  select
    sub_instance.id,
    row_number() over (
      order by main_instance.year desc,
               main_instance.term desc,
               main_product.code,
               sub_product.code,
               sub_instance.id
    ) as product_rank
  from sub_product_instances sub_instance
  join main_product_instances main_instance
    on main_instance.id = sub_instance.main_product_instance_id
  join product_definitions main_product
    on main_product.id = main_instance.main_product_id
  join product_definitions sub_product
    on sub_product.id = sub_instance.sub_product_id
  where main_instance.year in (2025, 2026)
    and main_instance.term in (1, 2)
),
score_seed as (
  select
    selected_branches.id as branch_id,
    ranked_products.id as sub_product_instance_id,
    selected_branches.branch_rank,
    ranked_products.product_rank,
    (80 + ((selected_branches.branch_rank * 7 + ranked_products.product_rank * 3) % 90))::numeric(18, 2) as target_value,
    case ((selected_branches.branch_rank + ranked_products.product_rank) % 5)
      when 0 then 1.1200
      when 1 then 0.9400
      when 2 then 0.8100
      when 3 then 0.6800
      else 0.5500
    end::numeric(9, 4) as success_ratio,
    case ((selected_branches.branch_rank + ranked_products.product_rank) % 3)
      when 0 then 0.7000
      when 1 then 0.6000
      else 0.5500
    end::numeric(9, 4) as hgo_share,
    case ((selected_branches.branch_rank + ranked_products.product_rank) % 3)
      when 0 then 0.1500
      when 1 then 0.2500
      else 0.3000
    end::numeric(9, 4) as development_share,
    case ((selected_branches.branch_rank + ranked_products.product_rank) % 3)
      when 0 then 0.1500
      when 1 then 0.1500
      else 0.1500
    end::numeric(9, 4) as size_share
  from selected_branches
  cross join ranked_products
  where ranked_products.product_rank <= 18
    and (ranked_products.product_rank <= 5 or (ranked_products.product_rank + selected_branches.branch_rank) % 4 = 0)
)
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
  branch_id,
  sub_product_instance_id,
  round((target_value * success_ratio)::numeric, 2),
  target_value,
  hgo_share,
  development_share,
  size_share,
  now(),
  now()
from score_seed
on conflict (branch_id, sub_product_instance_id) do update
set
  score = excluded.score,
  target_value = excluded.target_value,
  hgo_share = excluded.hgo_share,
  development_share = excluded.development_share,
  size_share = excluded.size_share,
  updated_at = now();

insert into audit_logs (action, entity_name, entity_key, description, actor, created_at)
values ('SeedMockData', 'Database', 'mock-data-v7', 'Zengin ürün, şube ve performans mock verileri yüklendi.', 'seed-script', now());

commit;
