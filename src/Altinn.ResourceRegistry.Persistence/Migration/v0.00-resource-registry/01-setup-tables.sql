-- Schema: resourceregistry
CREATE SCHEMA IF NOT EXISTS resourceregistry;

DROP TABLE IF EXISTS resourceregistry.resources

-- Table: resourceregistry.resources
CREATE TABLE  resourceregistry.resources
(
    identifier text COLLATE pg_catalog."default" NOT NULL,
    created timestamp with time zone NOT NULL,
    modified timestamp with time zone,
    serviceresourcejson jsonb,
    CONSTRAINT resourceregistry_pkey PRIMARY KEY (identifier)
)
TABLESPACE pg_default;

-- Enum: resourceregistry.resourcetype
DO $$ BEGIN
    CREATE TYPE resourceregistry.resourcetype AS ENUM ('default', 'systemresource', 'maskinportenschema');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;