// Environment configuration template
// This file will be processed by nginx to inject runtime environment variables
(function (window) {
  window.__env = window.__env || {};

  // Environment variables will be injected here by nginx
  window.__env.APPLICATIONINSIGHTS_CONNECTION_STRING = '${APPLICATIONINSIGHTS_CONNECTION_STRING}';
  window.__env.API_URL = '${API_URL}';
  window.__env.PRODUCTION = '${PRODUCTION}';
})(this);