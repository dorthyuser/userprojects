const dotenv = require('dotenv');
dotenv.config();

const { startScheduler, stopScheduler } = require('./jobs/scheduledJob');

let shuttingDown = false;

function log(level, message, meta = {}) {
  const safeMeta = { ...meta };
  delete safeMeta.password;
  delete safeMeta.token;
  delete safeMeta.apiKey;
  delete safeMeta.authorization;
  delete safeMeta.guestEmail;
  delete safeMeta.guestPhone;
  delete safeMeta.guestName;
  delete safeMeta.specialRequests;
  console.log(JSON.stringify({ level, message, ...safeMeta, timestamp: new Date().toISOString() }));
}

async function main() {
  try {
    log('INFO', 'scheduler_starting', { schedule: process.env.SCHEDULE_CRON || '0 0 * * *' });
    await startScheduler({
      schedule: process.env.SCHEDULE_CRON || '0 0 * * *',
      timezone: process.env.SCHEDULE_TIMEZONE || 'UTC',
      log
    });
    log('INFO', 'scheduler_started');
  } catch (error) {
    log('ERROR', 'scheduler_start_failed', { errorCode: 'SCHEDULER_START_FAILURE', errorMessage: 'Scheduler failed to start' });
    process.exitCode = 1;
  }
}

async function shutdown(signal) {
  if (shuttingDown) return;
  shuttingDown = true;
  try {
    log('INFO', 'scheduler_stopping', { signal });
    await stopScheduler({ log });
    log('INFO', 'scheduler_stopped', { signal });
    process.exit(0);
  } catch (error) {
    log('ERROR', 'scheduler_stop_failed', { errorCode: 'SCHEDULER_STOP_FAILURE', errorMessage: 'Scheduler failed to stop cleanly' });
    process.exit(1);
  }
}

process.on('SIGINT', () => shutdown('SIGINT'));
process.on('SIGTERM', () => shutdown('SIGTERM'));
process.on('uncaughtException', (error) => {
  log('ERROR', 'uncaught_exception', { errorCode: 'UNCAUGHT_EXCEPTION', errorMessage: 'Unexpected server failure' });
  process.exit(1);
});
process.on('unhandledRejection', () => {
  log('ERROR', 'unhandled_rejection', { errorCode: 'UNHANDLED_REJECTION', errorMessage: 'Unexpected promise rejection' });
  process.exit(1);
});

main();
