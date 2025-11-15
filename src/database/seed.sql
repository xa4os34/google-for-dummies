CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "vector";

CREATE TABLE IF NOT EXISTS WebsiteRecord (
    Id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    Url VARCHAR(4096),
    Title VARCHAR(1024),
    Description VARCHAR(2048),
    TitleMeaning VECTOR(768),
    DescriptionMeaning VECTOR(768),
    PageMeaning VECTOR(768)
);


CREATE INDEX embedding_idx_title ON WebsiteRecord USING diskann (TitleMeaning);
CREATE INDEX embedding_idx_description ON WebsiteRecord USING diskann (DescriptionMeaning);
CREATE INDEX embedding_idx_page ON WebsiteRecord USING diskann (PageMeaning);
