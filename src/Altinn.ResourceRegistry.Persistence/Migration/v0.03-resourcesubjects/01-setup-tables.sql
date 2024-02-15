-- Schema: resourceregistry

DO $$
BEGIN
CREATE TYPE resourceregistry.attributetypevalue AS
(
    type text COLLATE pg_catalog."default",
	value text COLLATE pg_catalog."default"
);

ALTER TYPE resourceregistry.attributetypevalue
    OWNER TO postgres;

END
$$;

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


CREATE OR REPLACE FUNCTION resourceregistry.getsubjectsfromresources(
    attributetypevalue resourceregistry.attributetypevalue[])
 RETURNS table(resource_type text, resource_value text, subject_type text, subject_value text, resource_owner text)
   LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
RETURN QUERY 
    SELECT rs.resource_type, rs.resource_value, rs.subject_type, rs.subject_value, rs.resource_owner  	FROM resourceregistry.resourcesubjects rs
    JOIN UNNEST(attributetypevalue::resourceregistry.attributetypevalue[]) AS atr on rs.resource_type = atr.type AND rs.resource_value = atr.value;
END;
$BODY$;



CREATE OR REPLACE FUNCTION resourceregistry.getsubjectstest()
 RETURNS table(resource_type text, resource_value text, subject_type text, subject_value text, resource_owner text)
   LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
RETURN QUERY 
    SELECT rs.resource_type, rs.resource_value, rs.subject_type, rs.subject_value, rs.resource_owner FROM resourceregistry.resourcesubjects rs;
END;
$BODY$;
