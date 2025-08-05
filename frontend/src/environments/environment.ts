export const environment = {
    production: false,
    apiUrl: '/api', // This will use the proxy configuration
    applicationInsights: {
        connectionString: '' // Will be set via environment variable in production
    }
};
