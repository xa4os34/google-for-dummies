#!/usr/bin/env sh

psql -U $POSTGRES_USER -d $POSTGRES_DB -a -f /app/scripts/db/seed.sql
