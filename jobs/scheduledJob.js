const cron = require('node-cron');

let task = null;

function maskSensitive(meta = {}) {
  const safeMeta = { ...meta };
  delete safeMeta.password;
  delete safeMeta.token;
  delete safeMeta.apiKey;
  delete safeMeta.authorization;
  delete safeMeta.guestEmail;
  delete safeMeta.guestPhone;
  delete safeMeta.guestName;
  delete safeMeta.specialRequests;
  return safeMeta;
}

async function runJob({ log }) {
  const start = Date.now();
  log('INFO', 'job_run_started', { processingStep: 'START', status: 'STARTED' });
  try {
    await Promise.resolve();
    log('INFO', 'job_run_completed', { processingStep: 'END', status: 'SUCCESS', durationMs: Date.now() - start });
  } catch (error) {
    log('ERROR', 'job_run_failed', maskSensitive({ errorCode: 'JOB_EXECUTION_FAILURE', errorMessage: 'Scheduled job failed', durationMs: Date.now() - start }));
  }
}

async function startScheduler({ schedule, timezone, log }) {
  if (!schedule) throw new Error('Missing schedule');
  if (task) return;
  task = cron.schedule(schedule, () => {
    runJob({ log }).catch(() => {
      log('ERROR', 'job_run_failed', { errorCode: 'JOB_EXECUTION_FAILURE', errorMessage: 'Scheduled job failed' });
    });
  }, { timezone });
  log('INFO', 'scheduler_task_registered', { schedule, timezone });
}

async function stopScheduler({ log }) {
  if (task) {
    task.stop();
    task = null;
    log('INFO', 'scheduler_task_stopped');
  }
}

module.exports = { startScheduler, stopScheduler, runJob };
