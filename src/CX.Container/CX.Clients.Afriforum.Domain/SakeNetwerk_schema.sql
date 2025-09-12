CREATE TABLE sakenetwerk_besighede (
                                       id UUID PRIMARY KEY,
                                       created_at TIMESTAMP,
                                       name TEXT,
                                       email TEXT,
                                       telephone TEXT,
                                       website_url TEXT,
                                       discount TEXT,
                                       registration_no TEXT,
                                       employee_count TEXT,
                                       is_online BOOLEAN,
                                       business_type TEXT,
                                       listing_status TEXT,
                                       address_1 TEXT,
                                       address_2 TEXT,
                                       suburb TEXT,
                                       city TEXT,
                                       postal_code TEXT,
                                       province TEXT,
                                       long TEXT,
                                       lat TEXT,
                                       categories TEXT,
                                       tags TEXT,
                                       created_by TEXT,
                                       user_tel TEXT,
                                       is_afriforum_member BOOLEAN
);

CREATE INDEX idx_city ON sakenetwerk_besighede(city);

