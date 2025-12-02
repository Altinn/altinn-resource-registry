-- Create a sequence for unique runtime versions
CREATE SEQUENCE resourceregistry.runtime_version_seq START 1;

-- Add the column with sequence as default
ALTER TABLE resourceregistry.resources
   ADD COLUMN runtimeVersion INTEGER DEFAULT nextval('resourceregistry.runtime_version_seq');

-- Update existing rows to have unique values (if any exist)
UPDATE resourceregistry.resources 
SET runtimeVersion = nextval('resourceregistry.runtime_version_seq')
WHERE runtimeVersion IS NULL OR runtimeVersion = 1;
