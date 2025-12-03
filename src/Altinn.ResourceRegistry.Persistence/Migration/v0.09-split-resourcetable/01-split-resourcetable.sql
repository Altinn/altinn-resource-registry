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

-- Step 3: Add foreign key constraint using existing identifier
ALTER TABLE resourceregistry.resources 
ADD CONSTRAINT fk_resources_resourcemain 
FOREIGN KEY (identifier) REFERENCES resourceregistry.resourcemain(identifier);

-- Step 4: Add versionid column with automatic generation
ALTER TABLE resourceregistry.resources 
ADD COLUMN versionid BIGSERIAL NOT NULL;

-- Step 5: Create indexes for better performance
CREATE INDEX idx_resources_identifier ON resourceregistry.resources(identifier);
