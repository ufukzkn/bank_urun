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
  group_no, name, group_type, is_active,
  branch_performance_enabled, miy_performance_enabled, scale_enabled, created_at, updated_at)
values
  ('1001', 'KARMA - 1', 'Karma', true, true, true, true, now(), now()),
  ('1002', 'KURUMSAL - 1', 'Kurumsal', true, true, true, true, now(), now()),
  ('1003', 'TICARI - 1', 'Ticari', true, true, true, true, now(), now()),
  ('1004', 'KOBİ - 1', 'Kobi', true, true, true, true, now(), now())
on conflict (group_no) do update
set name = excluded.name, group_type = excluded.group_type, is_active = true,
    branch_performance_enabled = true, miy_performance_enabled = true, scale_enabled = true,
    updated_at = now();

create temporary table mock_branch_seed (
  group_no varchar(24) not null,
  branch_code varchar(24) not null,
  name varchar(180) not null
) on commit drop;

insert into mock_branch_seed (group_no, branch_code, name)
values
  ('1001','1101','Adana Karma Şubesi'), ('1001','1102','Ankara Merkez Şubesi'), ('1001','1103','Antalya Karma Şubesi'),
  ('1001','1104','Bursa Karma Şubesi'), ('1001','1105','Denizli Karma Şubesi'), ('1001','1106','Diyarbakır Karma Şubesi'),
  ('1001','1107','Erzurum Karma Şubesi'), ('1001','1108','Eskişehir Karma Şubesi'), ('1001','1109','Gaziantep Karma Şubesi'),
  ('1001','1110','İstanbul Anadolu Karma Şubesi'), ('1001','1111','İstanbul Avrupa Karma Şubesi'), ('1001','1112','İzmir Karma Şubesi'),
  ('1001','1113','Kayseri Karma Şubesi'), ('1001','1114','Konya Karma Şubesi'), ('1001','1115','Mersin Karma Şubesi'),
  ('1001','1116','Samsun Karma Şubesi'), ('1001','1117','Trabzon Karma Şubesi'),
  ('1002','2101','Adana Kurumsal Şubesi'), ('1002','2102','Ankara Kurumsal Şubesi'), ('1002','2103','Antalya Kurumsal Şubesi'),
  ('1002','2104','Ataşehir Kurumsal Şubesi'), ('1002','2105','Avrupa Kurumsal Şubesi'), ('1002','2106','Bursa Kurumsal Şubesi'),
  ('1002','2107','Çukurova Kurumsal Şubesi'), ('1002','2108','Ege Kurumsal Şubesi'), ('1002','2109','Gaziantep Kurumsal Şubesi'),
  ('1002','2110','İkitelli Kurumsal Şubesi'), ('1002','2111','İzmir Kurumsal Şubesi'), ('1002','2112','Kocaeli Kurumsal Şubesi'),
  ('1002','2113','Konya Kurumsal Şubesi'), ('1002','2114','Maslak Kurumsal Şubesi'), ('1002','2115','Merter Kurumsal Şubesi'),
  ('1002','2116','Ostim Kurumsal Şubesi'), ('1002','2117','Trakya Kurumsal Şubesi'),
  ('1003','3101','Adana Ticari Şubesi'), ('1003','3102','Ankara Ticari Şubesi'), ('1003','3103','Antalya Ticari Şubesi'),
  ('1003','3104','Bursa Ticari Şubesi'), ('1003','3105','Çorlu Ticari Şubesi'), ('1003','3106','Denizli Ticari Şubesi'),
  ('1003','3107','Gaziantep Ticari Şubesi'), ('1003','3108','Gebze Ticari Şubesi'), ('1003','3109','İstanbul Ticari Şubesi'),
  ('1003','3110','İzmir Ticari Şubesi'), ('1003','3111','Kayseri Ticari Şubesi'), ('1003','3112','Konya Ticari Şubesi'),
  ('1003','3113','Mersin Ticari Şubesi'), ('1003','3114','Samsun Ticari Şubesi'), ('1003','3115','Şekerpınar Ticari Şubesi'),
  ('1003','3116','Trabzon Ticari Şubesi'),
  ('1004','4101','Adana KOBİ Şubesi'), ('1004','4102','Ankara KOBİ Şubesi'), ('1004','4103','Antalya KOBİ Şubesi'),
  ('1004','4104','Bursa KOBİ Şubesi'), ('1004','4105','Denizli KOBİ Şubesi'), ('1004','4106','Gaziantep KOBİ Şubesi'),
  ('1004','4107','İstanbul Anadolu KOBİ Şubesi'), ('1004','4108','İstanbul Avrupa KOBİ Şubesi'), ('1004','4109','İzmir KOBİ Şubesi'),
  ('1004','4110','Kayseri KOBİ Şubesi'), ('1004','4111','Konya KOBİ Şubesi'), ('1004','4112','Mersin KOBİ Şubesi');

insert into branches (group_id, branch_code, name, created_at, updated_at)
select groups.id, seed.branch_code, seed.name, now(), now()
from mock_branch_seed seed
join group_definitions groups on groups.group_no = seed.group_no
on conflict (branch_code) do update
set group_id = excluded.group_id, name = excluded.name, updated_at = now();

-- v20 owns the complete demo scope. Deleting parameters cascades old portfolio targets.
delete from branch_main_product_exclusions;
delete from portfolio_sub_product_monthly_metrics;
delete from portfolio_main_product_monthly_targets;
delete from portfolios;
delete from product_gamut_main_product_assignments;
delete from product_gamuts;
delete from portfolio_types;
delete from main_product_parameters;

insert into portfolio_types (code, name, is_active, created_at, updated_at)
values ('ST', 'Standart', true, now(), now()),
       ('UZ', 'Uzmanlaşmış', true, now(), now()),
       ('OZ', 'Özel', true, now(), now());

insert into product_gamuts (group_id, code, name, is_active, created_at, updated_at)
select groups.id, gamut.code, gamut.name, true, now(), now()
from group_definitions groups
cross join (values ('BI', 'Bireysel Ürün Gamı'), ('KO', 'KOBİ Ürün Gamı'), ('PR', 'Perakende Ürün Gamı')) as gamut(code, name)
where groups.group_no in ('1001','1002','1003','1004');

with gamut_product(gamut_code, product_code) as (
  values ('BI','AU'), ('BI','B1'), ('BI','KR'), ('BI','KK'), ('BI','SG'),
         ('KO','D2'), ('KO','G1'), ('KO','K0'), ('KO','NB'),
         ('PR','MA'), ('PR','MC'), ('PR','YP'), ('PR','SG')
)
insert into product_gamut_main_product_assignments (
  product_gamut_id, main_product_id, product_definition_type,
  effective_from_year, effective_from_term, effective_to_year, effective_to_term,
  created_at, updated_at)
select gamut.id, product.id, 'Main', 2024, 1, null, null, now(), now()
from product_gamuts gamut
join gamut_product mapping on mapping.gamut_code = gamut.code
join product_definitions product on product.product_type = 'Main' and product.code = mapping.product_code;

with parameter_seed(code, calculation_type, criterion_score) as (
  values ('AU','Average',3.00::numeric), ('B1','Cumulative',5.00::numeric), ('D2','Cumulative',6.00::numeric),
         ('G1','Average',8.00::numeric), ('K0','Cumulative',5.00::numeric), ('KR','Cumulative',8.00::numeric),
         ('KK','Average',10.00::numeric), ('MA','Average',21.00::numeric), ('MC','Average',7.00::numeric),
         ('NB','Cumulative',14.00::numeric), ('SG','Cumulative',10.00::numeric), ('YP','Cumulative',3.00::numeric)
)
insert into main_product_parameters (
  group_id, main_product_instance_id, calculation_type, criterion_score, is_active, created_at, updated_at)
select groups.id, instance.id, seed.calculation_type, seed.criterion_score, true, now(), now()
from parameter_seed seed
join product_definitions product on product.product_type = 'Main' and product.code = seed.code
join main_product_instances instance on instance.main_product_id = product.id
cross join group_definitions groups
where groups.group_no in ('1001','1002','1003','1004')
  and (instance.year, instance.term) in ((2024,1),(2024,2),(2025,1),(2025,2),(2026,1),(2026,2));

with branch_gamuts as (
  select branch.id as branch_id, branch.group_id, branch.branch_code, gamut.id as gamut_id, gamut.code as gamut_code,
         row_number() over (partition by branch.id order by gamut.code) as gamut_order
  from branches branch
  join mock_branch_seed seeded on seeded.branch_code = branch.branch_code
  join product_gamuts gamut on gamut.group_id = branch.group_id
)
insert into portfolios (branch_id, group_id, product_gamut_id, portfolio_type_id, code, name, is_active, created_at, updated_at)
select scope.branch_id, scope.group_id, scope.gamut_id, type.id,
       'P' || scope.branch_code || '-' || scope.gamut_code || '01',
       scope.gamut_code || ' Portföyü 01', true, now(), now()
from branch_gamuts scope
join portfolio_types type on type.code = case mod(scope.branch_id + scope.gamut_order, 3) when 0 then 'ST' when 1 then 'UZ' else 'OZ' end
union all
select scope.branch_id, scope.group_id, scope.gamut_id, type.id,
       'P' || scope.branch_code || '-' || scope.gamut_code || '02',
       scope.gamut_code || ' Portföyü 02', true, now(), now()
from branch_gamuts scope
join portfolio_types type on type.code = 'ST'
where mod(scope.branch_id, 5) = 0 and scope.gamut_code = 'BI';

with target_scope as (
  select portfolio.id as portfolio_id, portfolio.group_id, portfolio.code as portfolio_code,
         parameter.id as parameter_id, parameter.calculation_type,
         instance.year, instance.term, product.code as product_code, month_value.month
  from portfolios portfolio
  join product_gamut_main_product_assignments assignment on assignment.product_gamut_id = portfolio.product_gamut_id
  join product_definitions product on product.id = assignment.main_product_id
  join main_product_instances instance on instance.main_product_id = product.id
  join main_product_parameters parameter on parameter.main_product_instance_id = instance.id and parameter.group_id = portfolio.group_id
  cross join lateral generate_series(case when instance.term = 1 then 1 else 7 end, case when instance.term = 1 then 6 else 12 end) month_value(month)
  where (instance.year, instance.term) in ((2024,1),(2024,2),(2025,1),(2025,2),(2026,1),(2026,2))
)
insert into portfolio_main_product_monthly_targets (
  portfolio_id, group_id, main_product_parameter_id, month, target_value, created_at, updated_at)
select scope.portfolio_id, scope.group_id, scope.parameter_id, scope.month,
       round((180000 + mod(ascii(substr(scope.portfolio_code, 2, 1)) * 101
         + ascii(substr(scope.product_code, 1, 1)) * 83
         + coalesce(ascii(substr(scope.product_code, 2, 1)), 0) * 47
         + scope.month * 1703 + scope.year * 11 + scope.term * 97, 480000))::numeric
         * case when scope.calculation_type = 'Average' then 3 else 1 end, 2),
       now(), now()
from target_scope scope;

with actual_scope as (
  select distinct portfolio.id as portfolio_id, portfolio.code as portfolio_code,
         link.sub_product_id, sub_product.code as sub_product_code,
         instance.year, instance.term, month_value.month,
         make_date(instance.year, month_value.month, 1) as month_start,
         (make_date(instance.year, month_value.month, 1) + interval '1 month - 1 day')::date as month_end
  from portfolios portfolio
  join product_gamut_main_product_assignments assignment on assignment.product_gamut_id = portfolio.product_gamut_id
  join main_product_instances instance on instance.main_product_id = assignment.main_product_id
  join sub_product_instances link on link.main_product_instance_id = instance.id
  join product_definitions sub_product on sub_product.id = link.sub_product_id
  cross join lateral generate_series(case when instance.term = 1 then 1 else 7 end, case when instance.term = 1 then 6 else 12 end) month_value(month)
  where (instance.year, instance.term) in ((2024,1),(2024,2),(2025,1),(2025,2),(2026,1),(2026,2))
), actual_values as (
  select scope.*,
         round((52000 + mod(ascii(substr(scope.portfolio_code, 2, 1)) * 61
           + ascii(substr(scope.sub_product_code, 1, 1)) * 43
           + coalesce(ascii(substr(scope.sub_product_code, 2, 1)), 0) * 31
           + scope.month * 997 + scope.year * 13 + scope.term * 79, 215000))::numeric
           * (case mod(scope.sub_product_id + scope.portfolio_id + scope.year + scope.term, 6)
                when 0 then 0.58 when 1 then 0.69 when 2 then 0.79
                when 3 then 0.91 when 4 then 1.03 else 1.12 end
              + (mod(scope.portfolio_id + scope.month, 5) - 2)::numeric / 100), 2) as actual_value,
         scope.year = 2026 and scope.term = 2 and scope.month_start <= current_date
           and mod(scope.portfolio_id + scope.sub_product_id * 7 + scope.month * 17, 173) = 0 as missing_batch
  from actual_scope scope
)
insert into portfolio_sub_product_monthly_metrics (
  portfolio_id, sub_product_id, product_definition_type, year, term, month,
  actual_value, actual_as_of_date, created_at, updated_at)
select metric.portfolio_id, metric.sub_product_id, 'Sub', metric.year, metric.term, metric.month,
       case when metric.month_start > current_date or metric.missing_batch then null else metric.actual_value end,
       case when metric.month_start > current_date or metric.missing_batch then null
            when metric.year = 2026 and metric.term = 2 then least(current_date, metric.month_end)
            else metric.month_end end,
       now(), now()
from actual_values metric;

insert into audit_logs (action, entity_name, entity_key, description, actor, created_at)
select 'SeedMockData', 'System', 'mock-v20',
       '4 grup, 62 şube, BI/KO/PR ürün gamları, ST/UZ/OZ tipleri, portföy ana ürün hedefleri ve alt ürün gerçekleşmeleri yüklendi.',
       'seed-script', now()
where not exists (
  select 1 from audit_logs where action = 'SeedMockData' and entity_key = 'mock-v20'
);

commit;
