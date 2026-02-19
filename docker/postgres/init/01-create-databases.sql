-- Create Dialysis PDMS databases for per-tenant isolation (C5).
-- Runs on first container startup when data volume is empty.

CREATE DATABASE dialysis_patient;
CREATE DATABASE dialysis_prescription;
CREATE DATABASE dialysis_treatment;
CREATE DATABASE dialysis_alarm;
CREATE DATABASE dialysis_device;
CREATE DATABASE dialysis_fhir;
