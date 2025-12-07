-- Migration script to split resources table and add versioning
-- Step 1: Create the new resourcemain table
CREATE TABLE resourceregistry.resourcemain
(
    identifier text COLLATE pg_catalog."default" PRIMARY KEY,
    created timestamp with time zone NOT NULL
);

-- Step 2: Insert data from existing resources table into resourcemain
INSERT INTO resourceregistry.resourcemain (identifier, created)
SELECT DISTINCT identifier, created 
FROM resourceregistry.resources
ORDER BY identifier;

-- Remove the primary key constraint
alter table resourceregistry.access_list_resource_connections_state 
drop constraint access_list_resource_connections_state_resource_identifier_fkey;

-- Step 3: Drop the primary key constraint from resources table
ALTER TABLE resourceregistry.resources 
DROP CONSTRAINT resourceregistry_pkey;

-- Step 4: Add foreign key constraint using existing identifier
ALTER TABLE resourceregistry.resources 
ADD CONSTRAINT fk_resources_resourcemain 
FOREIGN KEY (identifier) REFERENCES resourceregistry.resourcemain(identifier);

-- Step 5: Add versionid column with automatic generation
ALTER TABLE resourceregistry.resources 
ADD COLUMN version_id BIGSERIAL NOT NULL;

-- Step 6: Create indexes for better performance
CREATE INDEX idx_resources_identifier ON resourceregistry.resources(identifier);

