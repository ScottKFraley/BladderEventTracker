// Environment configuration template
// This file will be processed by nginx to inject runtime environment variables
(function (window) {
  window.appConfig = window.appConfig || {};
  window.appConfig.applicationInsights = {
    connectionString: '${APPLICATIONINSIGHTS_CONNECTION_STRING}'
  };
  window.__env = window.__env || {};
  window.__env.API_URL = '${API_URL}';
  window.__env.PRODUCTION = '${PRODUCTION}';
})(this);
