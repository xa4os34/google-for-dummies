CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "vector";
CREATE EXTENSION IF NOT EXISTS "vectorscale" CASCADE;

CREATE TABLE IF NOT EXISTS website_record (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    url VARCHAR(4096),
    title VARCHAR(1024),
    description VARCHAR(2048),
    title_meaning VECTOR(768),
    description_meaning VECTOR(768),
    page_meaning VECTOR(768)
);


CREATE INDEX embedding_idx_title ON WebsiteRecord USING diskann (title_meaning);
CREATE INDEX embedding_idx_description ON WebsiteRecord USING diskann (description_meaning);
CREATE INDEX embedding_idx_page ON WebsiteRecord USING diskann (page_meaning);
