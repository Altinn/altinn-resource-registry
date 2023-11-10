/*
  Test data required: deployed app (reference app: ttd/apps-test)
  userid, partyid for two users that are DAGL for two orgs, and partyid and orgno for those orgs (user1 and user2)
  Org number for user2's org
docker-compose run k6 run /src/tests/resourceregistry/resourceregistry.js -e env=*** -e tokengenuser=*** -e tokengenuserpwd=*** -e appsaccesskey=***


*/
import { check, sleep, fail } from 'k6';
import { addErrorCount, stopIterationOnFail } from '../../errorcounter.js';
import { generateToken } from '../../api/altinn-testtools/token-generator.js';
import { generateJUnitXML, reportPath } from '../../report.js';
import * as resourceregistry from '../../api/platform/resourceregistry/resourceregistry.js';

const environment = __ENV.env.toLowerCase();
const tokenGeneratorUserName = __ENV.tokengenuser;
const tokenGeneratorUserPwd = __ENV.tokengenuserpwd;
let testdataFile = open(`../../data/testdata/maskinportenschema/${__ENV.env}testdata.json`);
var testdata = JSON.parse(testdataFile);
var org1;
var org2;

export const options = {
  thresholds: {
    errors: ['count<1'],
  },
  setupTimeout: '1m',
};

export function setup() {

  //generate personal token for user 1 (DAGL for org1)
  var tokenGenParams = {
    env: environment,
    scopes: 'altinn:instances.read',
    pid: testdata.org1.dagl.pid,
    userid: testdata.org1.dagl.userid,
    partyid: testdata.org1.dagl.partyid,
    authLvl: 3,
  };
  testdata.org1.dagl.token = generateToken('personal', tokenGeneratorUserName, tokenGeneratorUserPwd, tokenGenParams);

  tokenGenParams = {
    env: environment,
    scopes: 'altinn:instances.read',
    pid: testdata.org2.dagl.pid,
    userid: testdata.org2.dagl.userid,
    partyid: testdata.org2.dagl.partyid,
    authLvl: 3,
  };
  testdata.org2.dagl.token = generateToken('personal', tokenGeneratorUserName, tokenGeneratorUserPwd, tokenGenParams);

  tokenGenParams = {
    env: environment,
    scopes: 'altinn:instances.read',
    pid: testdata.org1.hadm.pid,
    userid: testdata.org1.hadm.userid,
    partyid: testdata.org1.hadm.partyid,
    authLvl: 3,
  };
  testdata.org1.hadm.token = generateToken('personal', tokenGeneratorUserName, tokenGeneratorUserPwd, tokenGenParams);

  return testdata;
}

export default function (data) {
  if (!data) {
    return;
  }
  org1 = data.org1;
  org2 = data.org2;

  //tests
  getResourceTest();
}

/** Check that list of offered maschinportenschemas is correct */
export function getResourceTest() {
  // Arrange
  const resource = 'altinn_maskinporten_scope_delegation';

  // Act
  var res = resourceregistry.getResource(resource);

  // Assert
  var success = check(res, {
    'get resource - status is 200': (r) => r.status === 200,
  });
  addErrorCount(success);
}


export function handleSummary(data) {
  let result = {};
  result[reportPath('resourceregistry.xml')] = generateJUnitXML(data, 'resourceregistry');
  return result;
}

export function showTestdata() {
  console.log(environment)
  console.log('personalToken1: ' + org1.dagl.token);
  console.log('personalToken2: ' + org2.dagl.token);
  console.log('org: ' + testdata.org);
  console.log('app: ' + testdata.app);
  console.log('user1pid: ' + org1.dagl.pid);
  console.log('user1userid: ' + org1.dagl.userid);
  console.log('user1partyid: ' + org1.dagl.partyid);
  console.log('orgno1: ' + org1.orgno);
  console.log('orgpartyid1: ' + org1.partyid);
  console.log('hadm1userid: ' + org1.hadm.userid);
  console.log('hadm1partyid: ' + org1.hadm.partyid);
  console.log('user2pid: ' + org2.dagl.pid);
  console.log('user2userid: ' + org2.dagl.userid);
  console.log('user2partyid: ' + org2.dagl.partyid);
  console.log('orgno2: ' + org2.orgno);
  console.log('orgpartyid2: ' + org2.partyid);
}
