const { calculateDob } = require('../controllers/userController');

// Use Jest to mock timers so we can control Date()
beforeAll(() => {
  jest.useFakeTimers('modern');
});

afterAll(() => {
  jest.useRealTimers();
});

describe('calculateDob controller', () => {
  let res;
  let next;

  beforeEach(() => {
    res = {
      status: jest.fn().mockReturnThis(),
      json: jest.fn().mockReturnValue('SENT')
    };
    next = jest.fn();
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  test('returns 400 when dob is missing', async () => {
    const req = { query: {}, body: {} };

    const ret = await calculateDob(req, res, next);

    expect(res.status).toHaveBeenCalledWith(400);
    expect(res.json).toHaveBeenCalledTimes(1);
    const arg = res.json.mock.calls[0][0];
    expect(arg).toHaveProperty('error');
    expect(arg.error).toEqual({ message: 'dob is required', code: 'MISSING_DOB' });
    // controller returns the value returned by res.json
    expect(ret).toBe('SENT');
    expect(next).not.toHaveBeenCalled();
  });

  test('returns 400 when dob format is invalid', async () => {
    const req = { query: { dob: 'not-a-date' }, body: {} };

    const ret = await calculateDob(req, res, next);

    expect(res.status).toHaveBeenCalledWith(400);
    expect(res.json).toHaveBeenCalledTimes(1);
    const arg = res.json.mock.calls[0][0];
    expect(arg).toHaveProperty('error');
    expect(arg.error).toEqual({ message: 'Invalid dob format', code: 'INVALID_DOB' });
    expect(ret).toBe('SENT');
    expect(next).not.toHaveBeenCalled();
  });

  test('calculates correct output when valid dob provided in query', async () => {
    // fix the "now" time for deterministic calculations
    const fixedNow = new Date('2020-01-02T00:00:00.000Z');
    jest.setSystemTime(fixedNow);

    const dobString = '2000-01-01T00:00:00.000Z';
    const req = { query: { dob: dobString }, body: {} };

    const ret = await calculateDob(req, res, next);

    // Ensure res.json was called with the computed object
    expect(res.json).toHaveBeenCalledTimes(1);
    const out = res.json.mock.calls[0][0];

    // Compute expected values using the same logic as the controller
    const dobDate = new Date(dobString);
    const diffMs = fixedNow - dobDate;
    const expectedDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    const expectedWeeks = Math.floor(expectedDays / 7);
    const expectedMinutes = Math.floor(diffMs / (1000 * 60));
    const expectedSeconds = Math.floor(diffMs / 1000);
    const expectedFormatted = `${expectedDays} days, ${expectedWeeks} weeks, ${expectedMinutes} minutes, ${expectedSeconds} seconds`;

    expect(out).toHaveProperty('days', expectedDays);
    expect(out).toHaveProperty('weeks', expectedWeeks);
    expect(out).toHaveProperty('minutes', expectedMinutes);
    expect(out).toHaveProperty('seconds', expectedSeconds);
    expect(out).toHaveProperty('formatted', expectedFormatted);

    // controller returns the value from res.json
    expect(ret).toBe('SENT');
    expect(next).not.toHaveBeenCalled();

    // cleanup system time
    jest.setSystemTime(new Date());
  });

  test('calculates correct output when dob provided in body', async () => {
    const fixedNow = new Date('2021-06-01T12:00:00.000Z');
    jest.setSystemTime(fixedNow);

    const dobString = '1995-06-01T12:00:00.000Z';
    const req = { query: {}, body: { dob: dobString } };

    const ret = await calculateDob(req, res, next);

    expect(res.json).toHaveBeenCalledTimes(1);
    const out = res.json.mock.calls[0][0];

    const dobDate = new Date(dobString);
    const diffMs = fixedNow - dobDate;
    const expectedDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    const expectedWeeks = Math.floor(expectedDays / 7);
    const expectedMinutes = Math.floor(diffMs / (1000 * 60));
    const expectedSeconds = Math.floor(diffMs / 1000);
    const expectedFormatted = `${expectedDays} days, ${expectedWeeks} weeks, ${expectedMinutes} minutes, ${expectedSeconds} seconds`;

    expect(out).toHaveProperty('days', expectedDays);
    expect(out).toHaveProperty('weeks', expectedWeeks);
    expect(out).toHaveProperty('minutes', expectedMinutes);
    expect(out).toHaveProperty('seconds', expectedSeconds);
    expect(out).toHaveProperty('formatted', expectedFormatted);

    expect(ret).toBe('SENT');
    expect(next).not.toHaveBeenCalled();

    jest.setSystemTime(new Date());
  });
});
