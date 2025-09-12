UPDATE sakenetwerk_besighede
SET categories = REPLACE(REPLACE(categories, '{', '['), '}', ']'),
    tags = REPLACE(REPLACE(tags, '{', '['), '}', ']');

update sakenetwerk_besighede
set categories = null
where categories = '[NULL]';

update sakenetwerk_besighede
set tags = null
where tags = '[NULL]';

CREATE OR REPLACE FUNCTION is_valid_json(input_text TEXT) RETURNS BOOLEAN AS $$
BEGIN
    -- Try to cast the text to JSON, return TRUE if successful, FALSE otherwise
    PERFORM input_text::json;
    RETURN TRUE;
EXCEPTION WHEN others THEN
    RETURN FALSE;
END;
$$ LANGUAGE plpgsql;

UPDATE sakenetwerk_besighede
SET categories = NULL
WHERE NOT is_valid_json(categories);

UPDATE sakenetwerk_besighede
SET tags = NULL
WHERE NOT is_valid_json(tags);
    
alter table sakenetwerk_besighede ALTER COLUMN categories TYPE JSONB USING categories::JSONB;
alter table sakenetwerk_besighede ALTER COLUMN tags TYPE JSONB USING tags::JSONB;

CREATE INDEX idx_categories_gin
    ON sakenetwerk_besighede
        USING gin (categories jsonb_path_ops);

CREATE INDEX idx_tags_gin
    ON sakenetwerk_besighede
        USING gin (tags jsonb_path_ops);

UPDATE sakenetwerk_besighede
SET categories = (
    SELECT jsonb_agg(trim(elem))
    FROM jsonb_array_elements_text(categories::jsonb) AS elem
)
WHERE jsonb_typeof(categories::jsonb) = 'array';

UPDATE sakenetwerk_besighede
SET categories = (
    SELECT jsonb_agg(
                   REGEXP_REPLACE(
                           elem,
                           'Finansi.le dienste', -- The pattern using a dot as a wildcard for the special character
                           'Finansiële dienste',
                           'g'
                   )
           )
    FROM jsonb_array_elements_text(categories::jsonb) AS elem
)
WHERE jsonb_typeof(categories::jsonb) = 'array';

UPDATE sakenetwerk_besighede
SET tags = (
    SELECT jsonb_agg(trim(elem))
    FROM jsonb_array_elements_text(tags::jsonb) AS elem
)
WHERE jsonb_typeof(tags::jsonb) = 'array';

UPDATE sakenetwerk_besighede
SET tags = (
    SELECT jsonb_agg(
                   REGEXP_REPLACE(
                           elem,
                           'Finans..le', -- The pattern using dots as wildcards for any characters
                           'Finansiële',
                           'g'
                   )
           )
    FROM jsonb_array_elements_text(tags::jsonb) AS elem
)
WHERE jsonb_typeof(tags::jsonb) = 'array';
