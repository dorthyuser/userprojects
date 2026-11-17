module.exports = {
  testEnvironment: 'node',
  testMatch: ['**/test/**/*.test.js', '**/test/**/*.spec.js'],
  collectCoverage: true,
  coverageDirectory: 'coverage',
  coverageThreshold: {
    global: { branches: 50, functions: 80, lines: 80, statements: 80 }
  }
};
