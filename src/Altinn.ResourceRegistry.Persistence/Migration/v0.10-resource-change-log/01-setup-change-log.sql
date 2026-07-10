-- Change log for the resource change feed. One row per change to a resource:
-- metadata create/update ('metadata'), policy update ('policy') or delete ('deleted').
-- The log outlives the resource (no FK), so deletes remain visible to feed consumers.
CREATE TABLE resourceregistry.resource_change_log
(
    seq BIGSERIAL NOT NULL PRIMARY KEY,
    identifier text NOT NULL,
    changed_at timestamp with time zone NOT NULL DEFAULT now(),
    change_source text NOT NULL
);

CREATE INDEX idx_resource_change_log_identifier ON resourceregistry.resource_change_log (identifier, seq DESC);

-- Backfill: one entry per existing resource at its latest version, in version order
-- so the initial feed ordering matches the historical change ordering.
INSERT INTO resourceregistry.resource_change_log (identifier, changed_at, change_source)
SELECT identifier, COALESCE(modified, created), 'metadata'
FROM (
    SELECT DISTINCT ON (identifier) identifier, modified, created, version_id
    FROM resourceregistry.resources
    ORDER BY identifier, version_id DESC
) latest
ORDER BY latest.version_id;
