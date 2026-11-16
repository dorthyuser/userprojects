async function runScheduledJob() {
  const startedAt = new Date().toISOString();
  console.log(JSON.stringify({ message: 'Job execution started', startedAt }));

  try {
    await new Promise((resolve) => setTimeout(resolve, 1000));
    console.log(JSON.stringify({ message: 'Job execution finished', finishedAt: new Date().toISOString() }));
  } catch (error) {
    throw new Error('Scheduled job execution failed');
  }
}

module.exports = { runScheduledJob };
