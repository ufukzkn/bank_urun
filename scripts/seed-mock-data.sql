begin;

insert into product_definitions (product_type, code, name, is_active, created_at, updated_at)
values
  ('Main', 'AU', 'Bireysel Ürün Sahipliği', true, now(), now()),
  ('Main', 'B1', 'Bireysel Krediler', true, now(), now()),
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
  ('Sub', 'Q1', 'Kampanya Ürünü', true, now(), now()),
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
set name = excluded.name, is_active = true, updated_at = now();

insert into main_product_instances (main_product_id, product_definition_type, year, term, created_at)
select product.id, 'Main', period.year, period.term, now()
from product_definitions product
cross join (values (2025, 2), (2026, 1), (2026, 2)) as period(year, term)
where product.product_type = 'Main'
  and product.code in ('AU', 'B1', 'D2', 'G1', 'K0', 'KR', 'KK', 'MA', 'MC', 'NB', 'SG', 'YP')
on conflict (main_product_id, year, term) do nothing;

with ranked_main as (
  select instance.id,
         row_number() over (partition by instance.year, instance.term order by product.code) as product_rank
  from main_product_instances instance
  join product_definitions product on product.id = instance.main_product_id
  where product.product_type = 'Main'
    and product.code in ('AU', 'B1', 'D2', 'G1', 'K0', 'KR', 'KK', 'MA', 'MC', 'NB', 'SG', 'YP')
    and (instance.year, instance.term) in ((2025, 2), (2026, 1), (2026, 2))
),
ranked_sub as (
  select product.id,
         row_number() over (order by product.code) as sub_rank,
         count(*) over () as sub_count
  from product_definitions product
  where product.product_type = 'Sub'
    and product.code in ('24', '42', '5F', '9B', 'AB', 'CD', 'EF', 'GH', 'JK', 'KP', 'LM', 'MT', 'NO', 'OH', 'PR', 'Q1', 'RA', 'SA', 'ST', 'TR', 'UY', 'UV', 'VB', 'VS', 'X1', 'XA', 'XD', 'YZ')
)
insert into sub_product_instances (main_product_instance_id, sub_product_id, product_definition_type, created_at)
select main.id, sub.id, 'Sub', now()
from ranked_main main
cross join generate_series(0, 2) as offset_value
join ranked_sub sub on sub.sub_rank = ((main.product_rank * 2 + offset_value - 1) % sub.sub_count) + 1
on conflict (main_product_instance_id, sub_product_id) do nothing;

delete from branches
where branch_code in (
  '0101','0601','0701','5501','1603','2001','3301','6101',
  '3401','3402','4101','3502','0602','1604','2702','5401',
  '3501','1602','4201','2701','0102','0702','3801','5901','1601','9901'
);
delete from branches
where group_id in (
  select id from group_definitions where group_no in ('1004', '1005')
);
delete from group_definitions where group_no in ('1004', '1005');

insert into group_definitions (
  group_no, name, group_segment, is_active,
  branch_performance_enabled, miy_performance_enabled, scale_enabled,
  created_at, updated_at
)
values
  ('1001', 'KARMA - 1', 'Karma', true, true, true, true, now(), now()),
  ('1002', 'KURUMSAL - 1', 'Kurumsal', true, true, true, true, now(), now()),
  ('1003', 'TICARI - 1', 'Ticari', true, true, true, true, now(), now())
on conflict (group_no) do update
set name = excluded.name,
    group_segment = excluded.group_segment,
    is_active = excluded.is_active,
    branch_performance_enabled = excluded.branch_performance_enabled,
    miy_performance_enabled = excluded.miy_performance_enabled,
    scale_enabled = excluded.scale_enabled,
    updated_at = now();

create temporary table mock_branch_seed (
  group_no varchar(24) not null,
  branch_code varchar(24) not null,
  name varchar(180) not null
) on commit drop;

insert into mock_branch_seed (group_no, branch_code, name)
values
  ('1001', '1101', 'Adana Karma Şubesi'), ('1001', '1102', 'Ankara Merkez Şubesi'),
  ('1001', '1103', 'Antalya Karma Şubesi'), ('1001', '1104', 'Bursa Karma Şubesi'),
  ('1001', '1105', 'Denizli Karma Şubesi'), ('1001', '1106', 'Diyarbakır Karma Şubesi'),
  ('1001', '1107', 'Erzurum Karma Şubesi'), ('1001', '1108', 'Eskişehir Karma Şubesi'),
  ('1001', '1109', 'Gaziantep Karma Şubesi'), ('1001', '1110', 'İstanbul Anadolu Karma Şubesi'),
  ('1001', '1111', 'İstanbul Avrupa Karma Şubesi'), ('1001', '1112', 'İzmir Karma Şubesi'),
  ('1001', '1113', 'Kayseri Karma Şubesi'), ('1001', '1114', 'Konya Karma Şubesi'),
  ('1001', '1115', 'Mersin Karma Şubesi'), ('1001', '1116', 'Samsun Karma Şubesi'),
  ('1001', '1117', 'Trabzon Karma Şubesi'),
  ('1002', '2101', 'Adana Kurumsal Şubesi'), ('1002', '2102', 'Ankara Kurumsal Şubesi'),
  ('1002', '2103', 'Antalya Kurumsal Şubesi'), ('1002', '2104', 'Ataşehir Kurumsal Şubesi'),
  ('1002', '2105', 'Avrupa Kurumsal Şubesi'), ('1002', '2106', 'Bursa Kurumsal Şubesi'),
  ('1002', '2107', 'Çukurova Kurumsal Şubesi'), ('1002', '2108', 'Ege Kurumsal Şubesi'),
  ('1002', '2109', 'Gaziantep Kurumsal Şubesi'), ('1002', '2110', 'İkitelli Kurumsal Şubesi'),
  ('1002', '2111', 'İzmir Kurumsal Şubesi'), ('1002', '2112', 'Kocaeli Kurumsal Şubesi'),
  ('1002', '2113', 'Konya Kurumsal Şubesi'), ('1002', '2114', 'Maslak Kurumsal Şubesi'),
  ('1002', '2115', 'Merter Kurumsal Şubesi'), ('1002', '2116', 'Ostim Kurumsal Şubesi'),
  ('1002', '2117', 'Trakya Kurumsal Şubesi'),
  ('1003', '3101', 'Adana Ticari Şubesi'), ('1003', '3102', 'Ankara Ticari Şubesi'),
  ('1003', '3103', 'Antalya Ticari Şubesi'), ('1003', '3104', 'Bursa Ticari Şubesi'),
  ('1003', '3105', 'Çorlu Ticari Şubesi'), ('1003', '3106', 'Denizli Ticari Şubesi'),
  ('1003', '3107', 'Gaziantep Ticari Şubesi'), ('1003', '3108', 'Gebze Ticari Şubesi'),
  ('1003', '3109', 'İstanbul Ticari Şubesi'), ('1003', '3110', 'İzmir Ticari Şubesi'),
  ('1003', '3111', 'Kayseri Ticari Şubesi'), ('1003', '3112', 'Konya Ticari Şubesi'),
  ('1003', '3113', 'Mersin Ticari Şubesi'), ('1003', '3114', 'Samsun Ticari Şubesi'),
  ('1003', '3115', 'Şekerpınar Ticari Şubesi'), ('1003', '3116', 'Trabzon Ticari Şubesi');

insert into branches (group_id, branch_code, name, created_at, updated_at)
select group_definition.id, seed.branch_code, seed.name, now(), now()
from mock_branch_seed seed
join group_definitions group_definition on group_definition.group_no = seed.group_no
on conflict (branch_code) do update
set group_id = excluded.group_id, name = excluded.name, updated_at = now();

with parameter_seed(code, calculation_type, criterion_score) as (
  values
    ('AU', 'Average', 3.00::numeric), ('B1', 'Cumulative', 5.00::numeric),
    ('D2', 'Cumulative', 3.00::numeric), ('G1', 'Average', 8.00::numeric),
    ('K0', 'Average', 5.00::numeric), ('KR', 'Cumulative', 4.00::numeric),
    ('KK', 'Average', 5.00::numeric), ('MA', 'Average', 21.00::numeric),
    ('MC', 'Average', 7.00::numeric), ('NB', 'Cumulative', 14.00::numeric),
    ('SG', 'Cumulative', 3.00::numeric), ('YP', 'Cumulative', 3.00::numeric)
)
insert into main_product_parameters (
  main_product_instance_id, calculation_type, criterion_score,
  is_active, created_at, updated_at
)
select instance.id, seed.calculation_type, seed.criterion_score, true, now(), now()
from parameter_seed seed
join product_definitions product on product.product_type = 'Main' and product.code = seed.code
join main_product_instances instance
  on instance.main_product_id = product.id
 and (instance.year, instance.term) in ((2025, 2), (2026, 1), (2026, 2))
on conflict (main_product_instance_id) do update
set calculation_type = excluded.calculation_type,
    criterion_score = excluded.criterion_score,
    is_active = true,
    updated_at = now();

delete from branch_main_product_monthly_metrics metric
where metric.branch_id in (select branch.id from branches branch join mock_branch_seed seed on seed.branch_code = branch.branch_code)
  and metric.main_product_parameter_id in (
    select parameter.id
    from main_product_parameters parameter
    join main_product_instances instance on instance.id = parameter.main_product_instance_id
    where (instance.year, instance.term) in ((2025, 2), (2026, 1), (2026, 2))
  );

with seeded_branches as (
  select branch.id, branch.branch_code,
         row_number() over (order by branch.branch_code) as seed_rank
  from branches branch
  join mock_branch_seed seed on seed.branch_code = branch.branch_code
),
metric_scope as (
  select branch.id as branch_id,
         parameter.id as parameter_id,
         parameter.calculation_type,
         instance.year,
         instance.term,
         month_value.month,
         make_date(instance.year, month_value.month, 1) as month_start,
         (make_date(instance.year, month_value.month, 1) + interval '1 month - 1 day')::date as month_end
  from seeded_branches branch
  cross join main_product_parameters parameter
  join main_product_instances instance
    on instance.id = parameter.main_product_instance_id
   and (instance.year, instance.term) in ((2025, 2), (2026, 1), (2026, 2))
  cross join lateral generate_series(
    case when instance.term = 1 then 1 else 7 end,
    case when instance.term = 1 then 6 else 12 end
  ) as month_value(month)
  where branch.seed_rank <= 12
     or (instance.year = 2026 and instance.term = 2 and make_date(instance.year, month_value.month, 1) <= current_date)
),
metric_values as (
  select scope.*,
         round(
           ((500000 + mod(abs(scope.branch_id * 37 + scope.parameter_id * 101 + scope.month * 17), 500000))::numeric
             * case when scope.calculation_type = 'Average' then 10 else 2 end)
           * case
               when date_trunc('month', current_date)::date = scope.month_start
                 then extract(day from current_date)::numeric / extract(day from scope.month_end)::numeric
               else 1::numeric
             end,
           2
         ) as target_value,
         scope.month_start <= current_date as actual_available,
         mod(abs(scope.branch_id + scope.parameter_id + scope.month), 41) = 0 as simulate_missing_batch,
         (55 + mod(abs(scope.branch_id * 11 + scope.parameter_id * 7 + scope.month * 3), 61))::numeric / 100 as performance_factor
  from metric_scope scope
)
insert into branch_main_product_monthly_metrics (
  branch_id, main_product_parameter_id, month,
  target_value, actual_value, actual_as_of_date,
  created_at, updated_at
)
select metric.branch_id,
       metric.parameter_id,
       metric.month,
       metric.target_value,
       case when metric.actual_available and not metric.simulate_missing_batch
         then round(metric.target_value * metric.performance_factor, 2) else null end,
       case when metric.actual_available and not metric.simulate_missing_batch
         then least(current_date, metric.month_end) else null end,
       now(), now()
from metric_values metric
on conflict (branch_id, main_product_parameter_id, month) do update
set target_value = excluded.target_value,
    actual_value = excluded.actual_value,
    actual_as_of_date = excluded.actual_as_of_date,
    updated_at = now();

insert into audit_logs (action, entity_name, entity_key, description, actor, created_at)
select 'SeedMockData', 'System', 'mock-v13',
       '50 şube, 12 ana ürün ve sınırlı aylık batch verisi yüklendi.',
       'seed-script', now()
where not exists (
  select 1 from audit_logs where action = 'SeedMockData' and entity_key = 'mock-v13'
);

commit;
