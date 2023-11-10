import http from 'k6/http';
import * as config from '../../../config.js';
import * as header from '../../../buildrequestheaders.js';


/**
 * GET call to get a resource in resource registry
 * @param {*} resource the resource id
 */
export function getResource(resource) {
  var endpoint = config.buildResourceRegistryResourceUrls(resource);
  var params = header.buildHeaderWithJsonOnly();
  return http.get(endpoint, params);
}

