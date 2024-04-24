INSERT INTO resourceregistry.access_list_events (eid,etime,kind,aggregate_id,identifier,name,description,resource_owner,actions,party_ids) VALUES
	 (1,'2024-03-12 17:14:13.313005+01','created','8214f628-f395-45fa-a2f2-115a4e92aac3','test01','test01','','digdir',NULL,NULL),
	 (2,'2024-03-12 17:14:22.519274+01','created','fdf2f108-2fd3-4301-9741-bbdf6a4448ba','test02','test02','','digdir',NULL,NULL),
	 (3,'2024-03-12 17:14:33.126093+01','created','06dec262-f529-424c-9abd-ff8f450be6db','test03','test03','','digdir',NULL,NULL),
	 (5,'2024-03-12 17:14:48.673932+01','updated','fdf2f108-2fd3-4301-9741-bbdf6a4448ba',NULL,NULL,'Test Description for test02',NULL,NULL,NULL),
	 (7,'2024-03-12 17:14:58.904873+01','updated','8214f628-f395-45fa-a2f2-115a4e92aac3',NULL,NULL,'Test Description for test01',NULL,NULL,NULL);
INSERT INTO resourceregistry.access_list_events (eid,etime,kind,aggregate_id,identifier,name,description,resource_owner,actions,party_ids) VALUES
	 (8,'2024-03-12 17:16:15.856131+01','members_added','8214f628-f395-45fa-a2f2-115a4e92aac3',NULL,NULL,NULL,NULL,NULL,'{3268353b-0a0d-40f1-9eb5-65cf02346702,239e3cc3-0f46-4ac0-aa0d-3e649b713953,29ad8a2a-19be-411d-a6c5-947ec5527c61}');
