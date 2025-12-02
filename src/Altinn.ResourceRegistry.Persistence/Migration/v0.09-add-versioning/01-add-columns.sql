-- Create a sequence for unique runtime versions
CREATE SEQUENCE resourceregistry.runtime_version_seq START 1;

-- Add the column with sequence as default
ALTER TABLE resourceregistry.resources
   ADD COLUMN runtimeVersion INTEGER DEFAULT nextval('resourceregistry.runtime_version_seq');

-- Update existing rows to have unique values (if any exist)
UPDATE resourceregistry.resources 
SET runtimeVersion = nextval('resourceregistry.runtime_version_seq')
WHERE runtimeVersion IS NULL OR runtimeVersion = 1;

-- Remove the primary key constraint
--alter table resourceregistry.access_list_resource_connections_state 
--drop constraint access_list_resource_connections_state_resource_identifier_fkey;

--ALTER TABLE resourceregistry.resources
--   DROP CONSTRAINT resourceregistry_pkey;

--ALTER TABLE resourceregistry.resources
--   add CONSTRAINT resourceregistry_pkey PRIMARY KEY (identifier, runtimeversion);
   
--CREATE INDEX IF NOT EXISTS resourceregistry_identifier_idx ON resourceregistry.resources(identifier,runtimeversion) TABLESPACE pg_default;
 
--ALTER TABLE resourceregistry.access_list_resource_connections_state ADD CONSTRAINT access_list_resource_connections_state_resource_identifier_fkey FOREIGN KEY (resource_identifier) REFERENCES resourceregistry.resources(identifier) ON UPDATE RESTRICT ON DELETE RESTRICT;
