-- Schema: resourceregistry

-- Table: resourceregistry.resourcesubjects
CREATE TABLE IF NOT EXISTS resourceregistry.resourcesubjects(
    -- resource_type - urn:altinn:resource ++
    resource_type text COLLATE pg_catalog."default",
    -- resource_value - Identifer for resource in resource registry
    resource_value text COLLATE pg_catalog."default",
    -- subject_type - Type of subject. Example urn:altinn:rolecode ++
    subject_type text COLLATE pg_catalog."default",
    -- subject_value - Reference to rolecode, accesspackage +++
    subject_value text COLLATE pg_catalog."default",
    -- registry_owner - registry_created.registry_owner -- the org. nr of the owning party
    resource_owner text COLLATE pg_catalog."default"
)
TABLESPACE pg_default;

