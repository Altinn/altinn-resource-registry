CREATE INDEX IF NOT EXISTS ix_access_list_members_state_party_id ON resourceregistry.access_list_members_state(party_id) TABLESPACE pg_default;
