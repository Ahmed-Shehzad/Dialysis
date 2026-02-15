-- saved_cohorts table for Dialysis.Analytics (PostgresSavedCohortStore)
-- Run manually to create in a dedicated database, or let EnsureTableAsync create it in postgres.

CREATE TABLE IF NOT EXISTS saved_cohorts (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    criteria JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    tenant_id TEXT
);
