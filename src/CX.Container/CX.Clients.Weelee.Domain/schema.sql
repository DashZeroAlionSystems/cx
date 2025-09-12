drop view if exists vWeelee_stock_items;
create view vWeelee_stock_items
as
select
    case bodytype
        when 'H/B' then 'Hatchback'
        when 'S/C' then 'Single Cab'
        when 'SUV' then 'SUV'
        when 'S/W' then 'Station Wagon'
        when 'P/V' then 'Panel Van'
        when 'C/P' then 'Coupe'
        when 'SAV' then 'SUV'
        when 'S/D' then 'Sedan'
        when 'R/V' then 'R/V'--the single entry for this body type was actually an SUV
        when 'D/S' then 'D/S'--the single entry for this body type was a reference to a description for a type of pickup truck body
        when 'B/S' then 'Bus'
        when 'MPV' then 'Multi-purpose Vehicle'
        when 'X/O' then 'Crossover'
        when 'C/B' then 'Convertible'
        when 'D/C' then 'Double Cab'
        else bodytype end as bodytype,
    colour, cubiccapacity, description, doors, fueltype, make, mileage, model, price, seats, stock_no, transmission, variant, year
from weelee_stock_items wsi; 