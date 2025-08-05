// environment.prod.ts
export const environment = {
    production: true,
    apiUrl: 'http://your-production-api-url', // you'll set this later
    applicationInsights: {
        connectionString: '' // Will be set via environment variable in production
    }
};
