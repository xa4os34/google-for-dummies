FROM pgvector/pgvector:pg17
WORKDIR /app
COPY init.sh /docker-entrypoint-initdb.d/
COPY seed.sql ./scripts/db/
