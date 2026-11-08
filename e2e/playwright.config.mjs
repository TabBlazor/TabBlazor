import { defineConfig, devices } from '@playwright/test';

// Runs against the docs Server host, built beforehand (CI: dotnet build -c Release).
const configuration = process.env.CONFIGURATION ?? 'Release';
const port = Number(process.env.E2E_PORT ?? 5330);

export default defineConfig({
    testDir: '.',
    testMatch: '*.spec.mjs',
    timeout: 60_000,
    retries: process.env.CI ? 1 : 0,
    reporter: process.env.CI ? [['github'], ['list']] : 'list',
    use: {
        baseURL: `http://localhost:${port}`,
        trace: 'retain-on-failure'
    },
    projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
    webServer: {
        command: `dotnet run --project ../docs/Tabler.Docs.Server/Tabler.Docs.Server.csproj -c ${configuration} --no-build --no-launch-profile --urls http://localhost:${port}`,
        url: `http://localhost:${port}/docs/forms/rich-text-editors`,
        timeout: 120_000,
        reuseExistingServer: !process.env.CI,
        env: { ASPNETCORE_ENVIRONMENT: 'Development' }
    }
});
