FROM pgvector/pgvector:pg17
COPY seed.sql /docker-entrypoint-initdb.d/01-seed.sql
RUN apt-get update && apt-get upgrade -y && \
    apt-get install --no-install-recommends -y curl unzip postgresql-server-dev-17 ca-certificates && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*;

RUN cd /tmp && \
    curl -fSL -o pgvectorscale.zip https://github.com/timescale/pgvectorscale/releases/download/0.9.0/pgvectorscale-0.9.0-pg17-amd64.zip && \
    unzip pgvectorscale.zip && \
    dpkg -i pgvectorscale-postgresql-17_0.9.0-Linux_amd64.deb && \
    rm -rf /tmp/pgvectorscale.zip /tmp/pgvectorscale-postgresql-17_0.9.0-Linux_amd64.deb /tmp/pgvectorscale-postgresql-17-dbgsym_0.9.0-Linux_amd64.deb;