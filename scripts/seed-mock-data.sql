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
  ('Main', 'NB', 'Nakit Akış Cirosu', true, now(), now()),
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
cross join (values (2024, 1), (2024, 2), (2025, 1), (2025, 2), (2026, 1), (2026, 2)) as period(year, term)
where product.product_type = 'Main'
  and product.code in ('AU','B1','D2','G1','K0','KR','KK','MA','MC','NB','SG','YP')
on conflict (main_product_id, year, term) do nothing;

with ranked_main as (
  select instance.id,
         row_number() over (partition by instance.year, instance.term order by product.code) as product_rank
  from main_product_instances instance
  join product_definitions product on product.id = instance.main_product_id
  where product.product_type = 'Main'
    and product.code in ('AU','B1','D2','G1','K0','KR','KK','MA','MC','NB','SG','YP')
    and (instance.year, instance.term) in ((2024,1),(2024,2),(2025,1),(2025,2),(2026,1),(2026,2))
), ranked_sub as (
  select product.id, row_number() over (order by product.code) as sub_rank, count(*) over () as sub_count
  from product_definitions product
  where product.product_type = 'Sub'
    and product.code in ('24','42','5F','9B','AB','CD','EF','GH','JK','KP','LM','MT','NO','OH','PR','Q1','RA','SA','ST','TR','UY','UV','VB','VS','X1','XA','XD','YZ')
)
insert into sub_product_instances (main_product_instance_id, sub_product_id, product_definition_type, created_at)
select main.id, sub.id, 'Sub', now()
from ranked_main main
cross join generate_series(0, 2) as offset_value
join ranked_sub sub on sub.sub_rank = ((main.product_rank * 2 + offset_value - 1) % sub.sub_count) + 1
on conflict (main_product_instance_id, sub_product_id) do nothing;

insert into group_definitions (
  group_no, name, group_segment, is_active,
  branch_performance_enabled, miy_performance_enabled, scale_enabled, created_at, updated_at)
values
  ('1001', 'KARMA - 1', 'Karma', true, true, true, true, now(), now()),
  ('1002', 'KURUMSAL - 1', 'Kurumsal', true, true, true, true, now(), now()),
  ('1003', 'TICARI - 1', 'Ticari', true, true, true, true, now(), now()),
  ('1004', 'KOBİ - 1', 'Kobi', true, true, true, true, now(), now())
on conflict (group_no) do update
set name = excluded.name, group_segment = excluded.group_segment, is_active = true,
    branch_performance_enabled = true, miy_performance_enabled = true, scale_enabled = true,
    updated_at = now();

create temporary table mock_branch_seed (
  group_no varchar(24) not null,
  branch_code varchar(24) not null,
  name varchar(180) not null
) on commit drop;

insert into mock_branch_seed (group_no, branch_code, name)
values
  ('1001','1101','Adana Karma Şubesi'), ('1001','1102','Ankara Merkez Şubesi'),
  ('1001','1103','Antalya Karma Şubesi'), ('1001','1104','Bursa Karma Şubesi'),
  ('1001','1105','Denizli Karma Şubesi'), ('1001','1106','Diyarbakır Karma Şubesi'),
  ('1001','1107','Erzurum Karma Şubesi'), ('1001','1108','Eskişehir Karma Şubesi'),
  ('1001','1109','Gaziantep Karma Şubesi'), ('1001','1110','İstanbul Anadolu Karma Şubesi'),
  ('1001','1111','İstanbul Avrupa Karma Şubesi'), ('1001','1112','İzmir Karma Şubesi'),
  ('1001','1113','Kayseri Karma Şubesi'), ('1001','1114','Konya Karma Şubesi'),
  ('1001','1115','Mersin Karma Şubesi'), ('1001','1116','Samsun Karma Şubesi'),
  ('1001','1117','Trabzon Karma Şubesi'),
  ('1002','2101','Adana Kurumsal Şubesi'), ('1002','2102','Ankara Kurumsal Şubesi'),
  ('1002','2103','Antalya Kurumsal Şubesi'), ('1002','2104','Ataşehir Kurumsal Şubesi'),
  ('1002','2105','Avrupa Kurumsal Şubesi'), ('1002','2106','Bursa Kurumsal Şubesi'),
  ('1002','2107','Çukurova Kurumsal Şubesi'), ('1002','2108','Ege Kurumsal Şubesi'),
  ('1002','2109','Gaziantep Kurumsal Şubesi'), ('1002','2110','İkitelli Kurumsal Şubesi'),
  ('1002','2111','İzmir Kurumsal Şubesi'), ('1002','2112','Kocaeli Kurumsal Şubesi'),
  ('1002','2113','Konya Kurumsal Şubesi'), ('1002','2114','Maslak Kurumsal Şubesi'),
  ('1002','2115','Merter Kurumsal Şubesi'), ('1002','2116','Ostim Kurumsal Şubesi'),
  ('1002','2117','Trakya Kurumsal Şubesi'),
  ('1003','3101','Adana Ticari Şubesi'), ('1003','3102','Ankara Ticari Şubesi'),
  ('1003','3103','Antalya Ticari Şubesi'), ('1003','3104','Bursa Ticari Şubesi'),
  ('1003','3105','Çorlu Ticari Şubesi'), ('1003','3106','Denizli Ticari Şubesi'),
  ('1003','3107','Gaziantep Ticari Şubesi'), ('1003','3108','Gebze Ticari Şubesi'),
  ('1003','3109','İstanbul Ticari Şubesi'), ('1003','3110','İzmir Ticari Şubesi'),
  ('1003','3111','Kayseri Ticari Şubesi'), ('1003','3112','Konya Ticari Şubesi'),
  ('1003','3113','Mersin Ticari Şubesi'), ('1003','3114','Samsun Ticari Şubesi'),
  ('1003','3115','Şekerpınar Ticari Şubesi'), ('1003','3116','Trabzon Ticari Şubesi'),
  ('1004','4101','Adana KOBİ Şubesi'), ('1004','4102','Ankara KOBİ Şubesi'),
  ('1004','4103','Antalya KOBİ Şubesi'), ('1004','4104','Bursa KOBİ Şubesi'),
  ('1004','4105','Denizli KOBİ Şubesi'), ('1004','4106','Gaziantep KOBİ Şubesi'),
  ('1004','4107','İstanbul Anadolu KOBİ Şubesi'), ('1004','4108','İstanbul Avrupa KOBİ Şubesi'),
  ('1004','4109','İzmir KOBİ Şubesi'), ('1004','4110','Kayseri KOBİ Şubesi'),
  ('1004','4111','Konya KOBİ Şubesi'), ('1004','4112','Mersin KOBİ Şubesi');

insert into branches (group_id, branch_code, name, created_at, updated_at)
select groups.id, seed.branch_code, seed.name, now(), now()
from mock_branch_seed seed
join group_definitions groups on groups.group_no = seed.group_no
on conflict (branch_code) do update
set group_id = excluded.group_id, name = excluded.name, updated_at = now();

-- v17 owns these representative rules, segment distributions and batch rows.
delete from main_product_parameters parameter
using main_product_instances instance, product_definitions product
where parameter.main_product_instance_id = instance.id
  and instance.main_product_id = product.id
  and product.product_type = 'Main'
  and product.code in ('AU','B1','D2','G1','K0','KR','KK','MA','MC','NB','SG','YP')
  and (instance.year, instance.term) in ((2024,1),(2024,2),(2025,1),(2025,2),(2026,1),(2026,2));

with parameter_seed(code, calculation_type, criterion_score) as (
  values
    ('AU','Average',3.00::numeric), ('B1','Cumulative',5.00::numeric),
    ('D2','Cumulative',6.00::numeric),
    ('G1','Average',8.00::numeric), ('K0','Cumulative',5.00::numeric),
    ('KR','Cumulative',8.00::numeric), ('KK','Average',10.00::numeric),
    ('MA','Average',21.00::numeric), ('MC','Average',7.00::numeric),
    ('NB','Cumulative',14.00::numeric), ('SG','Cumulative',10.00::numeric),
    ('YP','Cumulative',3.00::numeric)
)
insert into main_product_parameters (
  group_id, main_product_instance_id, calculation_type, criterion_score,
  is_active, created_at, updated_at)
select groups.id, instance.id, seed.calculation_type, seed.criterion_score, true, now(), now()
from parameter_seed seed
join product_definitions product on product.product_type = 'Main' and product.code = seed.code
join main_product_instances instance on instance.main_product_id = product.id
cross join group_definitions groups
where groups.group_no in ('1001','1002','1003','1004')
  and (instance.year, instance.term) in ((2024,1),(2024,2),(2025,1),(2025,2),(2026,1),(2026,2))
on conflict (group_id, main_product_instance_id) do update
set calculation_type = excluded.calculation_type, criterion_score = excluded.criterion_score,
    is_active = true, updated_at = now();

insert into main_product_segment_rules (
  main_product_parameter_id, performance_segment, sort_order,
  target_share, size_share, scale_share, allocated_score,
  hgo_weight, development_weight, size_weight, created_at, updated_at)
select parameter.id,
       distribution.segment,
       distribution.sort_order,
       distribution.target_share,
       0.2000,
       0.0000,
       case when distribution.sort_order = 5 then
         parameter.criterion_score
         - (round(parameter.criterion_score * 0.25, 2) * 2)
         - (round(parameter.criterion_score * 0.20, 2) * 2)
       else round(parameter.criterion_score * distribution.target_share, 2) end,
       0.7000,
       0.1500,
       0.1500,
       now(),
       now()
from main_product_parameters parameter
join main_product_instances instance on instance.id = parameter.main_product_instance_id
join product_definitions product on product.id = instance.main_product_id
cross join (values
  ('Kurumsal', 1, 0.2500::numeric),
  ('Ticari', 2, 0.2500::numeric),
  ('Kobi', 3, 0.2000::numeric),
  ('Bireysel', 4, 0.2000::numeric),
  ('Diger', 5, 0.1000::numeric)
) as distribution(segment, sort_order, target_share)
where product.code in ('AU','B1','D2','G1','K0','KR','KK','MA','MC','NB','SG','YP')
  and (instance.year, instance.term) in ((2024,1),(2024,2),(2025,1),(2025,2),(2026,1),(2026,2))
on conflict (main_product_parameter_id, performance_segment) do update
set sort_order = excluded.sort_order,
    target_share = excluded.target_share,
    size_share = excluded.size_share,
    scale_share = excluded.scale_share,
    allocated_score = excluded.allocated_score,
    hgo_weight = excluded.hgo_weight,
    development_weight = excluded.development_weight,
    size_weight = excluded.size_weight,
    updated_at = now();

with metric_scope as (
  select branch.id as branch_id, branch.group_id, branch.branch_code,
         parameter.id as parameter_id, parameter.calculation_type,
         instance.year, instance.term, product.code as product_code, month_value.month,
         make_date(instance.year, month_value.month, 1) as month_start,
         (make_date(instance.year, month_value.month, 1) + interval '1 month - 1 day')::date as month_end
  from branches branch
  join mock_branch_seed seeded_branch on seeded_branch.branch_code = branch.branch_code
  join main_product_parameters parameter on parameter.group_id = branch.group_id
  join main_product_instances instance on instance.id = parameter.main_product_instance_id
  join product_definitions product on product.id = instance.main_product_id
  cross join lateral generate_series(
    case when instance.term = 1 then 1 else 7 end,
    case when instance.term = 1 then 6 else 12 end
  ) as month_value(month)
  where product.code in ('AU','B1','D2','G1','K0','KR','KK','MA','MC','NB','SG','YP')
    and (instance.year, instance.term) in ((2024,1),(2024,2),(2025,1),(2025,2),(2026,1),(2026,2))
), metric_values as (
  select scope.*,
         round((350000 + mod(scope.branch_code::integer * 37
               + ascii(substr(scope.product_code,1,1)) * 101
               + coalesce(ascii(substr(scope.product_code,2,1)), 0) * 53
               + scope.month * 1703 + scope.year, 750000))::numeric
               * case when scope.calculation_type = 'Average' then 8 else 2 end, 2) as target_value,
         greatest(0.45::numeric,
           case mod(scope.branch_code::integer
             + ascii(substr(scope.product_code,1,1)) * 3
             + coalesce(ascii(substr(scope.product_code,2,1)), 0) * 5
             + scope.year + scope.term * 11, 6)
             when 0 then 0.58 when 1 then 0.69 when 2 then 0.79
             when 3 then 0.91 when 4 then 1.03 else 1.12 end
           + (mod(scope.branch_code::integer + scope.month, 5) - 2)::numeric / 100) as performance_factor,
         scope.year = 2026 and scope.term = 2
           and scope.month_start <= current_date
           and mod(scope.branch_code::integer
            + ascii(substr(scope.product_code,1,1)) * 3
            + coalesce(ascii(substr(scope.product_code,2,1)), 0) * 5
            + scope.month * 7 + scope.year, 173) = 0 as missing_batch
  from metric_scope scope
)
insert into branch_main_product_monthly_metrics (
  group_id, branch_id, main_product_parameter_id, month,
  target_value, actual_value, actual_as_of_date, created_at, updated_at)
select metric.group_id, metric.branch_id, metric.parameter_id, metric.month,
       metric.target_value,
       case when metric.month_start > current_date or metric.missing_batch then null
            else round(metric.target_value * metric.performance_factor, 2) end,
       case when metric.month_start > current_date or metric.missing_batch then null
            when metric.year = 2026 and metric.term = 2 then least(current_date, metric.month_end)
            else metric.month_end end,
       now(), now()
from metric_values metric
on conflict (branch_id, main_product_parameter_id, month) do update
set group_id = excluded.group_id, target_value = excluded.target_value,
    actual_value = excluded.actual_value, actual_as_of_date = excluded.actual_as_of_date,
    updated_at = now();

with sub_metric_scope as (
  select distinct branch.id as branch_id, branch.branch_code,
         sub_product.id as sub_product_id, sub_product.code as sub_product_code,
         instance.year, instance.term, month_value.month,
         make_date(instance.year, month_value.month, 1) as month_start,
         (make_date(instance.year, month_value.month, 1) + interval '1 month - 1 day')::date as month_end
  from branches branch
  join mock_branch_seed seeded_branch on seeded_branch.branch_code = branch.branch_code
  join main_product_parameters parameter on parameter.group_id = branch.group_id
  join main_product_instances instance on instance.id = parameter.main_product_instance_id
  join sub_product_instances link on link.main_product_instance_id = instance.id
  join product_definitions sub_product on sub_product.id = link.sub_product_id and sub_product.product_type = 'Sub'
  cross join lateral generate_series(
    case when instance.term = 1 then 1 else 7 end,
    case when instance.term = 1 then 6 else 12 end
  ) as month_value(month)
  where (instance.year, instance.term) in ((2024,1),(2024,2),(2025,1),(2025,2),(2026,1),(2026,2))
), sub_metric_values as (
  select scope.*,
         round((90000 + mod(scope.branch_code::integer * 29
           + ascii(substr(scope.sub_product_code,1,1)) * 83
           + coalesce(ascii(substr(scope.sub_product_code,2,1)), 0) * 47
           + scope.month * 997 + scope.year, 310000))::numeric, 2) as target_value,
         greatest(0.45::numeric,
           case mod(scope.branch_code::integer
             + ascii(substr(scope.sub_product_code,1,1)) * 3
             + coalesce(ascii(substr(scope.sub_product_code,2,1)), 0) * 5
             + scope.year + scope.term * 13, 6)
             when 0 then 0.58 when 1 then 0.69 when 2 then 0.79
             when 3 then 0.91 when 4 then 1.03 else 1.12 end
           + (mod(scope.branch_code::integer + scope.month, 5) - 2)::numeric / 100) as performance_factor,
         scope.year = 2026 and scope.term = 2
           and scope.month_start <= current_date
           and mod(scope.branch_code::integer
             + ascii(substr(scope.sub_product_code,1,1)) * 7
             + coalesce(ascii(substr(scope.sub_product_code,2,1)), 0) * 11
             + scope.month * 17 + scope.year, 173) = 0 as missing_batch
  from sub_metric_scope scope
)
insert into branch_sub_product_monthly_metrics (
  branch_id, sub_product_id, product_definition_type, year, term, month,
  target_value, actual_value, actual_as_of_date, created_at, updated_at)
select metric.branch_id, metric.sub_product_id, 'Sub', metric.year, metric.term, metric.month,
       metric.target_value,
       case when metric.month_start > current_date or metric.missing_batch then null
            else round(metric.target_value * metric.performance_factor, 2) end,
       case when metric.month_start > current_date or metric.missing_batch then null
            when metric.year = 2026 and metric.term = 2 then least(current_date, metric.month_end)
            else metric.month_end end,
       now(), now()
from sub_metric_values metric
on conflict (branch_id, sub_product_id, year, term, month) do update
set target_value = excluded.target_value, actual_value = excluded.actual_value,
    actual_as_of_date = excluded.actual_as_of_date, updated_at = now();

insert into audit_logs (action, entity_name, entity_key, description, actor, created_at)
select 'SeedMockData', 'System', 'mock-v18',
       '4 grup, 62 şube, 12 ana ürün, 6 dönem ve yaklaşık 58000 alt ürün aylık metriği yüklendi.',
       'seed-script', now()
where not exists (
  select 1 from audit_logs where action = 'SeedMockData' and entity_key = 'mock-v18'
);

commit;
