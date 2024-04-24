ALTER TABLE resourceregistry.access_list_events
	ALTER COLUMN eid SET GENERATED ALWAYS;

SELECT setval(
  pg_get_serial_sequence('resourceregistry.access_list_events', 'eid'),
  (SELECT max(eid) + 1 FROM resourceregistry.access_list_events)
);
