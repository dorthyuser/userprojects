const fs = require('fs');
const path = require('path');
const cron = require('node-cron');
const { runScheduledJob } = require('./jobs/scheduledJob');

const configPath = path.join(__dirname, 'config.json');
const config = JSON.parse(fs.readFileSync(configPath, 'utf8'));

const schedule = process.env.SCHEDULE_CRON || config.schedule;
const timezone = process.env.SCHEDULE_TIMEZONE || config.timezone || 'UTC';
const appName = process.env.APP_NAME || 'ai2dev-scheduler';

function safeLog(message, meta = {}) {
  const safeMeta = { ...meta };
  delete safeMeta.token;
  delete safeMeta.secret;
  delete safeMeta.password;
  delete safeMeta.apiKey;
  delete safeMeta.authorization;
  console.log(JSON.stringify({ appName, message, ...safeMeta, timestamp: new Date().toISOString() }));
}

function safeError(message, error = {}) {
  const safeMeta = {
    name: error.name || 'Error',
    message: error.message || 'Unknown error'
  };
  console.error(JSON.stringify({ appName, message, error: safeMeta, timestamp: new Date().toISOString() }));
}

let isRunning = false;

async function executeJob() {
  if (isRunning) {
    safeLog('Job skipped because previous run is still in progress');
    return;
  }

  isRunning = true;
  safeLog('Scheduled job started', { schedule, timezone });

  try {
    await runScheduledJob();
    safeLog('Scheduled job completed successfully');
  } catch (error) {
    safeError('Scheduled job failed', error);
  } finally {
    isRunning = false;
  }
}

function startScheduler() {
  if (!cron.validate(schedule)) {
    throw new Error('Invalid cron schedule');
  }

  safeLog('Scheduler starting', { schedule, timezone });

  const task = cron.schedule(schedule, executeJob, {
    scheduled: true,
    timezone
  });

  const shutdown = async (signal) => {
    safeLog('Shutdown initiated', { signal });
    try {
      task.stop();
      safeLog('Scheduler stopped');
      process.exit(0);
    } catch (error) {
      safeError('Error during shutdown', error);
      process.exit(1);
    }
  };

  process.on('SIGINT', shutdown);
  process.on('SIGTERM', shutdown);
  process.on('uncaughtException', (error) => {
    safeError('Uncaught exception', error);
    process.exit(1);
  });
  process.on('unhandledRejection', (reason) => {
    safeError('Unhandled rejection', reason instanceof Error ? reason : new Error(String(reason)));
    process.exit(1);
  });

  return task;
}

startScheduler();
